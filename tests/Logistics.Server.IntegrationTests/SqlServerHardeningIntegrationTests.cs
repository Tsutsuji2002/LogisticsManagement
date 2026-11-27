using System.Collections.Concurrent;
using FluentAssertions;
using Logistics.Server.Application.Common.Events;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Domain.Entities.Configuration;
using Logistics.Server.Domain.Entities.Integration;
using Logistics.Server.Infrastructure.Persistence;
using Logistics.Server.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Logistics.Server.IntegrationTests;

/// <summary>
/// Real SQL Server LocalDB hardening tests for distributed/concurrent locking and outbox fault isolation.
/// </summary>
public sealed class SqlServerHardeningIntegrationTests : IAsyncLifetime
{
    private readonly string _dbName = "LogisticsHardeningSqlTest_" + Guid.NewGuid().ToString("N")[..10];
    private string ConnectionString =>
        $"Server=(localdb)\\mssqllocaldb;Database={_dbName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    private DbContextOptions<ApplicationDbContext> _dbOptions = null!;

    public async Task InitializeAsync()
    {
        _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        await using var db = new ApplicationDbContext(_dbOptions);
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        try
        {
            await using var db = new ApplicationDbContext(_dbOptions);
            await db.Database.EnsureDeletedAsync();
        }
        catch
        {
            // Best-effort cleanup of disposable test DB
        }
    }

    [Fact]
    public async Task NumberSequenceService_SqlServerConcurrentCalls_GenerateUniqueSequentialNumbers()
    {
        await using (var seedDb = new ApplicationDbContext(_dbOptions))
        {
            seedDb.NumberSequences.Add(new NumberSequence
            {
                Scope = "WAYBILL",
                Prefix = "WB",
                NextNumber = 1,
                Increment = 1,
                NumberLength = 6,
                CurrentFormat = "{PREFIX}-{YYYYMM}-{NUMBER}",
                ResetFrequency = "Monthly",
                LastResetDate = DateOnly.FromDateTime(DateTime.UtcNow)
            });
            await seedDb.SaveChangesAsync();
        }

        const int workers = 12;
        var generated = new ConcurrentBag<string>();

        var tasks = Enumerable.Range(0, workers).Select(async _ =>
        {
            await using var db = new ApplicationDbContext(_dbOptions);
            var svc = new NumberSequenceService(db);
            var code = await svc.GetNextNumberAsync("WAYBILL");
            generated.Add(code);
        });

        await Task.WhenAll(tasks);

        generated.Should().HaveCount(workers);
        generated.Should().OnlyHaveUniqueItems("SQL Server UPDLOCK/ROWLOCK row locking must prevent duplicate sequence issuance");

        await using var verifyDb = new ApplicationDbContext(_dbOptions);
        var seq = await verifyDb.NumberSequences.FirstAsync(s => s.Scope == "WAYBILL");
        seq.NextNumber.Should().Be(workers + 1);
    }

    [Fact]
    public async Task OutboxProcessorService_FailingHandlerWithDirtyTracker_IsolatesFailureAndCompletesBatch()
    {
        await using (var seedDb = new ApplicationDbContext(_dbOptions))
        {
            seedDb.OutboxMessages.AddRange(
                new OutboxMessage
                {
                    EventType = IntegrationEventTypes.DeliveryFailed,
                    AggregateType = "Shipment",
                    AggregateId = "DIRTY-FAIL-1",
                    PayloadJson = "{}",
                    Status = "Pending",
                    RetryCount = 0
                },
                new OutboxMessage
                {
                    EventType = IntegrationEventTypes.OrderConfirmed,
                    AggregateType = "Order",
                    AggregateId = "CLEAN-OK-2",
                    PayloadJson = "{}",
                    Status = "Pending",
                    RetryCount = 0
                });
            await seedDb.SaveChangesAsync();
        }

        await using var workerDb = new ApplicationDbContext(_dbOptions);
        var failingDirtyHandler = new DirtyStagedFailureHandler(workerDb);
        var successHandler = new SuccessCountingHandler();

        var processor = new OutboxProcessorService(
            workerDb,
            [failingDirtyHandler, successHandler],
            new TestAuditService(),
            new TestCurrentUserService(),
            NullLogger<OutboxProcessorService>.Instance);

        var processed = await processor.ProcessBatchAsync("hardening-worker", batchSize: 10);
        processed.Should().Be(1, "clean message should succeed even after prior handler staged an invalid entity and threw");
        successHandler.HandledAggregateIds.Should().ContainSingle(id => id == "CLEAN-OK-2");

        await using var checkDb = new ApplicationDbContext(_dbOptions);
        var failedMsg = await checkDb.OutboxMessages.FirstAsync(m => m.AggregateId == "DIRTY-FAIL-1");
        failedMsg.Status.Should().Be("Pending");
        failedMsg.RetryCount.Should().Be(1);
        failedMsg.NextRetryAt.Should().NotBeNull();
        failedMsg.LockedBy.Should().BeNull();
        failedMsg.ErrorMessage.Should().Contain("Simulated dirty-tracker failure");

        var okMsg = await checkDb.OutboxMessages.FirstAsync(m => m.AggregateId == "CLEAN-OK-2");
        okMsg.Status.Should().Be("Published");
        okMsg.ProcessedAt.Should().NotBeNull();
    }

    private sealed class DirtyStagedFailureHandler : IOutboxEventHandler
    {
        private readonly ApplicationDbContext _db;

        public DirtyStagedFailureHandler(ApplicationDbContext db) => _db = db;

        public string EventType => IntegrationEventTypes.DeliveryFailed;

        public Task HandleAsync(OutboxEventContext context, CancellationToken cancellationToken = default)
        {
            // Stage an invalid FK notification onto the shared tracked DbContext before failing
            _db.Notifications.Add(new Logistics.Server.Domain.Entities.Notification.Notification
            {
                RecipientUserId = 999999999L,
                Title = "Broken Notification",
                Content = "Should be detached on failure",
                Channel = "InApp",
                Status = "Pending"
            });
            throw new InvalidOperationException("Simulated dirty-tracker failure");
        }
    }

    private sealed class SuccessCountingHandler : IOutboxEventHandler
    {
        public string EventType => IntegrationEventTypes.OrderConfirmed;
        public ConcurrentBag<string> HandledAggregateIds { get; } = [];

        public Task HandleAsync(OutboxEventContext context, CancellationToken cancellationToken = default)
        {
            HandledAggregateIds.Add(context.AggregateId);
            return Task.CompletedTask;
        }
    }

    private sealed class TestAuditService : IAuditService
    {
        public Task RecordAsync(
            string action,
            string schemaName,
            string tableName,
            string primaryKey,
            object? oldValues,
            object? newValues,
            string? notes = null,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public long? UserId => null;
        public string? Username => "SqlHardeningWorker";
        public string? Email => null;
        public long? EmployeeId => null;
        public long? DefaultBranchId => null;
        public IReadOnlyList<long> AccessibleBranchIds => [];
        public IReadOnlyList<string> Roles => [];
        public bool IsAuthenticated => false;
        public bool IsInRole(string role) => false;
        public bool HasBranchAccess(long branchId) => true;
    }
}
