using System.Collections.Concurrent;
using FluentAssertions;
using Logistics.Server.Application.Common.Events;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Modules.BackgroundProcessing.DTOs;
using Logistics.Server.Application.Modules.BackgroundProcessing.Services;
using Logistics.Server.Domain.Entities.Integration;
using Logistics.Server.Infrastructure.Persistence;
using Logistics.Server.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Logistics.Server.IntegrationTests;

/// <summary>
/// Relational SQL Server LocalDB integration tests validating migrations,
/// atomic transaction rollbacks, SQL Server UPDLOCK/READPAST multi-worker claiming,
/// lease recovery, and concurrent background job processing.
/// </summary>
public class OutboxSqlServerConcurrencyTests : IAsyncLifetime
{
    private readonly string _connectionString;
    private readonly DbContextOptions<ApplicationDbContext> _dbOptions;

    public OutboxSqlServerConcurrencyTests()
    {
        var dbName = $"LogisticsOutboxSqlTest_{Guid.NewGuid():N}";
        _connectionString = $@"Server=(localdb)\mssqllocaldb;Database={dbName};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";
        _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
    }

    public async Task InitializeAsync()
    {
        await using var db = new ApplicationDbContext(_dbOptions);
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await using var db = new ApplicationDbContext(_dbOptions);
        await db.Database.EnsureDeletedAsync();
    }

    private sealed class StubAuditService : IAuditService
    {
        public Task RecordAsync(string action, string schemaName, string tableName, string primaryKey,
            object? oldValues, object? newValues, string? notes = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class StubCurrentUserService : ICurrentUserService
    {
        public long? UserId => null;
        public string? Username => "SqlTestWorker";
        public string? Email => null;
        public long? EmployeeId => null;
        public long? DefaultBranchId => null;
        public IReadOnlyList<long> AccessibleBranchIds => [];
        public IReadOnlyList<string> Roles => [];
        public bool IsAuthenticated => false;
        public bool IsInRole(string role) => false;
        public bool HasBranchAccess(long branchId) => true;
    }

    private sealed class RecordingOutboxHandler : IOutboxEventHandler
    {
        public string EventType => IntegrationEventTypes.OrderConfirmed;
        public ConcurrentBag<(string WorkerId, long MessageId)> Invocations { get; } = [];
        public Func<OutboxEventContext, Task>? CustomBehavior { get; init; }

        public async Task HandleAsync(OutboxEventContext context, CancellationToken cancellationToken = default)
        {
            if (CustomBehavior != null)
                await CustomBehavior(context);

            Invocations.Add(("handled", context.MessageId));
        }
    }

    private sealed class FailingOutboxHandler : IOutboxEventHandler
    {
        public string EventType => IntegrationEventTypes.DeliveryFailed;
        public Task HandleAsync(OutboxEventContext context, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Simulated downstream failure.");
    }

    [Fact]
    public async Task ConcurrentWorkers_SqlServerRowLocking_ClaimEachOutboxMessageExactlyOnce()
    {
        const int totalMessages = 24;

        await using (var seedDb = new ApplicationDbContext(_dbOptions))
        {
            for (int i = 1; i <= totalMessages; i++)
            {
                seedDb.OutboxMessages.Add(new OutboxMessage
                {
                    EventType = IntegrationEventTypes.OrderConfirmed,
                    AggregateType = "Order",
                    AggregateId = i.ToString(),
                    PayloadJson = $"{{\"orderId\":{i}}}",
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow.AddSeconds(-i)
                });
            }
            await seedDb.SaveChangesAsync();
        }

        var handler = new RecordingOutboxHandler
        {
            CustomBehavior = async _ => await Task.Delay(25)
        };

        var audit = new StubAuditService();
        var currentUser = new StubCurrentUserService();

        async Task<int> RunWorkerAsync(string workerId)
        {
            await using var workerDb = new ApplicationDbContext(_dbOptions);
            var svc = new OutboxProcessorService(
                workerDb, [handler], audit, currentUser, NullLogger<OutboxProcessorService>.Instance);

            int total = 0;
            while (true)
            {
                var processed = await svc.ProcessBatchAsync(workerId, batchSize: 4);
                if (processed == 0) break;
                total += processed;
            }
            return total;
        }

        var counts = await Task.WhenAll(
            RunWorkerAsync("worker-A"),
            RunWorkerAsync("worker-B"),
            RunWorkerAsync("worker-C"));

        counts.Sum().Should().Be(totalMessages);
        handler.Invocations.Select(x => x.MessageId).Should().OnlyHaveUniqueItems();
        handler.Invocations.Should().HaveCount(totalMessages);

        await using var verifyDb = new ApplicationDbContext(_dbOptions);
        var published = await verifyDb.OutboxMessages.CountAsync(m => m.Status == "Published");
        published.Should().Be(totalMessages);
    }

    [Fact]
    public async Task FailedDatabaseTransaction_DoesNotLeaveOrphanedOutboxMessage()
    {
        await using var db = new ApplicationDbContext(_dbOptions);
        await using var tx = await db.Database.BeginTransactionAsync();

        var outbox = new OutboxService(db, new Microsoft.AspNetCore.Http.HttpContextAccessor());
        outbox.Enqueue(new OrderConfirmedIntegrationEvent(999, "ORD-ROLLBACK", 1, 1, 50000m), "Order", "999");
        await db.SaveChangesAsync();

        await tx.RollbackAsync();

        await using var checkDb = new ApplicationDbContext(_dbOptions);
        var exists = await checkDb.OutboxMessages.AnyAsync(m => m.AggregateId == "999");
        exists.Should().BeFalse("rolled-back transactions must roll back staged OutboxMessages atomically");
    }

    [Fact]
    public async Task StaleInProgressOutboxMessage_And_DeadLetterRetries_WorkOnSqlServer()
    {
        await using (var seedDb = new ApplicationDbContext(_dbOptions))
        {
            // 1. Stale message left InProgress by crashed worker
            seedDb.OutboxMessages.Add(new OutboxMessage
            {
                EventType = IntegrationEventTypes.OrderConfirmed,
                AggregateType = "Order",
                AggregateId = "STALE-1",
                PayloadJson = "{}",
                Status = "InProgress",
                LockedAt = DateTime.UtcNow.AddMinutes(-15),
                LockedBy = "crashed-worker"
            });

            // 2. Failing message at RetryCount = 4 (1 away from terminal Failed)
            seedDb.OutboxMessages.Add(new OutboxMessage
            {
                EventType = IntegrationEventTypes.DeliveryFailed,
                AggregateType = "Shipment",
                AggregateId = "FAIL-1",
                PayloadJson = "{}",
                Status = "Pending",
                RetryCount = OutboxProcessorService.MaxRetryCount - 1
            });

            await seedDb.SaveChangesAsync();
        }

        await using var db = new ApplicationDbContext(_dbOptions);
        var audit = new StubAuditService();
        var currentUser = new StubCurrentUserService();
        var handler1 = new RecordingOutboxHandler();
        var handler2 = new FailingOutboxHandler();

        var processor = new OutboxProcessorService(
            db, [handler1, handler2], audit, currentUser, NullLogger<OutboxProcessorService>.Instance);

        // Recover stale lease
        var recovered = await processor.RecoverStaleInProgressAsync(TimeSpan.FromMinutes(5));
        recovered.Should().Be(1);

        // Process both
        await processor.ProcessBatchAsync("recovery-worker", 10);

        await using var verifyDb = new ApplicationDbContext(_dbOptions);
        var staleMsg = await verifyDb.OutboxMessages.FirstAsync(m => m.AggregateId == "STALE-1");
        staleMsg.Status.Should().Be("Published");
        staleMsg.LockedBy.Should().BeNull();

        var failMsg = await verifyDb.OutboxMessages.FirstAsync(m => m.AggregateId == "FAIL-1");
        failMsg.Status.Should().Be("Failed");
        failMsg.RetryCount.Should().Be(OutboxProcessorService.MaxRetryCount);
        failMsg.ErrorMessage.Should().Contain("Simulated downstream failure");
    }

    private sealed class SampleJobHandler : IBackgroundJobHandler
    {
        public string JobName => "ConcurrentSqlJob";
        public ConcurrentBag<string?> ExecutedRefs { get; } = [];

        public async Task<BackgroundJobExecutionResult> ExecuteAsync(string? jobReference, string? payloadJson, CancellationToken cancellationToken = default)
        {
            await Task.Delay(20, cancellationToken);
            ExecutedRefs.Add(jobReference);
            return new BackgroundJobExecutionResult { Succeeded = true, RecordsProcessed = 1, RecordsSucceeded = 1 };
        }
    }

    [Fact]
    public async Task BackgroundJobs_SqlServerConcurrentClaiming_And_StaleRecovery_Work()
    {
        await using (var seedDb = new ApplicationDbContext(_dbOptions))
        {
            for (int i = 1; i <= 10; i++)
            {
                seedDb.BackgroundJobLogs.Add(new BackgroundJobLog
                {
                    JobName = "ConcurrentSqlJob",
                    ExecutionId = Guid.NewGuid().ToString("N"),
                    JobReference = $"REF-{i}",
                    TriggerType = "Event",
                    Status = "Pending",
                    AttemptCount = 0,
                    MaxAttempts = 3,
                    NextRetryAt = DateTime.UtcNow.AddSeconds(-5)
                });
            }

            seedDb.BackgroundJobLogs.Add(new BackgroundJobLog
            {
                JobName = "ConcurrentSqlJob",
                ExecutionId = Guid.NewGuid().ToString("N"),
                JobReference = "STALE-JOB",
                TriggerType = "Event",
                Status = "Running",
                AttemptCount = 1,
                MaxAttempts = 3,
                LockedAt = DateTime.UtcNow.AddMinutes(-20),
                LockedBy = "dead-worker"
            });

            await seedDb.SaveChangesAsync();
        }

        var jobHandler = new SampleJobHandler();
        var audit = new StubAuditService();
        var currentUser = new StubCurrentUserService();

        await using (var recDb = new ApplicationDbContext(_dbOptions))
        {
            var recSvc = new BackgroundJobService(recDb, [jobHandler], audit, currentUser, NullLogger<BackgroundJobService>.Instance);
            var recovered = await recSvc.RecoverStaleJobsAsync(TimeSpan.FromMinutes(10));
            recovered.Should().Be(1);
        }

        async Task RunJobWorker(string workerId)
        {
            await using var db = new ApplicationDbContext(_dbOptions);
            var svc = new BackgroundJobService(db, [jobHandler], audit, currentUser, NullLogger<BackgroundJobService>.Instance);
            while (await svc.ProcessPendingJobsAsync(workerId, 3) > 0) { }
        }

        await Task.WhenAll(RunJobWorker("jw-1"), RunJobWorker("jw-2"));

        jobHandler.ExecutedRefs.Should().HaveCount(11);
        jobHandler.ExecutedRefs.Should().OnlyHaveUniqueItems();

        await using var verifyDb = new ApplicationDbContext(_dbOptions);
        var succeeded = await verifyDb.BackgroundJobLogs.CountAsync(j => j.Status == "Success");
        succeeded.Should().Be(11);
    }
}
