using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Logistics.Server.Application.Common.Events;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.BackgroundProcessing.DTOs;
using Logistics.Server.Application.Modules.BackgroundProcessing.Services;
using Logistics.Server.Application.Modules.Identity.DTOs;
using Logistics.Server.Application.Modules.Notification.DTOs;
using Logistics.Server.Application.Modules.Notification.Services;
using Logistics.Server.Application.Modules.Sales.DTOs;
using Logistics.Server.Domain.Entities.Configuration;
using Logistics.Server.Domain.Entities.Crm;
using Logistics.Server.Domain.Entities.Identity;
using Logistics.Server.Domain.Entities.Integration;
using Logistics.Server.Domain.Entities.Notification;
using Logistics.Server.Domain.Entities.Organization;
using Logistics.Server.Infrastructure.Persistence;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Logistics.Server.IntegrationTests;

public class NotificationAndBackgroundIntegrationTests : IClassFixture<WebApplicationFactory<Logistics.Server.API.Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Logistics.Server.API.Program> _factory;
    private readonly string _dbName;

    public NotificationAndBackgroundIntegrationTests(WebApplicationFactory<Logistics.Server.API.Program> factory)
    {
        _dbName = "NotificationBgTestDb_" + Guid.NewGuid().ToString("N");

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptors = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType.Name.Contains("DbContextOptions") ||
                    d.ServiceType == typeof(ApplicationDbContext)).ToList();

                foreach (var d in descriptors) services.Remove(d);

                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase(_dbName));
            });
        });

        _client = _factory.CreateClient();
    }

    private async Task<(long branch1Id, long branch2Id, long customerId)> SeedBaseDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<Logistics.Server.Application.Modules.Identity.Services.IIdentityDataSeeder>();
        await seeder.SeedAsync();

        if (!await context.Companies.AnyAsync())
        {
            var company = new Company { Code = "LOGI", LegalName = "Logi Corp", TradingName = "Logi", TaxNumber = "0102030405", IsActive = true };
            context.Companies.Add(company);
            await context.SaveChangesAsync();

            var b1 = new Branch { CompanyId = company.Id, Code = "SGN", Name = "Saigon", BranchType = "Hub", Phone = "028000", IsActive = true };
            var b2 = new Branch { CompanyId = company.Id, Code = "HAN", Name = "Hanoi", BranchType = "Hub", Phone = "024000", IsActive = true };
            context.Branches.AddRange(b1, b2);

            var country = new Country { Code = "VN", Alpha3Code = "VNM", Name = "Vietnam", IsActive = true };
            context.Countries.Add(country);

            var currency = new Currency { Code = "VND", Name = "Dong", Symbol = "₫", DecimalPlaces = 0, ExchangeRateToDefault = 1m, IsDefault = true, IsActive = true };
            context.Currencies.Add(currency);

            await context.SaveChangesAsync();

            var customer = new Customer
            {
                BranchId = b1.Id,
                CustomerCode = "CUST-NOTIF-1",
                CustomerType = "Corporate",
                Name = "Test Merchant",
                Email = "merchant@test.vn",
                PhoneNumber = "0901112223",
                IsActive = true
            };
            context.Customers.Add(customer);

            if (!await context.ReportDefinitions.AnyAsync())
            {
                context.ReportDefinitions.Add(new ReportDefinition
                {
                    Code = "RPT_DAILY_DISPATCH",
                    Name = "Daily Dispatch Summary",
                    Category = "Operations",
                    QueryDefinition = "SELECT * FROM shipping.Shipments",
                    IsActive = true
                });
            }

            await context.SaveChangesAsync();
        }

        var branches = await context.Branches.OrderBy(b => b.Id).ToListAsync();
        var cust = await context.Customers.FirstAsync();
        return (branches[0].Id, branches[1].Id, cust.Id);
    }

    private async Task<(long userId, string token)> GetUserAndTokenAsync(string username, string roleCode, long? restrictBranchId = null)
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
                user.UserRoles.Add(new UserRole { RoleId = role.Id, AssignedBy = "TestSetup", AssignedAt = DateTime.UtcNow });
            }

            if (restrictBranchId.HasValue)
            {
                user.BranchAccesses.Add(new UserBranchAccess { BranchId = restrictBranchId.Value, IsDefault = true, GrantedBy = "TestSetup", GrantedAt = DateTime.UtcNow });
            }
            else
            {
                foreach (var b in await context.Branches.ToListAsync())
                {
                    user.BranchAccesses.Add(new UserBranchAccess { BranchId = b.Id, IsDefault = b.Code == "SGN", GrantedBy = "TestSetup", GrantedAt = DateTime.UtcNow });
                }
            }

            context.UserAccounts.Add(user);
            await context.SaveChangesAsync();
        }

        var loginResp = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(username, password));
        loginResp.EnsureSuccessStatusCode();
        var auth = await loginResp.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        return (user.Id, auth!.Data!.AccessToken);
    }

    // ═══════════════════════════════════════════════════════════════
    //  1. Notification Lifecycle, Read State & Strict User Isolation
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Notifications_Create_Filter_ReadState_And_UserIsolation_Enforced()
    {
        var (_, adminToken) = await GetUserAndTokenAsync("notif_admin", AppRoles.SuperAdmin);
        var (userAId, userAToken) = await GetUserAndTokenAsync("notif_user_a", AppRoles.WarehouseStaff);
        var (userBId, userBToken) = await GetUserAndTokenAsync("notif_user_b", AppRoles.WarehouseStaff);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Create multiple notifications for User A and User B
        var createReq = new CreateNotificationRequest
        {
            RecipientUserIds = [userAId, userBId],
            Title = "Hub Sorting Alert",
            Content = "Inbound manifest #992 arrived.",
            Channel = "InApp",
            Priority = "High",
            NotificationType = "ParcelArrived"
        };

        var createRes = await _client.PostAsJsonAsync("/api/v1/notifications", createReq);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await createRes.Content.ReadFromJsonAsync<ApiResponse<List<NotificationDto>>>())!.Data!;
        created.Should().HaveCount(2);

        var userANotif = created.First(n => n.RecipientUserId == userAId);
        var userBNotif = created.First(n => n.RecipientUserId == userBId);

        // User A checks unread count & reads own notification
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userAToken);

        var unreadRes = await _client.GetFromJsonAsync<ApiResponse<UnreadCountDto>>("/api/v1/notifications/unread-count");
        unreadRes!.Data!.UnreadCount.Should().BeGreaterOrEqualTo(1);

        var listRes = await _client.GetFromJsonAsync<ApiResponse<PagedResponse<NotificationDto>>>(
            "/api/v1/notifications?isRead=false&notificationType=ParcelArrived");
        listRes!.Data!.Items.Should().Contain(n => n.Id == userANotif.Id);
        listRes.Data.Items.Should().NotContain(n => n.Id == userBNotif.Id);

        // User A CANNOT read or mark User B's notification -> 403 Forbidden
        var forbiddenGet = await _client.GetAsync($"/api/v1/notifications/{userBNotif.Id}");
        forbiddenGet.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var forbiddenPatch = await _client.PatchAsync($"/api/v1/notifications/{userBNotif.Id}/read", null);
        forbiddenPatch.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // User A marks own notification read
        var markRes = await _client.PatchAsync($"/api/v1/notifications/{userANotif.Id}/read", null);
        markRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var markedDto = (await markRes.Content.ReadFromJsonAsync<ApiResponse<NotificationDto>>())!.Data!;
        markedDto.IsRead.Should().BeTrue();
        markedDto.ReadAt.Should().NotBeNull();

        // Mark all read
        var markAllRes = await _client.PostAsync("/api/v1/notifications/mark-all-read", null);
        markAllRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var unreadAfter = await _client.GetFromJsonAsync<ApiResponse<UnreadCountDto>>("/api/v1/notifications/unread-count");
        unreadAfter!.Data!.UnreadCount.Should().Be(0);
    }

    // ═══════════════════════════════════════════════════════════════
    //  2. Template Management, Snapshot Preservation & Inactive Rejection
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task NotificationTemplates_Create_Preview_Snapshot_And_InactiveRejection_Succeeds()
    {
        var (userId, adminToken) = await GetUserAndTokenAsync("tpl_admin", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // 1. Create template
        var tplReq = new CreateNotificationTemplateRequest
        {
            Code = "ORDER_CONFIRMED_TPL",
            Name = "Order Confirmation Alert",
            Channel = "InApp",
            SubjectTemplate = "Order {{OrderNumber}} Confirmed",
            BodyTemplate = "Hello {{CustomerName}}, order {{OrderNumber}} is confirmed.",
            AllowedParameters = ["CustomerName", "OrderNumber"]
        };

        var tplRes = await _client.PostAsJsonAsync("/api/v1/notification-templates", tplReq);
        tplRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var template = (await tplRes.Content.ReadFromJsonAsync<ApiResponse<NotificationTemplateDto>>())!.Data!;

        // 2. Preview
        var previewRes = await _client.PostAsJsonAsync($"/api/v1/notification-templates/{template.Id}/preview",
            new PreviewTemplateRequest
            {
                Parameters = new Dictionary<string, string>
                {
                    ["CustomerName"] = "VinFast",
                    ["OrderNumber"] = "ORD-100"
                }
            });
        previewRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var preview = (await previewRes.Content.ReadFromJsonAsync<ApiResponse<RenderedTemplateDto>>())!.Data!;
        preview.Subject.Should().Be("Order ORD-100 Confirmed");

        // 3. Create notification using template (snapshot stored)
        var notifRes = await _client.PostAsJsonAsync("/api/v1/notifications", new CreateNotificationRequest
        {
            RecipientUserId = userId,
            TemplateCode = "ORDER_CONFIRMED_TPL",
            TemplateParameters = new Dictionary<string, string>
            {
                ["CustomerName"] = "VinFast",
                ["OrderNumber"] = "ORD-100"
            },
            Channel = "InApp"
        });
        notifRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdNotif = (await notifRes.Content.ReadFromJsonAsync<ApiResponse<List<NotificationDto>>>())!.Data!.First();

        // 4. Edit template and ensure historical notification content did NOT mutate
        await _client.PutAsJsonAsync($"/api/v1/notification-templates/{template.Id}", new UpdateNotificationTemplateRequest
        {
            Name = "Updated Name",
            Channel = "InApp",
            SubjectTemplate = "MUTATED {{OrderNumber}}",
            BodyTemplate = "MUTATED {{CustomerName}}",
            AllowedParameters = ["CustomerName", "OrderNumber"]
        });

        var oldNotif = await _client.GetFromJsonAsync<ApiResponse<NotificationDetailDto>>($"/api/v1/notifications/{createdNotif.Id}");
        oldNotif!.Data!.Title.Should().Be("Order ORD-100 Confirmed");

        // 5. Deactivate template -> creating notification fails with 409 Conflict
        await _client.PatchAsJsonAsync($"/api/v1/notification-templates/{template.Id}/status",
            new UpdateTemplateStatusRequest { IsActive = false });

        var inactiveRes = await _client.PostAsJsonAsync("/api/v1/notifications", new CreateNotificationRequest
        {
            RecipientUserId = userId,
            TemplateCode = "ORDER_CONFIRMED_TPL",
            TemplateParameters = new Dictionary<string, string>
            {
                ["CustomerName"] = "VinFast",
                ["OrderNumber"] = "ORD-101"
            },
            Channel = "InApp"
        });
        inactiveRes.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ═══════════════════════════════════════════════════════════════
    //  3. Transactional Outbox Integration + Idempotent Handler
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task OrderConfirm_PersistsOutboxAtomically_And_DispatchesIdempotently()
    {
        var (branch1Id, _, customerId) = await SeedBaseDataAsync();
        var (adminId, adminToken) = await GetUserAndTokenAsync("outbox_admin", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // 1. Create and Confirm Order via Sales API
        var createOrderRes = await _client.PostAsJsonAsync("/api/v1/orders", new CreateOrderRequest
        {
            CustomerId = customerId,
            BranchId = branch1Id,
            CodAmount = 250000m,
            Notes = "Outbox integration test order"
        });
        createOrderRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = (await createOrderRes.Content.ReadFromJsonAsync<ApiResponse<OrderDetailDto>>())!.Data!;

        var confirmRes = await _client.PostAsync($"/api/v1/orders/{order.Id}/confirm", null);
        confirmRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2. Verify OutboxMessage persisted atomically
        long outboxMsgId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var msg = await db.OutboxMessages.FirstOrDefaultAsync(m =>
                m.EventType == IntegrationEventTypes.OrderConfirmed && m.AggregateId == order.Id.ToString());
            msg.Should().NotBeNull();
            msg!.Status.Should().Be("Pending");
            outboxMsgId = msg.Id;
        }

        // 3. Failed business operation (confirming already confirmed order -> 409) must NOT emit extra OutboxMessage
        var secondConfirm = await _client.PostAsync($"/api/v1/orders/{order.Id}/confirm", null);
        secondConfirm.StatusCode.Should().Be(HttpStatusCode.Conflict);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var count = await db.OutboxMessages.CountAsync(m =>
                m.EventType == IntegrationEventTypes.OrderConfirmed && m.AggregateId == order.Id.ToString());
            count.Should().Be(1);
        }

        // 4. Process Outbox batch & verify notification created + handler idempotency on duplicate execution
        using (var scope = _factory.Services.CreateScope())
        {
            var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessorService>();
            var processed = await processor.ProcessBatchAsync("test-worker", 50);
            processed.Should().BeGreaterOrEqualTo(1);

            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var msg = await db.OutboxMessages.FindAsync(outboxMsgId);
            msg!.Status.Should().Be("Published");
            msg.ProcessedAt.Should().NotBeNull();

            var notifCount1 = await db.Notifications.CountAsync(n =>
                n.NotificationType == "OrderConfirmed" && n.ReferenceId == order.Id);
            notifCount1.Should().BeGreaterOrEqualTo(1);

            // Simulate duplicate outbox delivery of the EXACT same OutboxEventContext
            var handler = scope.ServiceProvider.GetServices<IOutboxEventHandler>()
                .First(h => h.EventType == IntegrationEventTypes.OrderConfirmed);

            await handler.HandleAsync(new OutboxEventContext(
                msg.Id, msg.EventType, msg.AggregateType, msg.AggregateId, msg.PayloadJson, msg.CorrelationId, msg.CreatedAt));

            var notifCount2 = await db.Notifications.CountAsync(n =>
                n.NotificationType == "OrderConfirmed" && n.ReferenceId == order.Id);
            notifCount2.Should().Be(notifCount1, "idempotent EventKey must prevent duplicate notifications");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  4. Background Jobs, Unknown Outbox Event & Authorization Tests
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task BackgroundJobs_And_ScheduledReports_Authorization_And_Lifecycle_Enforced()
    {
        var (b1, b2, _) = await SeedBaseDataAsync();
        var (_, staffToken) = await GetUserAndTokenAsync("staff_denied", AppRoles.WarehouseStaff, restrictBranchId: b1);
        var (_, b1MgrToken) = await GetUserAndTokenAsync("b1_mgr", AppRoles.BranchManager, restrictBranchId: b1);
        var (_, adminToken) = await GetUserAndTokenAsync("bg_admin", AppRoles.SuperAdmin);

        // 1. Unauthorized WarehouseStaff denied on background jobs & scheduled reports
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);
        (await _client.GetAsync("/api/v1/background-jobs")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await _client.GetAsync("/api/v1/background-jobs/health")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await _client.GetAsync("/api/v1/scheduled-reports")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 2. SuperAdmin checks health, retries failed job, cancels pending job, handles unknown outbox event
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.OutboxMessages.Add(new OutboxMessage
            {
                EventType = "unknown.legacy.event.v0",
                AggregateType = "Unknown",
                AggregateId = "999",
                PayloadJson = "{}",
                Status = "Pending"
            });
            db.BackgroundJobLogs.AddRange(
                new BackgroundJobLog { JobName = "TestJob", ExecutionId = Guid.NewGuid().ToString("N"), Status = "Failed", ExceptionDetails = "Timeout" },
                new BackgroundJobLog { JobName = "TestPendingJob", ExecutionId = Guid.NewGuid().ToString("N"), Status = "Pending" }
            );
            await db.SaveChangesAsync();

            var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessorService>();
            await processor.ProcessBatchAsync("test-worker");
            var unknownMsg = await db.OutboxMessages.FirstAsync(m => m.EventType == "unknown.legacy.event.v0");
            unknownMsg.Status.Should().Be("Published");
            unknownMsg.ErrorMessage.Should().Contain("No handler registered");
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var healthRes = await _client.GetFromJsonAsync<ApiResponse<BackgroundProcessingHealthDto>>("/api/v1/background-jobs/health");
        healthRes!.Data.Should().NotBeNull();

        var failedList = await _client.GetFromJsonAsync<ApiResponse<PagedResponse<BackgroundJobLogDto>>>("/api/v1/background-jobs/failed");
        var failedJob = failedList!.Data!.Items.First();

        var retryRes = await _client.PostAsync($"/api/v1/background-jobs/{failedJob.Id}/retry", null);
        retryRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var retried = (await retryRes.Content.ReadFromJsonAsync<ApiResponse<BackgroundJobLogDto>>())!.Data!;
        retried.Status.Should().Be("Pending");

        // Cancel pending job
        long pendingId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            pendingId = (await db.BackgroundJobLogs.FirstAsync(j => j.JobName == "TestPendingJob")).Id;
        }
        var cancelRes = await _client.PostAsync($"/api/v1/background-jobs/{pendingId}/cancel?reason=Obsolete", null);
        cancelRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Scheduled Reports: Branch 1 Manager can create report for Branch 1, denied on Branch 2
        long defId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            defId = (await db.ReportDefinitions.FirstAsync()).Id;
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", b1MgrToken);
        var createB2Rpt = await _client.PostAsJsonAsync("/api/v1/scheduled-reports", new CreateScheduledReportRequest
        {
            ReportDefinitionId = defId,
            BranchId = b2,
            ScheduleCron = "0 6 * * *",
            OutputFormat = "Pdf",
            RecipientEmails = "mgr@b2.test"
        });
        createB2Rpt.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var createB1Rpt = await _client.PostAsJsonAsync("/api/v1/scheduled-reports", new CreateScheduledReportRequest
        {
            ReportDefinitionId = defId,
            BranchId = b1,
            ScheduleCron = "0 6 * * *",
            TimeZoneId = "SE Asia Standard Time",
            OutputFormat = "Pdf",
            RecipientEmails = "ops@b1.test"
        });
        createB1Rpt.StatusCode.Should().Be(HttpStatusCode.Created);
        var rpt = (await createB1Rpt.Content.ReadFromJsonAsync<ApiResponse<ScheduledReportDto>>())!.Data!;

        // Admin triggers manual run -> explicitly logs Warning & IsReportGenerationSupported = false (never fake PDF!)
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var runRes = await _client.PostAsync($"/api/v1/scheduled-reports/{rpt.Id}/run", null);
        runRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var runData = (await runRes.Content.ReadFromJsonAsync<ApiResponse<ScheduledReportRunResultDto>>())!.Data!;
        runData.IsReportGenerationSupported.Should().BeFalse();
        runData.Status.Should().Be("Warning");

        var historyRes = await _client.GetFromJsonAsync<ApiResponse<PagedResponse<BackgroundJobLogDto>>>($"/api/v1/scheduled-reports/{rpt.Id}/history");
        historyRes!.Data!.Items.Should().HaveCount(1);
    }

    // ═══════════════════════════════════════════════════════════════
    //  5. Swagger / OpenAPI & DI Scope Verification
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Swagger_And_DI_Verification_IncludesAllNotificationAndBackgroundEndpoints()
    {
        var swaggerRes = await _client.GetAsync("/swagger/v1/swagger.json");
        var json = await swaggerRes.Content.ReadAsStringAsync();
        swaggerRes.StatusCode.Should().Be(HttpStatusCode.OK, because: json);

        json.Should().Contain("/api/v1/notifications");
        json.Should().Contain("/api/v1/notification-templates");
        json.Should().Contain("/api/v1/background-jobs");
        json.Should().Contain("/api/v1/scheduled-reports");

        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        sp.GetRequiredService<INotificationService>().Should().NotBeNull();
        sp.GetRequiredService<INotificationTemplateService>().Should().NotBeNull();
        sp.GetRequiredService<INotificationTemplateRenderer>().Should().NotBeNull();
        sp.GetRequiredService<INotificationDeliveryService>().Should().NotBeNull();
        sp.GetRequiredService<IEmailProvider>().Should().NotBeNull();
        sp.GetRequiredService<IOutboxService>().Should().NotBeNull();
        sp.GetRequiredService<IOutboxProcessorService>().Should().NotBeNull();
        sp.GetRequiredService<IBackgroundJobService>().Should().NotBeNull();
        sp.GetRequiredService<IScheduledReportService>().Should().NotBeNull();
        sp.GetRequiredService<IScheduledMaintenanceService>().Should().NotBeNull();
        sp.GetRequiredService<IReportExecutionEngine>().Should().NotBeNull();
    }
}
