using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Identity.DTOs;
using Logistics.Server.Domain.Entities.Identity;
using Logistics.Server.Domain.Entities.Organization;
using Logistics.Server.Infrastructure.Persistence;
using Logistics.Shared.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Logistics.Server.IntegrationTests;

public class AuthIntegrationTests : IClassFixture<WebApplicationFactory<Logistics.Server.API.Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Logistics.Server.API.Program> _factory;
    private readonly string _dbName;

    public AuthIntegrationTests(WebApplicationFactory<Logistics.Server.API.Program> factory)
    {
        _dbName = "AuthTestDb_" + Guid.NewGuid().ToString("N");

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

    private async Task<(UserAccount User, string Password)> CreateTestUserAsync(
        string username = "testuser",
        string email = "testuser@logistics.vn",
        string password = "TestPassword2026!",
        string roleCode = AppRoles.CustomerService,
        bool isActive = true)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasherService>();

        // Ensure roles & permissions are seeded
        var seeder = scope.ServiceProvider.GetRequiredService<Logistics.Server.Application.Modules.Identity.Services.IIdentityDataSeeder>();
        await seeder.SeedAsync();

        var existing = await context.UserAccounts
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Username == username);

        if (existing != null)
        {
            return (existing, password);
        }

        var role = await context.Roles.FirstOrDefaultAsync(r => r.Code == roleCode);

        var user = new UserAccount
        {
            Username = username,
            Email = email,
            PasswordHash = hasher.HashPassword(password),
            SecurityStamp = Guid.NewGuid().ToString("N"),
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };

        if (role != null)
        {
            user.UserRoles.Add(new UserRole
            {
                RoleId = role.Id,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = "TestFixture"
            });
        }

        await context.UserAccounts.AddAsync(user);
        await context.SaveChangesAsync();

        return (user, password);
    }

    [Fact]
    public async Task Login_WithValidCredentials_Returns200AndTokens()
    {
        // Arrange
        var username = "login_success_" + Guid.NewGuid().ToString("N")[..6];
        var (user, password) = await CreateTestUserAsync(username: username, email: $"{username}@logistics.vn");

        var loginRequest = new LoginRequest(username, password);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.Data.RefreshToken.Should().NotBeNullOrWhiteSpace();
        body.Data.User.Username.Should().Be(username);
        body.Data.User.Roles.Should().Contain(AppRoles.CustomerService);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_Returns401()
    {
        // Arrange
        var username = "wrong_pwd_" + Guid.NewGuid().ToString("N")[..6];
        await CreateTestUserAsync(username: username, email: $"{username}@logistics.vn");

        var loginRequest = new LoginRequest(username, "CompletelyWrongPassword123!");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse>();
        body.Should().NotBeNull();
        body!.Success.Should().BeFalse();
        body.ErrorCode.Should().Be("UNAUTHORIZED");
    }

    [Fact]
    public async Task Login_WithNonExistentUser_Returns401WithoutLeakingAccountExistence()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("non_existent_user_9999", "SomePassword123!"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse>();
        body.Should().NotBeNull();
        body!.Success.Should().BeFalse();
        body.ErrorCode.Should().Be("UNAUTHORIZED");
        body.Message.Should().Be("Invalid username or password.");
    }

    [Fact]
    public async Task Login_WithInactiveAccount_Returns401()
    {
        // Arrange
        var username = "inactive_" + Guid.NewGuid().ToString("N")[..6];
        var (user, password) = await CreateTestUserAsync(username: username, email: $"{username}@logistics.vn", isActive: false);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(username, password));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse>();
        body!.Message.Should().Contain("deactivated");
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_RotatesAndReturnsNewTokens()
    {
        // Arrange - Login first
        var username = "refresh_user_" + Guid.NewGuid().ToString("N")[..6];
        var (user, password) = await CreateTestUserAsync(username: username, email: $"{username}@logistics.vn");

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(username, password));
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        var originalRefreshToken = loginBody!.Data!.RefreshToken;

        // Act - Refresh
        var refreshResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(originalRefreshToken));

        // Assert
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshBody = await refreshResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        refreshBody.Should().NotBeNull();
        refreshBody!.Success.Should().BeTrue();
        refreshBody.Data!.AccessToken.Should().NotBeNullOrWhiteSpace();
        refreshBody.Data.RefreshToken.Should().NotBeNullOrWhiteSpace();
        refreshBody.Data.RefreshToken.Should().NotBe(originalRefreshToken, "refresh token must rotate");
    }

    [Fact]
    public async Task RefreshToken_ReuseDetection_InvalidatesAllTokensAndSessions()
    {
        // Arrange
        var username = "reuse_user_" + Guid.NewGuid().ToString("N")[..6];
        var (user, password) = await CreateTestUserAsync(username: username, email: $"{username}@logistics.vn");

        // 1. Initial Login
        var loginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(username, password));
        var loginBody = await loginRes.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        var token1 = loginBody!.Data!.RefreshToken;

        // 2. First Refresh -> token1 is rotated to token2
        var refresh1Res = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(token1));
        refresh1Res.StatusCode.Should().Be(HttpStatusCode.OK);
        var refresh1Body = await refresh1Res.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        var token2 = refresh1Body!.Data!.RefreshToken;

        // 3. Attempt to REUSE token1 (which is already rotated/revoked) -> REUSE ATTACK!
        var reuseRes = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(token1));

        // Assert reuse rejection
        reuseRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var reuseBody = await reuseRes.Content.ReadFromJsonAsync<ApiResponse>();
        reuseBody!.Message.Should().Contain("reuse detected");

        // 4. Verify token2 was also invalidated because of the reuse event!
        var refresh2Res = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(token2));
        refresh2Res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUserProfile_WithValidToken_ReturnsUserProfile()
    {
        // Arrange
        var username = "profile_user_" + Guid.NewGuid().ToString("N")[..6];
        var (user, password) = await CreateTestUserAsync(username: username, email: $"{username}@logistics.vn");

        var loginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(username, password));
        var loginBody = await loginRes.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        var accessToken = loginBody!.Data!.AccessToken;

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<UserProfileDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data!.Username.Should().Be(username);
        body.Data.Email.Should().Be($"{username}@logistics.vn");
        body.Data.Roles.Should().Contain(AppRoles.CustomerService);
    }

    [Fact]
    public async Task ChangePassword_WithValidCredentials_UpdatesPassword()
    {
        // Arrange
        var username = "pwd_change_" + Guid.NewGuid().ToString("N")[..6];
        var (user, oldPassword) = await CreateTestUserAsync(username: username, email: $"{username}@logistics.vn");

        var loginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(username, oldPassword));
        var loginBody = await loginRes.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        var accessToken = loginBody!.Data!.AccessToken;

        var newPassword = "BrandNewSecurePassword2026!";
        var changeRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/change-password")
        {
            Content = JsonContent.Create(new ChangePasswordRequest(oldPassword, newPassword))
        };
        changeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // Act
        var changeResponse = await _client.SendAsync(changeRequest);

        // Assert
        changeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify login with new password succeeds
        var newLoginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(username, newPassword));
        newLoginRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify login with old password fails
        var oldLoginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(username, oldPassword));
        oldLoginRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Sessions_CanListAndTerminateActiveSession()
    {
        // Arrange
        var username = "session_user_" + Guid.NewGuid().ToString("N")[..6];
        var (user, password) = await CreateTestUserAsync(username: username, email: $"{username}@logistics.vn");

        var loginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(username, password));
        var loginBody = await loginRes.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        var accessToken = loginBody!.Data!.AccessToken;

        // 1. Get Sessions
        var getSessionsReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/sessions");
        getSessionsReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var getSessionsRes = await _client.SendAsync(getSessionsReq);

        getSessionsRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var sessionsBody = await getSessionsRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<UserSessionDto>>>();
        sessionsBody.Should().NotBeNull();
        sessionsBody!.Data.Should().NotBeEmpty();
        var sessionId = sessionsBody.Data![0].Id;

        // 2. Terminate Session
        var deleteReq = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/auth/sessions/{sessionId}");
        deleteReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var deleteRes = await _client.SendAsync(deleteReq);

        deleteRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
