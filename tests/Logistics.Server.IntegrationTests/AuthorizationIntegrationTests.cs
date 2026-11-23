using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Identity.DTOs;
using Logistics.Server.Domain.Entities.Identity;
using Logistics.Server.Infrastructure.Persistence;
using Logistics.Shared.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Logistics.Server.IntegrationTests;

public class AuthorizationIntegrationTests : IClassFixture<WebApplicationFactory<Logistics.Server.API.Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Logistics.Server.API.Program> _factory;
    private readonly string _dbName;

    public AuthorizationIntegrationTests(WebApplicationFactory<Logistics.Server.API.Program> factory)
    {
        _dbName = "AuthzTestDb_" + Guid.NewGuid().ToString("N");

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptors = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType.Name.Contains("DbContextOptions") ||
                    d.ServiceType == typeof(ApplicationDbContext)).ToList();

                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase(_dbName));
            });
        });

        _client = _factory.CreateClient();
    }

    private async Task<string> GetAccessTokenForUserAsync(string username, string roleCode)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasherService>();

        // Ensure roles & permissions are seeded
        var seeder = scope.ServiceProvider.GetRequiredService<Logistics.Server.Application.Modules.Identity.Services.IIdentityDataSeeder>();
        await seeder.SeedAsync();

        var password = "StrongPassword2026!";
        var user = await context.UserAccounts.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
        {
            var role = await context.Roles.FirstOrDefaultAsync(r => r.Code == roleCode);
            user = new UserAccount
            {
                Username = username,
                Email = $"{username}@logistics.vn",
                PasswordHash = hasher.HashPassword(password),
                SecurityStamp = Guid.NewGuid().ToString("N"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            if (role != null)
            {
                user.UserRoles.Add(new UserRole
                {
                    RoleId = role.Id,
                    AssignedAt = DateTime.UtcNow,
                    AssignedBy = "Setup"
                });
            }

            await context.UserAccounts.AddAsync(user);
            await context.SaveChangesAsync();
        }

        var loginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(username, password));
        var loginBody = await loginRes.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        return loginBody!.Data!.AccessToken;
    }

    [Fact]
    public async Task AdminEndpoint_WithoutToken_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/admin/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminEndpoint_WithStandardStaffRole_Returns403Forbidden()
    {
        // Arrange - Customer service staff does NOT have identity.users.read permission
        var token = await GetAccessTokenForUserAsync("cs_staff_" + Guid.NewGuid().ToString("N")[..6], AppRoles.CustomerService);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/users");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminEndpoint_WithSuperAdminRole_Returns200Ok()
    {
        // Arrange - SuperAdmin has all permissions
        var token = await GetAccessTokenForUserAsync("admin_" + Guid.NewGuid().ToString("N")[..6], AppRoles.SuperAdmin);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/users");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AdminUserManagement_CreateUser_ThenGetUser_Succeeds()
    {
        // Arrange
        var adminToken = await GetAccessTokenForUserAsync("superadmin_" + Guid.NewGuid().ToString("N")[..6], AppRoles.SuperAdmin);

        var targetUsername = "new_operator_" + Guid.NewGuid().ToString("N")[..6];
        var createRequest = new CreateUserRequest(
            Username: targetUsername,
            Email: $"{targetUsername}@logistics.vn",
            Password: "SecurePassword2026!",
            Roles: new[] { AppRoles.WarehouseStaff }
        );

        var postMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/users")
        {
            Content = JsonContent.Create(createRequest)
        };
        postMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act - Create User
        var postResponse = await _client.SendAsync(postMsg);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdBody = await postResponse.Content.ReadFromJsonAsync<ApiResponse<UserAccountDto>>();
        createdBody.Should().NotBeNull();
        createdBody!.Data!.Username.Should().Be(targetUsername);
        createdBody.Data.Roles.Should().Contain(AppRoles.WarehouseStaff);

        // Act - Get User by PublicId
        var getMsg = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/admin/users/{createdBody.Data.PublicId}");
        getMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var getResponse = await _client.SendAsync(getMsg);

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetchedBody = await getResponse.Content.ReadFromJsonAsync<ApiResponse<UserAccountDto>>();
        fetchedBody!.Data!.Email.Should().Be($"{targetUsername}@logistics.vn");
    }

    [Fact]
    public async Task AdminUserManagement_PreventSelfDeactivation()
    {
        // Arrange
        var adminUsername = "self_deact_admin_" + Guid.NewGuid().ToString("N")[..6];
        var adminToken = await GetAccessTokenForUserAsync(adminUsername, AppRoles.SuperAdmin);

        // Get own profile to get PublicId
        var meMsg = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        meMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var meRes = await _client.SendAsync(meMsg);
        var meBody = await meRes.Content.ReadFromJsonAsync<ApiResponse<UserProfileDto>>();
        var adminPublicId = meBody!.Data!.PublicId;

        // Act - Attempt self deactivation
        var statusMsg = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/admin/users/{adminPublicId}/status")
        {
            Content = JsonContent.Create(new UpdateUserStatusRequest(IsActive: false))
        };
        statusMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var statusRes = await _client.SendAsync(statusMsg);

        // Assert
        statusRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await statusRes.Content.ReadFromJsonAsync<ApiResponse>();
        body!.Message.Should().Contain("cannot deactivate their own active user account");
    }

    [Fact]
    public async Task AdminResetPassword_AllowsUserToLoginWithNewPassword()
    {
        // Arrange
        var adminToken = await GetAccessTokenForUserAsync("pwd_admin_" + Guid.NewGuid().ToString("N")[..6], AppRoles.SuperAdmin);

        var username = "target_reset_" + Guid.NewGuid().ToString("N")[..6];
        var createRequest = new CreateUserRequest(
            Username: username,
            Email: $"{username}@logistics.vn",
            Password: "OldInitialPassword2026!"
        );

        var postMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/users")
        {
            Content = JsonContent.Create(createRequest)
        };
        postMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var postResponse = await _client.SendAsync(postMsg);
        var createdUser = (await postResponse.Content.ReadFromJsonAsync<ApiResponse<UserAccountDto>>())!.Data!;

        // Act - Admin Reset Password
        var newAdminPassword = "AdminResetStrongPass2026!";
        var resetMsg = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/users/{createdUser.PublicId}/reset-password")
        {
            Content = JsonContent.Create(new AdminResetPasswordRequest(newAdminPassword))
        };
        resetMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var resetRes = await _client.SendAsync(resetMsg);

        resetRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert - Target user can log in with new password
        var loginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(username, newAdminPassword));
        loginRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetRolesAndPermissions_WithAdminToken_ReturnsMetadata()
    {
        // Arrange
        var adminToken = await GetAccessTokenForUserAsync("meta_admin_" + Guid.NewGuid().ToString("N")[..6], AppRoles.SuperAdmin);

        // 1. Roles
        var rolesReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/roles");
        rolesReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var rolesRes = await _client.SendAsync(rolesReq);

        rolesRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var rolesBody = await rolesRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<RoleDto>>>();
        rolesBody!.Data.Should().Contain(r => r.Code == AppRoles.SuperAdmin);
        rolesBody.Data.Should().Contain(r => r.Code == AppRoles.BranchManager);

        // 2. Permissions
        var permsReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/permissions");
        permsReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var permsRes = await _client.SendAsync(permsReq);

        permsRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var permsBody = await permsRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<PermissionDto>>>();
        permsBody!.Data.Should().Contain(p => p.Code == AppPermissions.Identity.UsersRead);
        permsBody.Data.Should().Contain(p => p.Code == AppPermissions.Shipping.ShipmentsRead);
    }
}
