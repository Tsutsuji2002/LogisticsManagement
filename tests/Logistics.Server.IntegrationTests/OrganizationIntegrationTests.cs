using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Identity.DTOs;
using Logistics.Server.Application.Modules.Organization.DTOs;
using Logistics.Server.Domain.Entities.Identity;
using Logistics.Server.Domain.Entities.Organization;
using Logistics.Server.Infrastructure.Persistence;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Logistics.Server.IntegrationTests;

public class OrganizationIntegrationTests : IClassFixture<WebApplicationFactory<Logistics.Server.API.Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Logistics.Server.API.Program> _factory;
    private readonly string _dbName;

    public OrganizationIntegrationTests(WebApplicationFactory<Logistics.Server.API.Program> factory)
    {
        _dbName = "OrgTestDb_" + Guid.NewGuid().ToString("N");

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

    private async Task SeedBaseDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<Logistics.Server.Application.Modules.Identity.Services.IIdentityDataSeeder>();
        await seeder.SeedAsync();

        if (!await context.Companies.AnyAsync())
        {
            var company = new Company
            {
                Code = "LOG-VN",
                LegalName = "Logistics Vietnam Enterprise",
                TradingName = "Logistics VN",
                TaxNumber = "0109998888",
                Email = "contact@logistics.vn",
                Phone = "+84-24-11223344",
                IsActive = true
            };
            await context.Companies.AddAsync(company);
            await context.SaveChangesAsync();

            var branchA = new Branch
            {
                CompanyId = company.Id,
                Code = "BR-HAN-01",
                Name = "Hanoi Central Hub",
                BranchType = "Headquarters",
                Phone = "+84-24-33445566",
                Email = "hanoi@logistics.vn",
                IsActive = true
            };

            var branchB = new Branch
            {
                CompanyId = company.Id,
                Code = "BR-SGN-01",
                Name = "Saigon South Hub",
                BranchType = "RegionalOffice",
                Phone = "+84-28-77889900",
                Email = "saigon@logistics.vn",
                IsActive = true
            };

            await context.Branches.AddRangeAsync(branchA, branchB);
            await context.SaveChangesAsync();
        }
    }

    private async Task<string> GetTokenAsync(string username, string roleCode, long? branchId = null)
    {
        await SeedBaseDataAsync();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasherService>();

        var password = "StrongPassword2026!";
        var user = await context.UserAccounts
            .Include(u => u.UserRoles)
            .Include(u => u.BranchAccesses)
            .FirstOrDefaultAsync(u => u.Username == username);

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
                user.UserRoles.Add(new UserRole { RoleId = role.Id, AssignedAt = DateTime.UtcNow, AssignedBy = "Setup" });
            }

            if (branchId.HasValue)
            {
                user.BranchAccesses.Add(new UserBranchAccess
                {
                    BranchId = branchId.Value,
                    IsDefault = true,
                    GrantedBy = "Setup",
                    GrantedAt = DateTime.UtcNow
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
    public async Task GetCompany_AsSuperAdmin_ShouldReturnCompany()
    {
        var token = await GetTokenAsync("admin_org_user", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await _client.GetAsync("/api/v1/organization/company");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await res.Content.ReadFromJsonAsync<ApiResponse<CompanyDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data!.Code.Should().Be("LOG-VN");
    }

    [Fact]
    public async Task UpdateCompany_WithValidData_ShouldUpdateAndRecordAudit()
    {
        var token = await GetTokenAsync("admin_org_user", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new UpdateCompanyRequest(
            LegalName: "Logistics Vietnam Enterprise Updated",
            TradingName: "Logistics VN Express",
            TaxNumber: "0109998888",
            Email: "updated@logistics.vn",
            Phone: "+84-24-99999999",
            Website: "https://logistics.vn"
        );

        var res = await _client.PutAsJsonAsync("/api/v1/organization/company", request);
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await res.Content.ReadFromJsonAsync<ApiResponse<CompanyDto>>();
        body!.Data!.LegalName.Should().Be("Logistics Vietnam Enterprise Updated");

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var audit = await context.AuditLogs.FirstOrDefaultAsync(a => a.TableName == "Companies" && a.Action == "Update");
        audit.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateBranch_WithDuplicateCode_ShouldReturn409Conflict()
    {
        var token = await GetTokenAsync("admin_org_user", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateBranchRequest(
            CompanyId: 1,
            Code: "BR-HAN-01",
            Name: "Duplicate Hanoi Hub",
            BranchType: "Headquarters",
            Phone: "0123456789"
        );

        var res = await _client.PostAsJsonAsync("/api/v1/organization/branches", request);
        res.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeactivateBranch_WithActiveEmployees_ShouldReturn409Conflict()
    {
        var token = await GetTokenAsync("admin_org_user", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var branch = await context.Branches.FirstAsync(b => b.Code == "BR-HAN-01");

            var emp = new Employee
            {
                BranchId = branch.Id,
                Code = "EMP-TEST-DEACT",
                FirstName = "Test",
                LastName = "User",
                FullName = "Test User",
                Email = "test.deact@logistics.vn",
                PhoneNumber = "+84912345678",
                JobTitle = "Staff",
                HireDate = new DateOnly(2023, 1, 1),
                Status = "Active"
            };
            await context.Employees.AddAsync(emp);
            await context.SaveChangesAsync();
        }

        var res = await _client.PatchAsJsonAsync("/api/v1/organization/branches/BR-HAN-01/status", new UpdateBranchStatusRequest(false, "Deactivation test"));
        res.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateDepartment_AndPreventDuplicateCodeInBranch()
    {
        var token = await GetTokenAsync("admin_org_user", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var branch = await context.Branches.FirstAsync(b => b.Code == "BR-HAN-01");

        var createReq = new CreateDepartmentRequest(branch.Id, "DEP-DISP", "Fleet Dispatch");
        var res1 = await _client.PostAsJsonAsync("/api/v1/organization/departments", createReq);
        res1.StatusCode.Should().Be(HttpStatusCode.Created);

        var res2 = await _client.PostAsJsonAsync("/api/v1/organization/departments", createReq);
        res2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateEmployee_ShouldCreateRecordWithoutUserAccount()
    {
        var token = await GetTokenAsync("admin_org_user", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var branch = await context.Branches.FirstAsync(b => b.Code == "BR-HAN-01");

        var request = new CreateEmployeeRequest(
            BranchId: branch.Id,
            DepartmentId: null,
            Code: "EMP-VAN-01",
            FirstName: "Tran",
            LastName: "Van B",
            Email: "b.tran@logistics.vn",
            PhoneNumber: "+84988112233",
            DateOfBirth: new DateOnly(1995, 3, 20),
            Gender: "Male",
            JobTitle: "Courier",
            HireDate: new DateOnly(2024, 2, 1)
        );

        var res = await _client.PostAsJsonAsync("/api/v1/employees", request);
        res.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await res.Content.ReadFromJsonAsync<ApiResponse<EmployeeDto>>();
        body!.Data!.HasUserAccount.Should().BeFalse();

        var empInDb = await context.Employees.Include(e => e.UserAccount).FirstOrDefaultAsync(e => e.Code == "EMP-VAN-01");
        empInDb.Should().NotBeNull();
        empInDb!.UserAccount.Should().BeNull();
    }

    [Fact]
    public async Task CreateEmployeeAccount_ShouldProvisionUserAndPreventDuplicate()
    {
        var token = await GetTokenAsync("admin_org_user", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var branch = await context.Branches.FirstAsync(b => b.Code == "BR-HAN-01");

            var emp = new Employee
            {
                BranchId = branch.Id,
                Code = "EMP-ACC-01",
                FirstName = "Le",
                LastName = "Van C",
                FullName = "Le Van C",
                Email = "c.le@logistics.vn",
                PhoneNumber = "+84911223344",
                JobTitle = "Warehouse Operator",
                HireDate = new DateOnly(2023, 5, 10),
                Status = "Active"
            };
            await context.Employees.AddAsync(emp);
            await context.SaveChangesAsync();
        }

        var accountReq = new CreateEmployeeAccountRequest(
            Username: "levanc",
            Password: "SecurePassword2026!",
            RoleCodes: [AppRoles.WarehouseStaff]
        );

        var res1 = await _client.PostAsJsonAsync("/api/v1/employees/EMP-ACC-01/account", accountReq);
        res1.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await res1.Content.ReadFromJsonAsync<ApiResponse<EmployeeAccountDto>>();
        body!.Data!.HasAccount.Should().BeTrue();
        body.Data!.Username.Should().Be("levanc");

        // Duplicate account creation attempt must fail with 409 Conflict
        var res2 = await _client.PostAsJsonAsync("/api/v1/employees/EMP-ACC-01/account", accountReq);
        res2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task EmployeeStatusTransitionToTerminated_ShouldDeactivateLinkedAccountAndRevokeSessions()
    {
        var token = await GetTokenAsync("admin_org_user", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        long empId;
        long userId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var branch = await context.Branches.FirstAsync(b => b.Code == "BR-HAN-01");

            var emp = new Employee
            {
                BranchId = branch.Id,
                Code = "EMP-TERM-01",
                FirstName = "Pham",
                LastName = "Van D",
                FullName = "Pham Van D",
                Email = "d.pham@logistics.vn",
                PhoneNumber = "+84933445566",
                JobTitle = "Driver",
                HireDate = new DateOnly(2022, 1, 1),
                Status = "Active"
            };
            await context.Employees.AddAsync(emp);
            await context.SaveChangesAsync();
            empId = emp.Id;

            var user = new UserAccount
            {
                EmployeeId = emp.Id,
                Username = "phamvand",
                Email = emp.Email,
                PasswordHash = "hash",
                SecurityStamp = Guid.NewGuid().ToString("N"),
                IsActive = true
            };
            user.RefreshTokens.Add(new RefreshToken
            {
                TokenHash = "dummyhash",
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            });
            user.UserSessions.Add(new UserSession
            {
                SessionTokenHash = "dummysession",
                ExpiresAt = DateTime.UtcNow.AddHours(8),
                IsTerminated = false
            });

            await context.UserAccounts.AddAsync(user);
            await context.SaveChangesAsync();
            userId = user.Id;
        }

        var res = await _client.PatchAsJsonAsync("/api/v1/employees/EMP-TERM-01/status", new UpdateEmployeeStatusRequest("Terminated", "Employment ended"));
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await context.UserAccounts
                .Include(u => u.RefreshTokens)
                .Include(u => u.UserSessions)
                .FirstAsync(u => u.Id == userId);

            user.IsActive.Should().BeFalse();
            user.RefreshTokens.All(rt => rt.IsRevoked).Should().BeTrue();
            user.UserSessions.All(s => s.IsTerminated).Should().BeTrue();
        }
    }

    [Fact]
    public async Task BranchAuthorization_BranchScopedUser_CannotAccessUnauthorizedBranch()
    {
        // Seed base data first so branches exist before querying
        await SeedBaseDataAsync();

        long branchAId;
        long branchBId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            branchAId = (await context.Branches.FirstAsync(b => b.Code == "BR-HAN-01")).Id;
            branchBId = (await context.Branches.FirstAsync(b => b.Code == "BR-SGN-01")).Id;
        }

        var tokenBranchA = await GetTokenAsync("mgr_branch_a", AppRoles.BranchManager, branchAId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenBranchA);

        // Branch A user accessing Branch A details -> 200 OK
        var resA = await _client.GetAsync($"/api/v1/organization/branches/{branchAId}");
        resA.StatusCode.Should().Be(HttpStatusCode.OK);

        // Branch A user attempting to access Branch B details -> 403 Forbidden
        var resB = await _client.GetAsync($"/api/v1/organization/branches/{branchBId}");
        resB.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task OrganizationOverview_AsSuperAdmin_ShouldReturnAccurateMetrics()
    {
        var token = await GetTokenAsync("admin_org_user", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await _client.GetAsync("/api/v1/organization/overview");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await res.Content.ReadFromJsonAsync<ApiResponse<OrganizationOverviewDto>>();
        body.Should().NotBeNull();
        body!.Data!.TotalBranches.Should().BeGreaterThanOrEqualTo(2);
        body.Data.ScopeType.Should().Be("Global");
    }

    [Fact]
    public async Task AdminUserBranches_GrantAndRevoke_ShouldUpdateUserAssignments()
    {
        // Seed a branch-scoped user first so it exists in this test's DB
        long branchAId;
        using (var setupScope = _factory.Services.CreateScope())
        {
            var setupCtx = setupScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await SeedBaseDataAsync();
            branchAId = (await setupCtx.Branches.FirstAsync(b => b.Code == "BR-HAN-01")).Id;
        }
        // Create the branch manager user via GetTokenAsync
        await GetTokenAsync("mgr_branch_a", AppRoles.BranchManager, branchAId);

        var token = await GetTokenAsync("admin_org_user", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        Guid userPublicId;
        long branchBId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await context.UserAccounts.FirstAsync(u => u.Username == "mgr_branch_a");
            userPublicId = user.PublicId;
            branchBId = (await context.Branches.FirstAsync(b => b.Code == "BR-SGN-01")).Id;
        }

        // Grant access to Branch B
        var grantRes = await _client.PostAsJsonAsync($"/api/v1/admin/users/{userPublicId}/branches", new GrantBranchAccessRequest(branchBId, false));
        grantRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Get branches
        var listRes = await _client.GetAsync($"/api/v1/admin/users/{userPublicId}/branches");
        listRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var listBody = await listRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<AuthorizedBranchDto>>>();
        listBody!.Data!.Should().Contain(b => b.BranchId == branchBId);

        // Revoke access to Branch B
        var revokeRes = await _client.DeleteAsync($"/api/v1/admin/users/{userPublicId}/branches/{branchBId}");
        revokeRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify revoked
        var listRes2 = await _client.GetAsync($"/api/v1/admin/users/{userPublicId}/branches");
        var listBody2 = await listRes2.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<AuthorizedBranchDto>>>();
        listBody2!.Data!.Should().NotContain(b => b.BranchId == branchBId);
    }
}
