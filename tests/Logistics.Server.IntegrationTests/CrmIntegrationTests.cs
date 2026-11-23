using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Crm.DTOs;
using Logistics.Server.Application.Modules.Identity.DTOs;
using Logistics.Server.Domain.Entities.Configuration;
using Logistics.Server.Domain.Entities.Crm;
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

public class CrmIntegrationTests : IClassFixture<WebApplicationFactory<Logistics.Server.API.Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Logistics.Server.API.Program> _factory;
    private readonly string _dbName;

    public CrmIntegrationTests(WebApplicationFactory<Logistics.Server.API.Program> factory)
    {
        _dbName = "CrmTestDb_" + Guid.NewGuid().ToString("N");

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

            var country = new Country
            {
                Code = "VN",
                Alpha3Code = "VNM",
                Name = "Vietnam",
                PhonePrefix = "+84",
                IsActive = true
            };
            await context.Countries.AddAsync(country);
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

    #region Customer Master Tests
    [Fact]
    public async Task CreateCustomer_WithValidData_ShouldSucceedAndRecordAudit()
    {
        var token = await GetTokenAsync("admin_crm_user", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        long branchId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            branchId = (await context.Branches.FirstAsync(b => b.Code == "BR-HAN-01")).Id;
        }

        var request = new CreateCustomerRequest(
            BranchId: branchId,
            CustomerCode: "CUST-INT-001",
            CustomerType: "Corporate",
            Name: "Global Trade Logistics Corp",
            TradeName: "Global Express",
            TaxCode: "0108877665",
            Email: "contact@globaltrade.vn",
            PhoneNumber: "+84912345678",
            CreditLimit: 100000000,
            CreditTermDays: 30,
            PaymentType: "PostpaidMonthly"
        );

        var res = await _client.PostAsJsonAsync("/api/v1/customers", request);
        res.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await res.Content.ReadFromJsonAsync<ApiResponse<CustomerDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data!.CustomerCode.Should().Be("CUST-INT-001");
        body.Data.Name.Should().Be("Global Trade Logistics Corp");

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var audit = await context.AuditLogs.FirstOrDefaultAsync(a => a.TableName == "Customers" && a.Action == "Create");
            audit.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task CreateCustomer_WithDuplicateCode_ShouldReturn409Conflict()
    {
        var token = await GetTokenAsync("admin_crm_user", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateCustomerRequest(
            BranchId: null,
            CustomerCode: "CUST-DUP-01",
            CustomerType: "Individual",
            Name: "Original Customer",
            TradeName: null,
            TaxCode: null,
            Email: "original@gmail.com",
            PhoneNumber: "+84911112222"
        );

        var res1 = await _client.PostAsJsonAsync("/api/v1/customers", request);
        res1.StatusCode.Should().Be(HttpStatusCode.Created);

        var res2 = await _client.PostAsJsonAsync("/api/v1/customers", request);
        res2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetCustomer_ByIdOrCode_ShouldReturnCustomer()
    {
        var token = await GetTokenAsync("admin_crm_user", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateCustomerRequest(
            BranchId: null,
            CustomerCode: "CUST-GET-01",
            CustomerType: "Individual",
            Name: "Lookup Customer",
            TradeName: null,
            TaxCode: null,
            Email: "lookup@gmail.com",
            PhoneNumber: "+84933334444"
        );

        var createRes = await _client.PostAsJsonAsync("/api/v1/customers", request);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await createRes.Content.ReadFromJsonAsync<ApiResponse<CustomerDto>>())!.Data!;

        // By CustomerCode
        var getByCodeRes = await _client.GetAsync("/api/v1/customers/CUST-GET-01");
        getByCodeRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var bodyByCode = await getByCodeRes.Content.ReadFromJsonAsync<ApiResponse<CustomerDto>>();
        bodyByCode!.Data!.Id.Should().Be(created.Id);

        // By ID
        var getByIdRes = await _client.GetAsync($"/api/v1/customers/{created.Id}");
        getByIdRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // By PublicId
        var getByGuidRes = await _client.GetAsync($"/api/v1/customers/{created.PublicId}");
        getByGuidRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateCustomer_WithValidData_ShouldUpdateRecord()
    {
        var token = await GetTokenAsync("admin_crm_user", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createReq = new CreateCustomerRequest(
            BranchId: null,
            CustomerCode: "CUST-UPD-01",
            CustomerType: "Individual",
            Name: "Before Update",
            TradeName: null,
            TaxCode: null,
            Email: "before@test.com",
            PhoneNumber: "+84955556666"
        );

        await _client.PostAsJsonAsync("/api/v1/customers", createReq);

        var updateReq = new UpdateCustomerRequest(
            CustomerType: "Corporate",
            Name: "After Update Corp",
            TradeName: "After DBA",
            TaxCode: "0109999888",
            Email: "after@test.com",
            PhoneNumber: "+84977778888",
            CreditLimit: 50000000,
            CreditTermDays: 15,
            PaymentType: "PostpaidMonthly",
            AccountManagerEmployeeId: null
        );

        var res = await _client.PutAsJsonAsync("/api/v1/customers/CUST-UPD-01", updateReq);
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await res.Content.ReadFromJsonAsync<ApiResponse<CustomerDto>>();
        body!.Data!.Name.Should().Be("After Update Corp");
        body.Data.CustomerType.Should().Be("Corporate");
    }

    [Fact]
    public async Task UpdateCustomerStatus_DeactivateAndReactivate_ShouldToggleStatus()
    {
        var token = await GetTokenAsync("admin_crm_user", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createReq = new CreateCustomerRequest(
            BranchId: null,
            CustomerCode: "CUST-STAT-01",
            CustomerType: "Individual",
            Name: "Status Customer",
            TradeName: null,
            TaxCode: null,
            Email: "stat@test.com",
            PhoneNumber: "+84988889999"
        );

        await _client.PostAsJsonAsync("/api/v1/customers", createReq);

        // Deactivate
        var deactRes = await _client.PatchAsJsonAsync("/api/v1/customers/CUST-STAT-01/status", new UpdateCustomerStatusRequest(false, "Payment overdue"));
        deactRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var getDeact = await _client.GetAsync("/api/v1/customers/CUST-STAT-01");
        (await getDeact.Content.ReadFromJsonAsync<ApiResponse<CustomerDto>>())!.Data!.IsActive.Should().BeFalse();

        // Reactivate
        var reactRes = await _client.PatchAsJsonAsync("/api/v1/customers/CUST-STAT-01/status", new UpdateCustomerStatusRequest(true, "Balance cleared"));
        reactRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var getReact = await _client.GetAsync("/api/v1/customers/CUST-STAT-01");
        (await getReact.Content.ReadFromJsonAsync<ApiResponse<CustomerDto>>())!.Data!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetOverview_AsSuperAdmin_ShouldReturnAccurateMetrics()
    {
        var token = await GetTokenAsync("admin_crm_user", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.PostAsJsonAsync("/api/v1/customers", new CreateCustomerRequest(
            BranchId: null,
            CustomerCode: "CUST-OVERVIEW-01",
            CustomerType: "Corporate",
            Name: "Overview Customer Corp",
            TradeName: null,
            TaxCode: null,
            Email: "overview@test.com",
            PhoneNumber: "+84901112233"
        ));

        var res = await _client.GetAsync("/api/v1/customers/overview");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await res.Content.ReadFromJsonAsync<ApiResponse<CustomerOverviewDto>>();
        body.Should().NotBeNull();
        body!.Data!.TotalCustomers.Should().BeGreaterThanOrEqualTo(1);
        body.Data.ScopeType.Should().Be("Global");
    }
    #endregion

    #region Branch Scoped Access Tests
    [Fact]
    public async Task BranchAuthorization_BranchScopedUser_CannotAccessOtherBranchCustomer()
    {
        await SeedBaseDataAsync();

        long branchAId;
        long branchBId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            branchAId = (await context.Branches.FirstAsync(b => b.Code == "BR-HAN-01")).Id;
            branchBId = (await context.Branches.FirstAsync(b => b.Code == "BR-SGN-01")).Id;

            // Create customer under Branch B
            var custB = new Customer
            {
                BranchId = branchBId,
                CustomerCode = "CUST-BRANCH-B-01",
                CustomerType = "Individual",
                Name = "Saigon Exclusive Customer",
                Email = "sgn@customer.vn",
                PhoneNumber = "+84900001111",
                IsActive = true
            };
            await context.Customers.AddAsync(custB);
            await context.SaveChangesAsync();
        }

        // Login as Branch Manager for Branch A only
        var tokenBranchA = await GetTokenAsync("mgr_branch_crm_a", AppRoles.BranchManager, branchAId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenBranchA);

        // Attempting to access customer in Branch B -> 403 Forbidden
        var res = await _client.GetAsync("/api/v1/customers/CUST-BRANCH-B-01");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
    #endregion

    #region Customer Contacts Tests
    [Fact]
    public async Task CustomerContacts_CreateAndSetPrimary_ShouldEnforceSinglePrimary()
    {
        var token = await GetTokenAsync("admin_crm_user", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var customerCode = "CUST-CONT-01";
        await _client.PostAsJsonAsync("/api/v1/customers", new CreateCustomerRequest(
            BranchId: null,
            CustomerCode: customerCode,
            CustomerType: "Corporate",
            Name: "Contact Test Company",
            TradeName: null,
            TaxCode: null,
            Email: "contact.test@company.vn",
            PhoneNumber: "+84911223344"
        ));

        // Create Contact 1 as Primary
        var contact1Res = await _client.PostAsJsonAsync($"/api/v1/customers/{customerCode}/contacts", new CreateCustomerContactRequest(
            ContactName: "Primary Contact A",
            PhoneNumber: "+84911111111",
            JobTitle: "Director",
            IsPrimary: true
        ));
        contact1Res.StatusCode.Should().Be(HttpStatusCode.Created);
        var contact1 = (await contact1Res.Content.ReadFromJsonAsync<ApiResponse<CustomerContactDto>>())!.Data!;
        contact1.IsPrimary.Should().BeTrue();

        // Create Contact 2 as Secondary
        var contact2Res = await _client.PostAsJsonAsync($"/api/v1/customers/{customerCode}/contacts", new CreateCustomerContactRequest(
            ContactName: "Secondary Contact B",
            PhoneNumber: "+84922222222",
            JobTitle: "Manager",
            IsPrimary: false
        ));
        contact2Res.StatusCode.Should().Be(HttpStatusCode.Created);
        var contact2 = (await contact2Res.Content.ReadFromJsonAsync<ApiResponse<CustomerContactDto>>())!.Data!;
        contact2.IsPrimary.Should().BeFalse();

        // Set Contact 2 as Primary -> Contact 1 must automatically lose primary status
        var setPrimaryRes = await _client.PatchAsync($"/api/v1/customers/{customerCode}/contacts/{contact2.Id}/primary", null);
        setPrimaryRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var listRes = await _client.GetAsync($"/api/v1/customers/{customerCode}/contacts");
        var contacts = (await listRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<CustomerContactDto>>>())!.Data!;

        contacts.First(c => c.Id == contact2.Id).IsPrimary.Should().BeTrue();
        contacts.First(c => c.Id == contact1.Id).IsPrimary.Should().BeFalse();
    }
    #endregion

    #region Customer Addresses Tests
    [Fact]
    public async Task CustomerAddresses_CreateAndSetDefault_ShouldEnforceSingleDefaultPerType()
    {
        var token = await GetTokenAsync("admin_crm_user", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var customerCode = "CUST-ADDR-01";
        await _client.PostAsJsonAsync("/api/v1/customers", new CreateCustomerRequest(
            BranchId: null,
            CustomerCode: customerCode,
            CustomerType: "Corporate",
            Name: "Address Test Company",
            TradeName: null,
            TaxCode: null,
            Email: "addr.test@company.vn",
            PhoneNumber: "+84933445566"
        ));

        // Create Address 1 as Default Billing
        var addr1Res = await _client.PostAsJsonAsync($"/api/v1/customers/{customerCode}/addresses", new CreateCustomerAddressRequest(
            AddressType: "Billing",
            ContactName: "Finance Dept",
            PhoneNumber: "+842411223344",
            AddressLine1: "100 Kim Ma St",
            IsDefault: true
        ));
        addr1Res.StatusCode.Should().Be(HttpStatusCode.Created);
        var addr1 = (await addr1Res.Content.ReadFromJsonAsync<ApiResponse<CustomerAddressDto>>())!.Data!;
        addr1.IsDefault.Should().BeTrue();

        // Create Address 2 as Default Billing -> Address 1 must lose default flag
        var addr2Res = await _client.PostAsJsonAsync($"/api/v1/customers/{customerCode}/addresses", new CreateCustomerAddressRequest(
            AddressType: "Billing",
            ContactName: "New Finance Tower",
            PhoneNumber: "+842499887766",
            AddressLine1: "200 Tran Duy Hung St",
            IsDefault: true
        ));
        addr2Res.StatusCode.Should().Be(HttpStatusCode.Created);
        var addr2 = (await addr2Res.Content.ReadFromJsonAsync<ApiResponse<CustomerAddressDto>>())!.Data!;
        addr2.IsDefault.Should().BeTrue();

        var listRes = await _client.GetAsync($"/api/v1/customers/{customerCode}/addresses?addressType=Billing");
        var addresses = (await listRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<CustomerAddressDto>>>())!.Data!;

        addresses.First(a => a.Id == addr2.Id).IsDefault.Should().BeTrue();
        addresses.First(a => a.Id == addr1.Id).IsDefault.Should().BeFalse();
    }
    #endregion

    #region Customer Contracts Tests
    [Fact]
    public async Task CustomerContracts_CreateAndTransitionStatus_ShouldSucceed()
    {
        var token = await GetTokenAsync("admin_crm_user", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var customerCode = "CUST-CTR-01";
        await _client.PostAsJsonAsync("/api/v1/customers", new CreateCustomerRequest(
            BranchId: null,
            CustomerCode: customerCode,
            CustomerType: "Corporate",
            Name: "Contract Test Enterprise",
            TradeName: null,
            TaxCode: null,
            Email: "contract.test@enterprise.vn",
            PhoneNumber: "+84944556677"
        ));

        var contractReq = new CreateCustomerContractRequest(
            ContractNumber: "CTR-VN-2026-001",
            Title: "Enterprise Annual Service Level Agreement",
            StartDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
            EndDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(355)),
            Status: "Draft",
            DiscountRate: 18.5m,
            PaymentTermDays: 45
        );

        var createRes = await _client.PostAsJsonAsync($"/api/v1/customers/{customerCode}/contracts", contractReq);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var contract = (await createRes.Content.ReadFromJsonAsync<ApiResponse<CustomerContractDto>>())!.Data!;
        contract.Status.Should().Be("Draft");

        // Transition to Active
        var activeRes = await _client.PatchAsJsonAsync($"/api/v1/customers/{customerCode}/contracts/{contract.Id}/status", new UpdateCustomerContractStatusRequest("Active", "Signed by CEO"));
        activeRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var getRes = await _client.GetAsync($"/api/v1/customers/{customerCode}/contracts/{contract.Id}");
        var updated = (await getRes.Content.ReadFromJsonAsync<ApiResponse<CustomerContractDto>>())!.Data!;
        updated.Status.Should().Be("Active");
    }

    [Fact]
    public async Task CustomerContracts_CreateDuplicateContractNumber_ShouldReturn409Conflict()
    {
        var token = await GetTokenAsync("admin_crm_user", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var customerCode = "CUST-CTR-02";
        await _client.PostAsJsonAsync("/api/v1/customers", new CreateCustomerRequest(
            BranchId: null,
            CustomerCode: customerCode,
            CustomerType: "Corporate",
            Name: "Duplicate Contract Corp",
            TradeName: null,
            TaxCode: null,
            Email: "dup.ctr@corp.vn",
            PhoneNumber: "+84955667788"
        ));

        var contractReq = new CreateCustomerContractRequest(
            ContractNumber: "CTR-UNIQUE-999",
            Title: "First Agreement",
            StartDate: new DateOnly(2026, 1, 1),
            EndDate: new DateOnly(2026, 12, 31)
        );

        var res1 = await _client.PostAsJsonAsync($"/api/v1/customers/{customerCode}/contracts", contractReq);
        res1.StatusCode.Should().Be(HttpStatusCode.Created);

        var res2 = await _client.PostAsJsonAsync($"/api/v1/customers/{customerCode}/contracts", contractReq);
        res2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
    #endregion

    #region Customer Pricing Rules Tests
    [Fact]
    public async Task CustomerPricingRules_CreateAndGetApplicable_ShouldReturnRulesSortedByPriority()
    {
        var token = await GetTokenAsync("admin_crm_user", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var customerCode = "CUST-PRICE-01";
        await _client.PostAsJsonAsync("/api/v1/customers", new CreateCustomerRequest(
            BranchId: null,
            CustomerCode: customerCode,
            CustomerType: "Corporate",
            Name: "Pricing Rule Test Co",
            TradeName: null,
            TaxCode: null,
            Email: "price.test@co.vn",
            PhoneNumber: "+84966778899"
        ));

        // Create Rule Priority 2
        await _client.PostAsJsonAsync($"/api/v1/customers/{customerCode}/pricing-rules", new CreateCustomerPricingRuleRequest(
            Name: "Standard Volume 10%",
            EffectiveFrom: DateTime.UtcNow.AddDays(-5),
            EffectiveTo: DateTime.UtcNow.AddDays(30),
            DiscountPercent: 10,
            Priority: 2
        ));

        // Create Rule Priority 1 (Higher precedence)
        await _client.PostAsJsonAsync($"/api/v1/customers/{customerCode}/pricing-rules", new CreateCustomerPricingRuleRequest(
            Name: "VIP Campaign 25%",
            EffectiveFrom: DateTime.UtcNow.AddDays(-2),
            EffectiveTo: DateTime.UtcNow.AddDays(15),
            DiscountPercent: 25,
            Priority: 1
        ));

        // Query applicable
        var applicableRes = await _client.GetAsync($"/api/v1/customers/{customerCode}/pricing-rules/applicable");
        applicableRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var rules = (await applicableRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<CustomerPricingRuleDto>>>())!.Data!;
        rules.Should().HaveCount(2);
        rules[0].Priority.Should().Be(1);
        rules[0].Name.Should().Be("VIP Campaign 25%");
        rules[1].Priority.Should().Be(2);
        rules[1].Name.Should().Be("Standard Volume 10%");
    }
    #endregion
}
