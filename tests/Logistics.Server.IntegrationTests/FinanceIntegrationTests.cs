using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Finance.DTOs;
using Logistics.Server.Application.Modules.Identity.DTOs;
using Logistics.Server.Domain.Entities.Configuration;
using Logistics.Server.Domain.Entities.Crm;
using Logistics.Server.Domain.Entities.Finance;
using Logistics.Server.Domain.Entities.Identity;
using Logistics.Server.Domain.Entities.Organization;
using Logistics.Server.Domain.Entities.Shipping;
using Logistics.Server.Infrastructure.Persistence;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Logistics.Server.IntegrationTests;

public class FinanceIntegrationTests : IClassFixture<WebApplicationFactory<Logistics.Server.API.Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Logistics.Server.API.Program> _factory;
    private readonly string _dbName;

    public FinanceIntegrationTests(WebApplicationFactory<Logistics.Server.API.Program> factory)
    {
        _dbName = "FinanceTestDb_" + Guid.NewGuid().ToString("N");

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
                Code = "LOGICORP",
                LegalName = "Logistics Corporation",
                TradingName = "LogiCorp",
                TaxNumber = "0101234567",
                IsActive = true
            };
            await context.Companies.AddAsync(company);
            await context.SaveChangesAsync();

            var branch1 = new Branch
            {
                CompanyId = company.Id,
                Code = "SGN",
                Name = "Saigon Central Hub",
                BranchType = "Hub",
                Phone = "0281234567",
                IsActive = true
            };
            var branch2 = new Branch
            {
                CompanyId = company.Id,
                Code = "HAN",
                Name = "Hanoi Central Hub",
                BranchType = "Hub",
                Phone = "0241234567",
                IsActive = true
            };
            await context.Branches.AddRangeAsync(branch1, branch2);
            await context.SaveChangesAsync();

            var country = new Country
            {
                Code = "VN",
                Alpha3Code = "VNM",
                Name = "Vietnam",
                IsActive = true
            };
            await context.Countries.AddAsync(country);
            await context.SaveChangesAsync();

            var currency = new Currency
            {
                Code = "VND",
                Name = "Vietnamese Dong",
                Symbol = "₫",
                DecimalPlaces = 0,
                ExchangeRateToDefault = 1.0m,
                IsDefault = true,
                IsActive = true
            };
            await context.Currencies.AddAsync(currency);
            await context.SaveChangesAsync();

            var province1 = new Province
            {
                CountryId = country.Id,
                Code = "SG",
                Name = "Ho Chi Minh City",
                Type = "City",
                IsActive = true
            };
            var province2 = new Province
            {
                CountryId = country.Id,
                Code = "HN",
                Name = "Hanoi",
                Type = "City",
                IsActive = true
            };
            await context.Provinces.AddRangeAsync(province1, province2);
            await context.SaveChangesAsync();

            var customer = new Customer
            {
                BranchId = branch1.Id,
                CustomerCode = "CUST-FIN-001",
                CustomerType = "Corporate",
                Name = "Alpha Logistics Partner",
                Email = "billing@alpha-partner.com",
                PhoneNumber = "0908887766",
                CreditLimit = 50000000m,
                IsActive = true
            };
            await context.Customers.AddAsync(customer);
            await context.SaveChangesAsync();
        }
    }

    private async Task<string> GetTokenAsync(string username, string roleCode)
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
                Email = $"{username}@logistics.test",
                PasswordHash = hasher.HashPassword(password),
                SecurityStamp = Guid.NewGuid().ToString("N"),
                IsActive = true
            };

            if (role != null)
            {
                user.UserRoles.Add(new UserRole
                {
                    RoleId = role.Id,
                    AssignedBy = "TestSetup",
                    AssignedAt = DateTime.UtcNow
                });
            }

            var branches = await context.Branches.ToListAsync();
            foreach (var b in branches)
            {
                user.BranchAccesses.Add(new UserBranchAccess
                {
                    BranchId = b.Id,
                    IsDefault = b.Code == "SGN",
                    GrantedBy = "TestSetup",
                    GrantedAt = DateTime.UtcNow
                });
            }

            await context.UserAccounts.AddAsync(user);
            await context.SaveChangesAsync();
        }

        var loginResp = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(username, password));
        loginResp.EnsureSuccessStatusCode();
        var authResult = await loginResp.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        return authResult!.Data!.AccessToken;
    }

    [Fact]
    public async Task RateCard_And_Rules_Lifecycle_Succeeds()
    {
        var token = await GetTokenAsync("finance_admin", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        long currencyId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            currencyId = (await db.Currencies.FirstAsync(c => c.Code == "VND")).Id;
        }

        // 1. Create Rate Card
        var createCardReq = new CreateShippingRateCardRequest
        {
            Code = "RC-DOM-STD-2026",
            Name = "Domestic Standard Express 2026",
            BaseCurrencyId = currencyId,
            IsDefault = true,
            EffectiveFrom = DateTime.UtcNow.AddDays(-10)
        };

        var postResp = await _client.PostAsJsonAsync("/api/v1/finance/rate-cards", createCardReq);
        postResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var postResult = await postResp.Content.ReadFromJsonAsync<ApiResponse<ShippingRateCardDto>>();
        postResult!.Data.Should().NotBeNull();
        var cardId = postResult.Data!.Id;

        // 2. Get Rate Card By ID
        var getResp = await _client.GetAsync($"/api/v1/finance/rate-cards/{cardId}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var getResult = await getResp.Content.ReadFromJsonAsync<ApiResponse<ShippingRateCardDto>>();
        getResult!.Data!.Code.Should().Be("RC-DOM-STD-2026");

        // 3. Add Rate Rule
        var createRuleReq = new CreateShippingRateRuleRequest
        {
            ServiceType = "StandardGround",
            FromWeightKg = 0m,
            ToWeightKg = 5m,
            BaseRate = 18000m,
            AdditionalPerKgRate = 2500m,
            MinimumFee = 18000m,
            EstimatedDays = 2
        };

        var ruleResp = await _client.PostAsJsonAsync($"/api/v1/finance/rate-cards/{cardId}/rules", createRuleReq);
        ruleResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var ruleResult = await ruleResp.Content.ReadFromJsonAsync<ApiResponse<ShippingRateRuleDto>>();
        ruleResult!.Data.Should().NotBeNull();
        ruleResult.Data!.ServiceType.Should().Be("StandardGround");

        // 4. Query Rules for Card
        var rulesListResp = await _client.GetAsync($"/api/v1/finance/rate-cards/{cardId}/rules");
        rulesListResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var rulesResult = await rulesListResp.Content.ReadFromJsonAsync<ApiResponse<List<ShippingRateRuleDto>>>();
        rulesResult!.Data.Should().HaveCountGreaterOrEqualTo(1);

        // 5. Update Rate Card
        var updateCardReq = new UpdateShippingRateCardRequest
        {
            Name = "Domestic Standard Express 2026 Revised",
            BaseCurrencyId = currencyId,
            IsDefault = true,
            IsActive = true,
            EffectiveFrom = DateTime.UtcNow.AddDays(-10)
        };
        var updateResp = await _client.PutAsync($"/api/v1/finance/rate-cards/{cardId}", JsonContent.Create(updateCardReq));
        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 6. Toggle Status
        var toggleResp = await _client.PatchAsync($"/api/v1/finance/rate-cards/{cardId}/status?isActive=true", null);
        toggleResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ShippingFee_Calculation_And_ShipmentCharge_Lifecycle_Succeeds()
    {
        var token = await GetTokenAsync("finance_officer", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        long customerId;
        long currencyId;
        long shipmentId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var customer = await db.Customers.FirstAsync();
            var currency = await db.Currencies.FirstAsync();
            var branch = await db.Branches.FirstAsync();

            customerId = customer.Id;
            currencyId = currency.Id;

            // Seed a RateCard if not present
            var card = await db.ShippingRateCards.FirstOrDefaultAsync(c => c.Code == "RC-CALC-TEST");
            if (card == null)
            {
                card = new ShippingRateCard
                {
                    Code = "RC-CALC-TEST",
                    Name = "Calculation Rate Card",
                    BaseCurrencyId = currencyId,
                    IsDefault = true,
                    IsActive = true,
                    EffectiveFrom = DateTime.UtcNow.AddDays(-30)
                };
                card.Rules.Add(new ShippingRateRule
                {
                    ServiceType = "StandardGround",
                    FromWeightKg = 0m,
                    ToWeightKg = 10m,
                    BaseRate = 25000m,
                    AdditionalPerKgRate = 3000m,
                    MinimumFee = 25000m,
                    EstimatedDays = 2
                });
                await db.ShippingRateCards.AddAsync(card);
                await db.SaveChangesAsync();
            }

            // Seed a Shipment
            var shipment = new Shipment
            {
                TrackingNumber = "TRK-FIN-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
                CustomerId = customerId,
                OriginBranchId = branch.Id,
                DestinationBranchId = branch.Id,
                ServiceType = "StandardGround",
                ActualWeightKg = 4.5m,
                ChargeableWeightKg = 4.5m,
                DeclaredValue = 1000000m,
                CodAmount = 0m,
                CurrencyId = currencyId,
                ShipmentStatus = "ReadyForPickup"
            };
            await db.Shipments.AddAsync(shipment);
            await db.SaveChangesAsync();
            shipmentId = shipment.Id;
        }

        // 1. Calculate Shipping Fee
        var calcReq = new CalculateShippingFeeRequest
        {
            CustomerId = customerId,
            ServiceType = "StandardGround",
            WeightKg = 4.5m,
            DeclaredValue = 1000000m,
            CodAmount = 0m
        };

        var calcResp = await _client.PostAsJsonAsync("/api/v1/finance/shipping-fees/calculate", calcReq);
        calcResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var calcResult = await calcResp.Content.ReadFromJsonAsync<ApiResponse<ShippingFeeCalculationResultDto>>();
        calcResult!.Data.Should().NotBeNull();
        calcResult.Data!.BaseRate.Should().BeGreaterThan(0);
        calcResult.Data.TotalAmount.Should().BeGreaterThan(0);

        // 2. Add Adjustment Charge
        var adjReq = new CreateChargeAdjustmentRequest
        {
            ChargeType = "RemoteAreaSurcharge",
            Amount = 35000m,
            CurrencyId = currencyId,
            Description = "Remote delivery zone surcharge"
        };

        var adjResp = await _client.PostAsJsonAsync($"/api/v1/finance/shipping-fees/charges/{shipmentId}/adjustments", adjReq);
        adjResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var adjResult = await adjResp.Content.ReadFromJsonAsync<ApiResponse<ShipmentChargeDto>>();
        adjResult!.Data.Should().NotBeNull();
        adjResult.Data!.Amount.Should().Be(35000m);
        var chargeId = adjResult.Data.Id;

        // 3. Get Charge By ID
        var getChargeResp = await _client.GetAsync($"/api/v1/finance/shipping-fees/charges/{chargeId}");
        getChargeResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. List Charges
        var listChargesResp = await _client.GetAsync($"/api/v1/finance/shipping-fees/charges?shipmentId={shipmentId}");
        listChargesResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var listChargesResult = await listChargesResp.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<ShipmentChargeDto>>>();
        listChargesResult!.Data!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Invoice_Draft_AddItems_Issue_Cancel_Lifecycle_Succeeds()
    {
        var token = await GetTokenAsync("finance_invoicing", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        long customerId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            customerId = (await db.Customers.FirstAsync()).Id;
        }

        // 1. Create Draft Invoice
        var draftReq = new CreateInvoiceRequest
        {
            CustomerId = customerId,
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            TaxRate = 8m,
            PaymentTermsDays = 30,
            Notes = "Monthly Consolidated Billing",
            Items =
            [
                new CreateInvoiceItemRequest
                {
                    Description = "Standard express shipping charges",
                    Quantity = 2,
                    UnitPrice = 45000m
                }
            ]
        };

        var postResp = await _client.PostAsJsonAsync("/api/v1/finance/invoices", draftReq);
        postResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var postResult = await postResp.Content.ReadFromJsonAsync<ApiResponse<InvoiceDto>>();
        postResult!.Data.Should().NotBeNull();
        var invoiceId = postResult.Data!.Id;
        postResult.Data.Status.Should().Be("Draft");
        postResult.Data.Items.Should().HaveCount(1);

        // 2. Add Item to Draft Invoice
        var addItemReq = new CreateInvoiceItemRequest
        {
            Description = "Fuel surcharge supplement",
            Quantity = 1,
            UnitPrice = 10000m
        };
        var addItemResp = await _client.PostAsJsonAsync($"/api/v1/finance/invoices/{invoiceId}/items", addItemReq);
        addItemResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var addItemResult = await addItemResp.Content.ReadFromJsonAsync<ApiResponse<InvoiceDto>>();
        addItemResult!.Data!.Items.Should().HaveCount(2);

        // 3. Issue Invoice
        var issueResp = await _client.PostAsync($"/api/v1/finance/invoices/{invoiceId}/issue", null);
        issueResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var issueResult = await issueResp.Content.ReadFromJsonAsync<ApiResponse<InvoiceDto>>();
        issueResult!.Data!.Status.Should().Be("Issued");
        issueResult.Data.OutstandingAmount.Should().BeGreaterThan(0);

        // 4. Cancel Invoice
        var cancelReq = new CancelInvoiceRequest
        {
            Reason = "Customer requested re-invoicing under revised contract terms."
        };
        var cancelResp = await _client.PostAsJsonAsync($"/api/v1/finance/invoices/{invoiceId}/cancel", cancelReq);
        cancelResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var cancelResult = await cancelResp.Content.ReadFromJsonAsync<ApiResponse<InvoiceDto>>();
        cancelResult!.Data!.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task Payment_Register_Allocate_And_Reverse_Lifecycle_Succeeds()
    {
        var token = await GetTokenAsync("finance_cashier", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        long customerId;
        long currencyId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            customerId = (await db.Customers.FirstAsync()).Id;
            currencyId = (await db.Currencies.FirstAsync()).Id;
        }

        // 1. Create and Issue an Invoice for 100,000 VND
        var invoiceReq = new CreateInvoiceRequest
        {
            CustomerId = customerId,
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15)),
            TaxRate = 0m,
            Items =
            [
                new CreateInvoiceItemRequest
                {
                    Description = "Express delivery charges",
                    Quantity = 1,
                    UnitPrice = 100000m
                }
            ]
        };
        var invResp = await _client.PostAsJsonAsync("/api/v1/finance/invoices", invoiceReq);
        var invResult = await invResp.Content.ReadFromJsonAsync<ApiResponse<InvoiceDto>>();
        var invoiceId = invResult!.Data!.Id;

        await _client.PostAsync($"/api/v1/finance/invoices/{invoiceId}/issue", null);

        // 2. Register Payment for 100,000 VND
        var payReq = new RegisterPaymentRequest
        {
            CustomerId = customerId,
            CurrencyId = currencyId,
            Amount = 100000m,
            PaymentMethod = "BankTransfer",
            PaymentDate = DateTime.UtcNow,
            TransactionReference = "TXN-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            BankName = "Vietcombank",
            BankAccount = "9988776655"
        };

        var payResp = await _client.PostAsJsonAsync("/api/v1/finance/payments", payReq);
        payResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var payResult = await payResp.Content.ReadFromJsonAsync<ApiResponse<PaymentDto>>();
        payResult!.Data.Should().NotBeNull();
        var paymentId = payResult.Data!.Id;
        payResult.Data.UnallocatedAmount.Should().Be(100000m);

        // 3. Allocate Payment to Invoice
        var allocReq = new CreatePaymentAllocationRequest
        {
            InvoiceId = invoiceId,
            Amount = 100000m
        };
        var allocResp = await _client.PostAsJsonAsync($"/api/v1/finance/payments/{paymentId}/allocations", allocReq);
        allocResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var allocResult = await allocResp.Content.ReadFromJsonAsync<ApiResponse<PaymentDto>>();
        allocResult!.Data!.AllocatedAmount.Should().Be(100000m);
        allocResult.Data.UnallocatedAmount.Should().Be(0m);
        var allocationId = allocResult.Data.Allocations.First().Id;

        // 4. Reverse Allocation
        var revResp = await _client.PostAsync($"/api/v1/finance/payments/{paymentId}/allocations/{allocationId}/reverse", null);
        revResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var revResult = await revResp.Content.ReadFromJsonAsync<ApiResponse<PaymentDto>>();
        revResult!.Data!.AllocatedAmount.Should().Be(0m);
        revResult.Data.UnallocatedAmount.Should().Be(100000m);
    }

    [Fact]
    public async Task CustomerBalance_And_Ledger_Tracking_Succeeds()
    {
        var token = await GetTokenAsync("finance_auditor", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        long customerId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            customerId = (await db.Customers.FirstAsync()).Id;
        }

        // 1. Get Customer Balance
        var balResp = await _client.GetAsync($"/api/v1/finance/customer-balances/{customerId}");
        balResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var balResult = await balResp.Content.ReadFromJsonAsync<ApiResponse<CustomerBalanceDto>>();
        balResult!.Data.Should().NotBeNull();
        balResult.Data!.CustomerId.Should().Be(customerId);

        // 2. Reconcile Customer Balance
        var reconResp = await _client.PostAsync($"/api/v1/finance/customer-balances/{customerId}/reconcile", null);
        reconResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var reconResult = await reconResp.Content.ReadFromJsonAsync<ApiResponse<CustomerBalanceReconciliationDto>>();
        reconResult!.Data.Should().NotBeNull();
        reconResult.Data!.IsConsistent.Should().BeTrue();

        // 3. Query Financial Ledger Entries
        var ledgerResp = await _client.GetAsync($"/api/v1/finance/ledger/entries?customerId={customerId}");
        ledgerResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var ledgerResult = await ledgerResp.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<FinancialLedgerEntryDto>>>();
        ledgerResult!.Data.Should().NotBeNull();
    }
}
