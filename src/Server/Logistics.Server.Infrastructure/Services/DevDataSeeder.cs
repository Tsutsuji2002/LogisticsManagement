using System.Data.Common;
using System.Diagnostics;
using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Domain.Entities.Carrier;
using Logistics.Server.Domain.Entities.Catalog;
using Logistics.Server.Domain.Entities.Configuration;
using Logistics.Server.Domain.Entities.Crm;
using Logistics.Server.Domain.Entities.Finance;
using Logistics.Server.Domain.Entities.Identity;
using Logistics.Server.Domain.Entities.Notification;
using Logistics.Server.Domain.Entities.Organization;
using Logistics.Server.Domain.Entities.Warehouse;
using Logistics.Server.Infrastructure.Persistence;
using Logistics.Shared.Contracts.DevData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Logistics.Server.Infrastructure.Services;

/// <summary>
/// Development-only idempotent data seeder &amp; management service.
/// Supports FullSeed, Incremental, ResetAndReseed, and ModuleSpecific modes
/// across Standard and Large scales, plus automated data integrity verification.
/// </summary>
public partial class DevDataSeeder : IDevTestDataService
{
    private readonly ApplicationDbContext _db;
    private readonly IPasswordHasherService _hasher;
    private readonly IConfiguration _config;
    private readonly ILogger<DevDataSeeder> _logger;
    private readonly IHostEnvironment? _hostEnv;
    private readonly ICacheService? _cache;

    private Random _rng = new(42);

    public DevDataSeeder(
        ApplicationDbContext db,
        IPasswordHasherService hasher,
        IConfiguration config,
        ILogger<DevDataSeeder> logger,
        IHostEnvironment? hostEnv = null,
        ICacheService? cache = null)
    {
        _db = db;
        _hasher = hasher;
        _config = config;
        _logger = logger;
        _hostEnv = hostEnv;
        _cache = cache;
    }

    /// <summary>
    /// Backward-compatible startup entry point called by Program.cs in Development.
    /// </summary>
    public async Task SeedAsync(CancellationToken ct = default)
    {
        EnsureDevSafety(isApiCall: false, isDestructive: false, confirmReset: false);
        if (_db.Database.IsRelational())
            _db.Database.SetCommandTimeout(180);
        await RunFullSeedInternalAsync(DevSeedScale.Standard, randomSeed: 42, ct);
        _logger.LogInformation("DevDataSeeder: startup seeding completed successfully.");
    }

    public async Task<DevTestDataStatusDto> GetStatusAsync(CancellationToken ct = default)
    {
        EnsureDevSafety(isApiCall: true, isDestructive: false, confirmReset: false);

        var (server, database) = ParseConnectionSummary();
        var modules = await BuildCoverageListAsync(ct);

        var settings = await _db.SystemSettings
            .AsNoTracking()
            .Where(s => s.Category == "DevSeed")
            .ToDictionaryAsync(s => s.Key, s => s.Value, ct);

        DateTime? lastRun = null;
        if (settings.TryGetValue("DevSeed.LastRunAtUtc", out var dtRaw) && DateTime.TryParse(dtRaw, out var parsedDt))
            lastRun = DateTime.SpecifyKind(parsedDt, DateTimeKind.Utc);

        return new DevTestDataStatusDto
        {
            IsDevelopmentEnvironment = _hostEnv?.IsDevelopment() ?? true,
            IsDevSeedEnabled = _config.GetValue<bool>("DevSeed:Enabled"),
            EnvironmentName = _hostEnv?.EnvironmentName ?? "Development",
            DatabaseProvider = _db.Database.ProviderName ?? "Unknown",
            DatabaseServer = server,
            DatabaseName = database,
            LastSeededAtUtc = lastRun,
            LastSeedMode = settings.GetValueOrDefault("DevSeed.LastMode"),
            LastSeedScale = settings.GetValueOrDefault("DevSeed.LastScale"),
            LastSeedSummary = settings.GetValueOrDefault("DevSeed.LastSummary"),
            TotalRecordsAcrossModules = modules.Sum(m => m.CurrentCount),
            Modules = modules
        };
    }

    public async Task<ExecuteDevSeedResultDto> ExecuteAsync(ExecuteDevSeedRequest request, CancellationToken ct = default)
    {
        var isDestructive = request.Mode == DevSeedMode.ResetAndReseed;
        EnsureDevSafety(isApiCall: true, isDestructive: isDestructive, confirmReset: request.ConfirmReset);

        _rng = new Random(request.RandomSeed);
        var sw = Stopwatch.StartNew();
        if (_db.Database.IsRelational())
            _db.Database.SetCommandTimeout(180);

        var beforeCounts = await CaptureKeyCountsAsync(ct);

        _logger.LogInformation(
            "DevTestData execution starting: Mode={Mode}, Scale={Scale}, RandomSeed={Seed}, Modules={Modules}",
            request.Mode, request.Scale, request.RandomSeed,
            request.Modules == null ? "ALL" : string.Join(",", request.Modules));

        if (isDestructive)
        {
            if (_db.Database.IsRelational())
            {
                await using var tx = await _db.Database.BeginTransactionAsync(ct);
                try
                {
                    await ResetTestDataAsync(ct);
                    await RunFullSeedInternalAsync(request.Scale, request.RandomSeed, ct);
                    await tx.CommitAsync(ct);
                }
                catch (Exception ex)
                {
                    try { await tx.RollbackAsync(CancellationToken.None); } catch { /* SQL Server may have already aborted tx */ }
                    _logger.LogError(ex, "DevDataSeeder ResetAndReseed failed and rolled back cleanly.");
                    throw;
                }
            }
            else
            {
                await ResetTestDataAsync(ct);
                await RunFullSeedInternalAsync(request.Scale, request.RandomSeed, ct);
            }
        }
        else if (request.Mode == DevSeedMode.Incremental)
        {
            await RunIncrementalSeedAsync(request.Scale, ct);
        }
        else if (request.Mode == DevSeedMode.ModuleSpecific)
        {
            await RunModuleSpecificSeedAsync(request.Scale, request.Modules ?? [], ct);
        }
        else
        {
            await RunFullSeedInternalAsync(request.Scale, request.RandomSeed, ct);
        }

        sw.Stop();
        var afterCounts = await CaptureKeyCountsAsync(ct);
        var deltas = new Dictionary<string, int>();
        foreach (var kvp in afterCounts)
        {
            var prev = isDestructive ? 0 : beforeCounts.GetValueOrDefault(kvp.Key, 0);
            deltas[kvp.Key] = Math.Max(0, kvp.Value - prev);
        }

        var totalAdded = deltas.Values.Sum();
        var summary = $"{request.Mode} ({request.Scale}) hoàn tất trong {sw.ElapsedMilliseconds} ms. Đã thêm {totalAdded:N0} bản ghi mới.";

        await SaveLastSeedMetadataAsync(request.Mode.ToString(), request.Scale.ToString(), summary, ct);
        await InvalidateDashboardCachesAsync(ct);

        var coverage = await BuildCoverageListAsync(ct);
        var integrity = await VerifyIntegrityAsync(ct);

        _logger.LogInformation("DevTestData execution completed: {Summary}", summary);

        return new ExecuteDevSeedResultDto
        {
            Succeeded = true,
            Mode = request.Mode,
            Scale = request.Scale,
            RandomSeed = request.RandomSeed,
            DurationMs = sw.ElapsedMilliseconds,
            ExecutedAtUtc = DateTime.UtcNow,
            Summary = summary,
            RecordsAddedByEntity = deltas,
            UpdatedCoverage = coverage,
            IntegrityReport = integrity
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Safety Guard
    // ─────────────────────────────────────────────────────────────────────────
    private void EnsureDevSafety(bool isApiCall, bool isDestructive, bool confirmReset)
    {
        if (_hostEnv != null && !_hostEnv.IsDevelopment())
        {
            throw new ForbiddenAccessException(
                $"Dev Test Data operations are forbidden in environment '{_hostEnv.EnvironmentName}'. Only Development is allowed.");
        }

        if (isApiCall && !_config.GetValue<bool>("DevSeed:Enabled"))
        {
            throw new ForbiddenAccessException(
                "Dev Test Data API is disabled (DevSeed:Enabled is false).");
        }

        var connStr = _db.Database.IsRelational() ? _db.Database.GetConnectionString() ?? string.Empty : string.Empty;
        if (!string.IsNullOrWhiteSpace(connStr))
        {
            var (_, dbName) = ParseConnectionSummary();
            if (dbName.Contains("prod", StringComparison.OrdinalIgnoreCase))
            {
                throw new ForbiddenAccessException(
                    $"Refusing to execute Dev Test Data against potential production database '{dbName}'.");
            }
        }

        if (isDestructive && !confirmReset)
        {
            throw new ValidationException(
                "ConfirmReset",
                "ResetAndReseed requires explicit confirmation (ConfirmReset = true).");
        }
    }

    private (string Server, string Database) ParseConnectionSummary()
    {
        if (!_db.Database.IsRelational())
            return ("InMemory", "TestDb");

        var raw = _db.Database.GetConnectionString();
        if (string.IsNullOrWhiteSpace(raw))
            return ("Unknown", "Unknown");

        try
        {
            var builder = new DbConnectionStringBuilder { ConnectionString = raw };
            var server = builder.ContainsKey("Server") ? builder["Server"]?.ToString()
                : builder.ContainsKey("Data Source") ? builder["Data Source"]?.ToString() : "Unknown";
            var db = builder.ContainsKey("Database") ? builder["Database"]?.ToString()
                : builder.ContainsKey("Initial Catalog") ? builder["Initial Catalog"]?.ToString() : "Unknown";
            return (server ?? "Unknown", db ?? "Unknown");
        }
        catch
        {
            return ("Configured", "Unknown");
        }
    }

    private async Task RunFullSeedInternalAsync(DevSeedScale scale, int randomSeed, CancellationToken ct)
    {
        _rng = new Random(randomSeed);

        await SeedGeographyAsync(ct);
        await SeedCurrenciesAsync(ct);
        await SeedUnitsOfMeasureAsync(ct);
        await SeedSystemSettingsAsync(ct);
        await SeedNumberSequencesAsync(ct);
        await SeedCompanyBranchesAndWarehousesAsync(ct);
        await SeedWarehouseZonesAndLocationsAsync(ct);
        await SeedDepartmentsAsync(ct);
        await SeedEmployeesAndUsersAsync(ct);
        await EnsureBootstrapAdminBranchAccessAsync(ct);
        await SeedCustomersAsync(scale, ct);
        await SeedProductCatalogAsync(ct);
        await SeedCarriersAsync(ct);
        await SeedShippingRateCardAsync(ct);
        await SeedNotificationTemplatesAndReportsAsync(ct);

        await SeedOrdersAndShipmentsAsync(scale, isIncremental: false, ct);
        await SeedPickupRequestsAsync(scale, isIncremental: false, ct);
        await SeedWarehouseOperationsAsync(scale, isIncremental: false, ct);
        await SeedInventoryDataAsync(scale, isIncremental: false, ct);
        await SeedFinanceDataAsync(isIncremental: false, ct);
        await SeedCodDataAsync(isIncremental: false, ct);
        await SeedNotificationsJobsAndAuditAsync(scale, isIncremental: false, ct);
        await UpdateNumberSequencesToMaxAsync(ct);
    }

    private async Task RunIncrementalSeedAsync(DevSeedScale scale, CancellationToken ct)
    {
        // Ensure reference data exists first
        await SeedGeographyAsync(ct);
        await SeedCurrenciesAsync(ct);
        await SeedUnitsOfMeasureAsync(ct);
        await SeedSystemSettingsAsync(ct);
        await SeedNumberSequencesAsync(ct);
        await SeedCompanyBranchesAndWarehousesAsync(ct);
        await SeedWarehouseZonesAndLocationsAsync(ct);
        await SeedDepartmentsAsync(ct);
        await SeedEmployeesAndUsersAsync(ct);
        await EnsureBootstrapAdminBranchAccessAsync(ct);
        await SeedCustomersAsync(scale, ct);
        await SeedProductCatalogAsync(ct);
        await SeedCarriersAsync(ct);
        await SeedShippingRateCardAsync(ct);
        await SeedNotificationTemplatesAndReportsAsync(ct);

        // Append a fresh batch of operational records
        await SeedOrdersAndShipmentsAsync(scale, isIncremental: true, ct);
        await SeedPickupRequestsAsync(scale, isIncremental: true, ct);
        await SeedWarehouseOperationsAsync(scale, isIncremental: true, ct);
        await SeedInventoryDataAsync(scale, isIncremental: true, ct);
        await SeedFinanceDataAsync(isIncremental: true, ct);
        await SeedCodDataAsync(isIncremental: true, ct);
        await SeedNotificationsJobsAndAuditAsync(scale, isIncremental: true, ct);
        await UpdateNumberSequencesToMaxAsync(ct);
    }

    private async Task RunModuleSpecificSeedAsync(DevSeedScale scale, IReadOnlyList<string> modules, CancellationToken ct)
    {
        var set = new HashSet<string>(modules, StringComparer.OrdinalIgnoreCase);

        // Always guarantee foundational reference & org data
        await SeedGeographyAsync(ct);
        await SeedCurrenciesAsync(ct);
        await SeedUnitsOfMeasureAsync(ct);
        await SeedSystemSettingsAsync(ct);
        await SeedNumberSequencesAsync(ct);
        await SeedCompanyBranchesAndWarehousesAsync(ct);
        await SeedWarehouseZonesAndLocationsAsync(ct);
        await SeedDepartmentsAsync(ct);
        await SeedEmployeesAndUsersAsync(ct);
        await EnsureBootstrapAdminBranchAccessAsync(ct);

        if (set.Count == 0 || set.Contains("CRM") || set.Contains("SalesShipping") || set.Contains("Finance") || set.Contains("COD"))
            await SeedCustomersAsync(scale, ct);

        if (set.Count == 0 || set.Contains("Catalog") || set.Contains("SalesShipping") || set.Contains("Warehouse") || set.Contains("Inventory"))
            await SeedProductCatalogAsync(ct);

        if (set.Count == 0 || set.Contains("Carrier") || set.Contains("SalesShipping"))
            await SeedCarriersAsync(ct);

        if (set.Count == 0 || set.Contains("Finance") || set.Contains("SalesShipping"))
            await SeedShippingRateCardAsync(ct);

        if (set.Count == 0 || set.Contains("Notifications"))
            await SeedNotificationTemplatesAndReportsAsync(ct);

        if (set.Contains("SalesShipping"))
        {
            await SeedOrdersAndShipmentsAsync(scale, isIncremental: await _db.Orders.AnyAsync(ct), ct);
            await SeedPickupRequestsAsync(scale, isIncremental: await _db.PickupRequests.AnyAsync(ct), ct);
        }
        if (set.Contains("Warehouse"))
            await SeedWarehouseOperationsAsync(scale, isIncremental: await _db.WarehouseReceipts.AnyAsync(ct), ct);
        if (set.Contains("Inventory"))
            await SeedInventoryDataAsync(scale, isIncremental: await _db.InventoryItems.AnyAsync(ct), ct);
        if (set.Contains("Finance"))
            await SeedFinanceDataAsync(isIncremental: await _db.Invoices.AnyAsync(ct), ct);
        if (set.Contains("COD"))
            await SeedCodDataAsync(isIncremental: await _db.CODCollections.AnyAsync(ct), ct);
        if (set.Contains("Notifications") || set.Contains("Audit"))
            await SeedNotificationsJobsAndAuditAsync(scale, isIncremental: await _db.Notifications.AnyAsync(ct), ct);

        await UpdateNumberSequencesToMaxAsync(ct);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Random Helpers
    // ─────────────────────────────────────────────────────────────────────────
    private DateTime DaysAgo(int days) => DateTime.UtcNow.AddDays(-days);
    private T Pick<T>(IList<T> list) => list[_rng.Next(list.Count)];
    private decimal RndDecimal(decimal min, decimal max) => min + (decimal)_rng.NextDouble() * (max - min);
    private int RndInt(int min, int max) => _rng.Next(min, max + 1);

    private async Task BackdateCreatedAtAsync<TEntity>(IReadOnlyList<(long Id, DateTime TargetUtc)> items, CancellationToken ct)
        where TEntity : Domain.Common.BaseEntity
    {
        if (!_db.Database.IsRelational() || items.Count == 0) return;

        // Group by date bucket (day) to perform fast batch ExecuteUpdateAsync without huge SQL IN lists
        var groupedByDay = items
            .GroupBy(x => x.TargetUtc)
            .ToList();

        // To keep SQL calls tiny, batch by day string (max 180 distinct dates)
        foreach (var dayGroup in items.GroupBy(x => x.TargetUtc.Date))
        {
            var dayTs = dayGroup.Key.AddHours(9); // Realistic business time in UTC (~16:00 VN)
            var ids = dayGroup.Select(x => x.Id).Distinct().ToList();
            foreach (var chunk in ids.Chunk(500))
            {
                await _db.Set<TEntity>()
                    .Where(e => chunk.Contains(e.Id))
                    .ExecuteUpdateAsync(s => s.SetProperty(e => e.CreatedAt, dayTs), ct);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 1. Geography (Country + Provinces + Districts + Wards)
    // ─────────────────────────────────────────────────────────────────────────
    private async Task SeedGeographyAsync(CancellationToken ct)
    {
        var vn = await _db.Countries
            .Include(c => c.Provinces).ThenInclude(p => p.Districts).ThenInclude(d => d.Wards)
            .FirstOrDefaultAsync(c => c.Code == "VN", ct);

        var isNewCountry = vn == null;
        vn ??= new Country
        {
            Code = "VN",
            Alpha3Code = "VNM",
            Name = "Vietnam",
            NativeName = "Việt Nam",
            NumericCode = 704,
            PhonePrefix = "+84",
            IsActive = true
        };

        var provinceDefs = new (string Code, string Name, string Type, (string Code, string Name, string Type)[] Districts)[]
        {
            ("79", "TP. Hồ Chí Minh", "Thành phố", [
                ("760", "Quận 1", "Quận"), ("769", "TP. Thủ Đức", "Thành phố"), ("770", "Quận 3", "Quận"),
                ("774", "Quận 5", "Quận"), ("778", "Quận 7", "Quận"), ("765", "Quận Bình Thạnh", "Quận"),
                ("766", "Quận Tân Bình", "Quận"), ("764", "Quận Gò Vấp", "Quận")
            ]),
            ("01", "Hà Nội", "Thành phố", [
                ("001", "Quận Ba Đình", "Quận"), ("002", "Quận Hoàn Kiếm", "Quận"), ("005", "Quận Cầu Giấy", "Quận"),
                ("006", "Quận Đống Đa", "Quận"), ("007", "Quận Hai Bà Trưng", "Quận"), ("009", "Quận Thanh Xuân", "Quận")
            ]),
            ("48", "Đà Nẵng", "Thành phố", [
                ("490", "Quận Liên Chiểu", "Quận"), ("491", "Quận Thanh Khê", "Quận"),
                ("492", "Quận Hải Châu", "Quận"), ("493", "Quận Sơn Trà", "Quận")
            ]),
            ("92", "Cần Thơ", "Thành phố", [
                ("916", "Quận Ninh Kiều", "Quận"), ("918", "Quận Bình Thủy", "Quận"), ("919", "Quận Cái Răng", "Quận")
            ]),
            ("31", "Hải Phòng", "Thành phố", [
                ("303", "Quận Hồng Bàng", "Quận"), ("304", "Quận Ngô Quyền", "Quận"), ("305", "Quận Lê Chân", "Quận")
            ])
        };

        var changed = isNewCountry;
        foreach (var pDef in provinceDefs)
        {
            var prov = vn.Provinces.FirstOrDefault(p => p.Code == pDef.Code);
            if (prov == null)
            {
                prov = new Province { Code = pDef.Code, Name = pDef.Name, Type = pDef.Type, IsActive = true };
                vn.Provinces.Add(prov);
                changed = true;
            }

            foreach (var dDef in pDef.Districts)
            {
                var dist = prov.Districts.FirstOrDefault(d => d.Code == dDef.Code);
                if (dist == null)
                {
                    dist = new District { Code = dDef.Code, Name = dDef.Name, Type = dDef.Type, IsActive = true };
                    dist.Wards.Add(new Ward { Code = $"{dDef.Code}-01", Name = $"Phường 1 ({dDef.Name})", Type = "Phường", IsActive = true });
                    dist.Wards.Add(new Ward { Code = $"{dDef.Code}-02", Name = $"Phường Trung Tâm ({dDef.Name})", Type = "Phường", IsActive = true });
                    prov.Districts.Add(dist);
                    changed = true;
                }
            }
        }

        if (isNewCountry)
            _db.Countries.Add(vn);
        if (changed)
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("DevDataSeeder: seeded/updated country, provinces, districts, and wards.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 2. Currencies
    // ─────────────────────────────────────────────────────────────────────────
    private async Task SeedCurrenciesAsync(CancellationToken ct)
    {
        if (await _db.Currencies.AnyAsync(ct)) return;

        _db.Currencies.AddRange(
            new Currency { Code = "VND", Name = "Việt Nam Đồng", Symbol = "₫", DecimalPlaces = 0, ExchangeRateToDefault = 1m, IsDefault = true, IsActive = true },
            new Currency { Code = "USD", Name = "US Dollar", Symbol = "$", DecimalPlaces = 2, ExchangeRateToDefault = 25000m, IsDefault = false, IsActive = true }
        );
        await _db.SaveChangesAsync(ct);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3. Units of Measure
    // ─────────────────────────────────────────────────────────────────────────
    private async Task SeedUnitsOfMeasureAsync(CancellationToken ct)
    {
        if (await _db.UnitsOfMeasure.AnyAsync(ct)) return;

        _db.UnitsOfMeasure.AddRange(
            new UnitOfMeasure { Code = "PCS", Name = "Piece", Type = "Count", ConversionFactor = 1m, IsBaseUnit = true, IsActive = true },
            new UnitOfMeasure { Code = "KG",  Name = "Kilogram", Type = "Weight", ConversionFactor = 1m, IsBaseUnit = true, IsActive = true },
            new UnitOfMeasure { Code = "G",   Name = "Gram", Type = "Weight", ConversionFactor = 0.001m, IsBaseUnit = false, IsActive = true },
            new UnitOfMeasure { Code = "CBM", Name = "Cubic Metre", Type = "Volume", ConversionFactor = 1m, IsBaseUnit = true, IsActive = true },
            new UnitOfMeasure { Code = "CM",  Name = "Centimetre", Type = "Dimension", ConversionFactor = 0.01m, IsBaseUnit = false, IsActive = true },
            new UnitOfMeasure { Code = "M",   Name = "Metre", Type = "Dimension", ConversionFactor = 1m, IsBaseUnit = true, IsActive = true }
        );
        await _db.SaveChangesAsync(ct);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3b. System Settings
    // ─────────────────────────────────────────────────────────────────────────
    private async Task SeedSystemSettingsAsync(CancellationToken ct)
    {
        var existing = await _db.SystemSettings.Select(s => s.Key).ToHashSetAsync(ct);
        var defaults = new[]
        {
            new SystemSetting { Category = "Finance", Key = "Finance.DefaultVatRatePercent", Value = "8", ValueType = "Decimal", Description = "Thuế GTGT mặc định (%)", UpdatedBy = "DevSeed" },
            new SystemSetting { Category = "Finance", Key = "Finance.DefaultPaymentTermsDays", Value = "30", ValueType = "Integer", Description = "Hạn thanh toán hóa đơn mặc định (ngày)", UpdatedBy = "DevSeed" },
            new SystemSetting { Category = "COD", Key = "Cod.DefaultServiceFeePercent", Value = "1.5", ValueType = "Decimal", Description = "Phí dịch vụ thu hộ COD (%)", UpdatedBy = "DevSeed" },
            new SystemSetting { Category = "Shipping", Key = "Shipping.VolumetricDivisor", Value = "5000", ValueType = "Integer", Description = "Hệ số quy đổi trọng lượng thể tích (LxWxH/5000)", UpdatedBy = "DevSeed" },
            new SystemSetting { Category = "Operations", Key = "Operations.MaxDeliveryAttempts", Value = "3", ValueType = "Integer", Description = "Số lần phát tối đa trước khi hoàn hàng", UpdatedBy = "DevSeed" }
        };

        var toAdd = defaults.Where(d => !existing.Contains(d.Key)).ToList();
        if (toAdd.Count > 0)
        {
            _db.SystemSettings.AddRange(toAdd);
            await _db.SaveChangesAsync(ct);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 4. Number Sequences (matching exact backend Scope keys)
    // ─────────────────────────────────────────────────────────────────────────
    private async Task SeedNumberSequencesAsync(CancellationToken ct)
    {
        var existingScopes = await _db.NumberSequences.Select(n => n.Scope).ToHashSetAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var required = new[]
        {
            new NumberSequence { Scope = "ORDER",      Prefix = "ORD",  NextNumber = 1, Increment = 1, NumberLength = 6, CurrentFormat = "{PREFIX}-{YYYYMM}-{NUMBER}", ResetFrequency = "Monthly", LastResetDate = today },
            new NumberSequence { Scope = "WAYBILL",    Prefix = "WB",   NextNumber = 1, Increment = 1, NumberLength = 8, CurrentFormat = "{PREFIX}{YYYYMMDD}{NUMBER}",  ResetFrequency = "None",    LastResetDate = today },
            new NumberSequence { Scope = "INV",        Prefix = "INV",  NextNumber = 1, Increment = 1, NumberLength = 6, CurrentFormat = "{PREFIX}-{YYYYMM}-{NUMBER}", ResetFrequency = "Monthly", LastResetDate = today },
            new NumberSequence { Scope = "PAY",        Prefix = "PAY",  NextNumber = 1, Increment = 1, NumberLength = 6, CurrentFormat = "{PREFIX}-{YYYYMM}-{NUMBER}", ResetFrequency = "Monthly", LastResetDate = today },
            new NumberSequence { Scope = "FLE",        Prefix = "FLE",  NextNumber = 1, Increment = 1, NumberLength = 6, CurrentFormat = "{PREFIX}-{YYYYMM}-{NUMBER}", ResetFrequency = "Monthly", LastResetDate = today },
            new NumberSequence { Scope = "RECEIPT",    Prefix = "RCP",  NextNumber = 1, Increment = 1, NumberLength = 6, CurrentFormat = "{PREFIX}-{YYYYMM}-{NUMBER}", ResetFrequency = "Monthly", LastResetDate = today },
            new NumberSequence { Scope = "DISPATCH",   Prefix = "DSP",  NextNumber = 1, Increment = 1, NumberLength = 6, CurrentFormat = "{PREFIX}-{YYYYMM}-{NUMBER}", ResetFrequency = "Monthly", LastResetDate = today },
            new NumberSequence { Scope = "ADJUST",     Prefix = "ADJ",  NextNumber = 1, Increment = 1, NumberLength = 6, CurrentFormat = "{PREFIX}-{YYYYMM}-{NUMBER}", ResetFrequency = "Monthly", LastResetDate = today },
            new NumberSequence { Scope = "TRANSFER",   Prefix = "TRF",  NextNumber = 1, Increment = 1, NumberLength = 6, CurrentFormat = "{PREFIX}-{YYYYMM}-{NUMBER}", ResetFrequency = "Monthly", LastResetDate = today },
            new NumberSequence { Scope = "INV-TXN",    Prefix = "TXN",  NextNumber = 1, Increment = 1, NumberLength = 6, CurrentFormat = "{PREFIX}-{YYYYMM}-{NUMBER}", ResetFrequency = "Monthly", LastResetDate = today },
            new NumberSequence { Scope = "PICKUP",     Prefix = "PKP",  NextNumber = 1, Increment = 1, NumberLength = 6, CurrentFormat = "{PREFIX}-{YYYYMM}-{NUMBER}", ResetFrequency = "Monthly", LastResetDate = today },
            new NumberSequence { Scope = "RETURN",     Prefix = "RTN",  NextNumber = 1, Increment = 1, NumberLength = 6, CurrentFormat = "{PREFIX}-{YYYYMM}-{NUMBER}", ResetFrequency = "Monthly", LastResetDate = today },
            new NumberSequence { Scope = "REMITTANCE", Prefix = "REM",  NextNumber = 1, Increment = 1, NumberLength = 6, CurrentFormat = "{PREFIX}-{YYYYMM}-{NUMBER}", ResetFrequency = "Monthly", LastResetDate = today }
        };

        var toAdd = required.Where(s => !existingScopes.Contains(s.Scope)).ToList();
        if (toAdd.Count > 0)
        {
            _db.NumberSequences.AddRange(toAdd);
            await _db.SaveChangesAsync(ct);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 5. Company + Branches + Warehouses
    // ─────────────────────────────────────────────────────────────────────────
    private async Task SeedCompanyBranchesAndWarehousesAsync(CancellationToken ct)
    {
        if (await _db.Companies.AnyAsync(ct)) return;

        var country = await _db.Countries.FirstAsync(ct);
        var provs = await _db.Provinces.Include(p => p.Districts).ToDictionaryAsync(p => p.Code, ct);

        Address MakeFacilityAddress(string name, string phone, string line1, string provCode)
        {
            var prov = provs.GetValueOrDefault(provCode);
            var dist = prov?.Districts.FirstOrDefault();
            return new Address
            {
                ContactName = name,
                PhoneNumber = phone,
                AddressLine1 = line1,
                CountryId = country.Id,
                ProvinceId = prov?.Id,
                DistrictId = dist?.Id,
                FormattedAddress = $"{line1}, {dist?.Name}, {prov?.Name}"
            };
        }

        var company = new Company
        {
            Code = "LMS",
            LegalName = "Công ty TNHH Vận Tải & Logistics Phương Nam (Mẫu)",
            TradingName = "LMS Express Vietnam",
            TaxNumber = "0318889999",
            Email = "contact@lms-logistics.vn",
            Phone = "028-3838-0000",
            Website = "https://lms-logistics.vn",
            IsActive = true
        };

        var hcmBranch = new Branch
        {
            Code = "HCM", Name = "Chi nhánh TP. Hồ Chí Minh", BranchType = "Hub",
            Phone = "028-3838-0001", Email = "hcm@lms-logistics.vn", IsActive = true,
            Address = MakeFacilityAddress("Trụ sở HCM", "028-3838-0001", "128 Nguyễn Văn Linh, Q.7", "79")
        };
        var haBranch = new Branch
        {
            Code = "HAN", Name = "Chi nhánh Hà Nội", BranchType = "Hub",
            Phone = "024-3838-0001", Email = "hanoi@lms-logistics.vn", IsActive = true,
            Address = MakeFacilityAddress("Hub Hà Nội", "024-3838-0001", "55 Giải Phóng, Q. Hai Bà Trưng", "01")
        };
        var danBranch = new Branch
        {
            Code = "DAN", Name = "Chi nhánh Đà Nẵng", BranchType = "Spoke",
            Phone = "0236-3838-0001", Email = "danang@lms-logistics.vn", IsActive = true,
            Address = MakeFacilityAddress("Hub Đà Nẵng", "0236-3838-0001", "210 Nguyễn Văn Thoại, Q. Sơn Trà", "48")
        };
        var ctoBranch = new Branch
        {
            Code = "CTO", Name = "Chi nhánh Cần Thơ", BranchType = "Spoke",
            Phone = "0292-3838-0001", Email = "cantho@lms-logistics.vn", IsActive = true,
            Address = MakeFacilityAddress("Hub Cần Thơ", "0292-3838-0001", "88 Đường 3/2, Q. Ninh Kiều", "92")
        };

        company.Branches.Add(hcmBranch);
        company.Branches.Add(haBranch);
        company.Branches.Add(danBranch);
        company.Branches.Add(ctoBranch);

        hcmBranch.Warehouses.Add(new Warehouse { Code = "WH-HCM-01", Name = "Kho Hồ Chí Minh Trung Tâm", Type = "Hybrid", TotalAreaSqM = 5500m, IsActive = true, Address = MakeFacilityAddress("Kho HCM 01", "028-3838-0101", "Lô C2 KCN Tân Bình", "79") });
        hcmBranch.Warehouses.Add(new Warehouse { Code = "WH-HCM-02", Name = "Hub Phân Loại Bưu Kiện HCM", Type = "ParcelHub", TotalAreaSqM = 3200m, IsActive = true, Address = MakeFacilityAddress("Hub HCM 02", "028-3838-0102", "KCN Cát Lái, TP. Thủ Đức", "79") });
        haBranch.Warehouses.Add(new Warehouse { Code = "WH-HAN-01", Name = "Kho Hà Nội Trung Tâm", Type = "Hybrid", TotalAreaSqM = 4200m, IsActive = true, Address = MakeFacilityAddress("Kho HAN 01", "024-3838-0101", "KCN Ngọc Hồi, Thanh Trì", "01") });
        haBranch.Warehouses.Add(new Warehouse { Code = "WH-HAN-02", Name = "Hub Phân Loại Bưu Kiện Bắc", Type = "ParcelHub", TotalAreaSqM = 2800m, IsActive = true, Address = MakeFacilityAddress("Hub HAN 02", "024-3838-0102", "KCN Quang Minh, Mê Linh", "01") });
        danBranch.Warehouses.Add(new Warehouse { Code = "WH-DAN-01", Name = "Kho Trung Chuyển Đà Nẵng", Type = "Hybrid", TotalAreaSqM = 2200m, IsActive = true, Address = MakeFacilityAddress("Kho DAN 01", "0236-3838-0101", "KCN Hòa Khánh, Liên Chiểu", "48") });
        ctoBranch.Warehouses.Add(new Warehouse { Code = "WH-CTO-01", Name = "Kho Cần Thơ ĐBSCL", Type = "Hybrid", TotalAreaSqM = 1800m, IsActive = true, Address = MakeFacilityAddress("Kho CTO 01", "0292-3838-0101", "KCN Trà Nóc, Bình Thủy", "92") });

        _db.Companies.Add(company);
        await _db.SaveChangesAsync(ct);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 6. Warehouse Zones + Locations
    // ─────────────────────────────────────────────────────────────────────────
    private async Task SeedWarehouseZonesAndLocationsAsync(CancellationToken ct)
    {
        if (await _db.WarehouseZones.AnyAsync(ct)) return;

        var warehouses = await _db.Warehouses.ToListAsync(ct);
        foreach (var wh in warehouses)
        {
            var rcvZone = new WarehouseZone { WarehouseId = wh.Id, Code = $"{wh.Code}-RCV", Name = $"Khu Tiếp Nhận ({wh.Code})", ZoneType = "Receiving", TemperatureType = "Ambient", IsActive = true };
            rcvZone.Locations.Add(new WarehouseLocation { Code = $"{wh.Code}-RCV-01", Aisle = "R", Rack = "01", Shelf = "01", Bin = "01", Barcode = $"{wh.Code}-RCV-01" });
            rcvZone.Locations.Add(new WarehouseLocation { Code = $"{wh.Code}-RCV-02", Aisle = "R", Rack = "01", Shelf = "01", Bin = "02", Barcode = $"{wh.Code}-RCV-02" });

            var strZone = new WarehouseZone { WarehouseId = wh.Id, Code = $"{wh.Code}-STR", Name = $"Khu Lưu Trữ ({wh.Code})", ZoneType = "BulkStorage", TemperatureType = "Ambient", IsActive = true };
            for (var rack = 1; rack <= 3; rack++)
            {
                for (var bin = 1; bin <= 4; bin++)
                {
                    strZone.Locations.Add(new WarehouseLocation
                    {
                        Code = $"{wh.Code}-STR-A{rack:D2}-{bin:D2}",
                        Aisle = "A",
                        Rack = $"{rack:D2}",
                        Shelf = "01",
                        Bin = $"{bin:D2}",
                        Barcode = $"{wh.Code}-STR-A{rack:D2}-{bin:D2}"
                    });
                }
            }

            var hubZone = new WarehouseZone { WarehouseId = wh.Id, Code = $"{wh.Code}-HUB", Name = $"Khu Phân Loại Hub ({wh.Code})", ZoneType = "ParcelSorting", TemperatureType = "Ambient", IsActive = true };
            hubZone.Locations.Add(new WarehouseLocation { Code = $"{wh.Code}-HUB-S01", Aisle = "S", Rack = "01", Shelf = "01", Bin = "01", Barcode = $"{wh.Code}-HUB-S01" });
            hubZone.Locations.Add(new WarehouseLocation { Code = $"{wh.Code}-HUB-S02", Aisle = "S", Rack = "01", Shelf = "01", Bin = "02", Barcode = $"{wh.Code}-HUB-S02" });

            _db.WarehouseZones.AddRange(rcvZone, strZone, hubZone);
        }
        await _db.SaveChangesAsync(ct);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 7. Departments (12 departments across 4 branches)
    // ─────────────────────────────────────────────────────────────────────────
    private async Task SeedDepartmentsAsync(CancellationToken ct)
    {
        if (await _db.Departments.AnyAsync(ct)) return;

        var company = await _db.Companies.FirstAsync(ct);
        var branches = await _db.Branches.ToDictionaryAsync(b => b.Code, ct);

        var defs = new (string Branch, string Code, string Name)[]
        {
            ("HCM", "OPS-HCM", "Vận hành & Điều vận HCM"),
            ("HCM", "FIN-HCM", "Tài chính & Kế toán HCM"),
            ("HCM", "CS-HCM",  "Chăm sóc khách hàng HCM"),
            ("HCM", "WH-HCM",  "Kho vận & Bưu cục HCM"),
            ("HAN", "OPS-HAN", "Vận hành & Điều vận Hà Nội"),
            ("HAN", "FIN-HAN", "Tài chính Hà Nội"),
            ("HAN", "CS-HAN",  "Chăm sóc khách hàng Hà Nội"),
            ("HAN", "WH-HAN",  "Kho vận & Bưu cục Hà Nội"),
            ("DAN", "OPS-DAN", "Vận hành Đà Nẵng"),
            ("DAN", "WH-DAN",  "Kho vận Đà Nẵng"),
            ("CTO", "OPS-CTO", "Vận hành Cần Thơ"),
            ("CTO", "WH-CTO",  "Kho vận Cần Thơ")
        };

        foreach (var (brCode, code, name) in defs)
        {
            if (branches.TryGetValue(brCode, out var br))
            {
                _db.Departments.Add(new Department
                {
                    CompanyId = company.Id,
                    BranchId = br.Id,
                    Code = code,
                    Name = name,
                    IsActive = true
                });
            }
        }
        await _db.SaveChangesAsync(ct);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 8. Employees (36) + User Accounts (26+) + Login History
    // ─────────────────────────────────────────────────────────────────────────
    private async Task SeedEmployeesAndUsersAsync(CancellationToken ct)
    {
        if (await _db.Employees.AnyAsync(ct)) return;

        var branches = await _db.Branches.ToDictionaryAsync(b => b.Code, ct);
        var depts = await _db.Departments.ToDictionaryAsync(d => d.Code, ct);
        var roles = await _db.Roles.ToDictionaryAsync(r => r.Code, ct);
        var today = DateOnly.FromDateTime(DateTime.Today);
        const string defaultPw = "Test@123456";
        var pwHash = _hasher.HashPassword(defaultPw);

        Employee MakeEmp(
            string code, string first, string last, string job, string phone,
            string brCode, string deptCode, string? username, string? roleCode,
            string? extraBranch = null)
        {
            var br = branches[brCode];
            var dept = depts.GetValueOrDefault(deptCode);

            var emp = new Employee
            {
                BranchId = br.Id,
                DepartmentId = dept?.Id,
                Code = code,
                FirstName = first,
                LastName = last,
                FullName = $"{last} {first}",
                Email = username != null ? $"{username}@lms-logistics.vn" : $"{code.ToLowerInvariant()}@lms-logistics.vn",
                PhoneNumber = phone,
                Gender = RndInt(0, 1) == 0 ? "Male" : "Female",
                JobTitle = job,
                HireDate = today.AddDays(-RndInt(180, 1400)),
                Status = "Active"
            };

            if (!string.IsNullOrEmpty(username))
            {
                var acc = new UserAccount
                {
                    Username = username,
                    Email = emp.Email!,
                    PasswordHash = pwHash,
                    SecurityStamp = Guid.NewGuid().ToString("N"),
                    IsActive = true,
                    LastLoginAt = DaysAgo(RndInt(0, 5)),
                    CreatedBy = "DevSeed"
                };

                if (roleCode != null && roles.TryGetValue(roleCode, out var role))
                {
                    acc.UserRoles.Add(new UserRole { RoleId = role.Id, AssignedBy = "DevSeed", AssignedAt = DaysAgo(180) });
                }

                acc.BranchAccesses.Add(new UserBranchAccess { BranchId = br.Id, IsDefault = true, GrantedAt = DaysAgo(180), GrantedBy = "DevSeed" });
                if (extraBranch != null && branches.TryGetValue(extraBranch, out var exBr))
                {
                    acc.BranchAccesses.Add(new UserBranchAccess { BranchId = exBr.Id, IsDefault = false, GrantedAt = DaysAgo(180), GrantedBy = "DevSeed" });
                }

                acc.LoginHistories.Add(new LoginHistory
                {
                    UsernameAttempted = username,
                    IpAddress = $"10.10.{RndInt(1, 20)}.{RndInt(10, 240)}",
                    UserAgent = "Logistics.Client.WinForms/2.0",
                    IsSuccessful = true,
                    AttemptedAt = DaysAgo(RndInt(0, 3))
                });

                emp.UserAccount = acc;
            }

            return emp;
        }

        // Core role accounts (preserving exact usernames documented in DevSeed) + rich regional team (36 total)
        var employees = new List<Employee>
        {
            MakeEmp("EMP-HCM-MGR",  "Minh",   "Nguyễn", "Trưởng Chi nhánh HCM",     "0901000001", "HCM", "OPS-HCM", "branch.manager",  AppRoles.BranchManager, "HAN"),
            MakeEmp("EMP-HCM-OPS",  "Hoa",    "Trần",   "Quản lý Vận hành HCM",     "0901000002", "HCM", "OPS-HCM", "ops.manager",     AppRoles.OperationsManager),
            MakeEmp("EMP-HCM-FIN",  "Long",   "Lê",     "Kế toán Công nợ & COD",    "0901000003", "HCM", "FIN-HCM", "finance.staff",   AppRoles.FinanceStaff),
            MakeEmp("EMP-HCM-CS",   "Lan",    "Phạm",   "Chuyên viên CSKH",         "0901000004", "HCM", "CS-HCM",  "cs.staff",        AppRoles.CustomerService),
            MakeEmp("EMP-HCM-WH",   "Tuấn",   "Vũ",     "Nhân viên Kho & Giao nhận","0901000005", "HCM", "WH-HCM",  "wh.staff",        AppRoles.WarehouseStaff),
            MakeEmp("EMP-HCM-WH2",  "Dũng",   "Đinh",   "Tài xế Giao nhận HCM",     "0901000006", "HCM", "WH-HCM",  "wh.staff2",       AppRoles.WarehouseStaff),
            MakeEmp("EMP-HCM-RO",   "An",     "Đỗ",     "Kiểm soát Nội bộ",         "0901000007", "HCM", "OPS-HCM", "viewer",          AppRoles.ReadOnly),
            MakeEmp("EMP-HAN-MGR",  "Bình",   "Hoàng",  "Trưởng Chi nhánh Hà Nội",  "0902000001", "HAN", "OPS-HAN", "hanoi.manager",   AppRoles.BranchManager),
            MakeEmp("EMP-HAN-WH",   "Tùng",   "Bùi",    "Nhân viên Kho Hà Nội",     "0902000002", "HAN", "WH-HAN",  "hanoi.wh",        AppRoles.WarehouseStaff),
            MakeEmp("EMP-DAN-OPS",  "Linh",   "Dương",  "Điều phối Vận hành ĐN",    "0903000001", "DAN", "OPS-DAN", "danang.ops",      AppRoles.OperationsManager),

            // Extended regional staff (26 more = 36 employees, 16 more accounts = 26 user accounts)
            MakeEmp("EMP-HCM-DRV1", "Hùng",   "Trương", "Tài xế Tuyến Q1-Q3",       "0901000011", "HCM", "OPS-HCM", "driver.hcm1",     AppRoles.WarehouseStaff),
            MakeEmp("EMP-HCM-DRV2", "Khang",  "Huỳnh",  "Tài xế Tuyến Thủ Đức",     "0901000012", "HCM", "OPS-HCM", "driver.hcm2",     AppRoles.WarehouseStaff),
            MakeEmp("EMP-HCM-DRV3", "Nam",    "Võ",     "Tài xế Tuyến Bình Thạnh",  "0901000013", "HCM", "OPS-HCM", "driver.hcm3",     AppRoles.WarehouseStaff),
            MakeEmp("EMP-HCM-DRV4", "Phúc",   "Đặng",   "Tài xế Tuyến Q7-Nhà Bè",   "0901000014", "HCM", "OPS-HCM", null,              null),
            MakeEmp("EMP-HCM-WH3",  "Hải",    "Ngô",    "Thủ kho Chính HCM",        "0901000015", "HCM", "WH-HCM",  "wh.hcm3",         AppRoles.WarehouseStaff),
            MakeEmp("EMP-HCM-WH4",  "Thảo",   "Bùi",    "Nhân viên Phân loại Hub",  "0901000016", "HCM", "WH-HCM",  null,              null),
            MakeEmp("EMP-HCM-FIN2", "Trang",  "Phan",   "Thủ quỹ Chi nhánh HCM",    "0901000017", "HCM", "FIN-HCM", "cashier.hcm",     AppRoles.FinanceStaff),
            MakeEmp("EMP-HCM-CS2",  "Ngọc",   "Lý",     "Nhân viên CSKH Doanh nghiệp","0901000018","HCM","CS-HCM",  "cs.hcm2",         AppRoles.CustomerService),
            MakeEmp("EMP-HCM-OPS2", "Quân",   "Tạ",     "Điều vận Tuyến Miền Nam",  "0901000019", "HCM", "OPS-HCM", "ops.hcm2",        AppRoles.OperationsManager),
            MakeEmp("EMP-HCM-RO2",  "Sơn",    "Mai",    "Chuyên viên Báo cáo",      "0901000020", "HCM", "FIN-HCM", null,              null),

            MakeEmp("EMP-HAN-OPS",  "Cường",  "Đào",    "Điều phối Vận hành HN",    "0902000011", "HAN", "OPS-HAN", "ops.hanoi",       AppRoles.OperationsManager),
            MakeEmp("EMP-HAN-FIN",  "Hà",     "Vương",  "Kế toán Hà Nội",           "0902000012", "HAN", "FIN-HAN", "fin.hanoi",       AppRoles.FinanceStaff),
            MakeEmp("EMP-HAN-CS",   "Hương",  "Chu",    "CSKH Hà Nội",              "0902000013", "HAN", "CS-HAN",  "cs.hanoi",        AppRoles.CustomerService),
            MakeEmp("EMP-HAN-DRV1", "Thắng",  "Trịnh",  "Tài xế Nội thành HN 1",    "0902000014", "HAN", "OPS-HAN", "driver.han1",     AppRoles.WarehouseStaff),
            MakeEmp("EMP-HAN-DRV2", "Đức",    "Lương",  "Tài xế Nội thành HN 2",    "0902000015", "HAN", "OPS-HAN", "driver.han2",     AppRoles.WarehouseStaff),
            MakeEmp("EMP-HAN-WH2",  "Vinh",   "Cao",    "Nhân viên Kiểm hàng HN",   "0902000016", "HAN", "WH-HAN",  null,              null),
            MakeEmp("EMP-HAN-WH3",  "Lâm",    "Phùng",  "Nhân viên Xuất nhập HN",   "0902000017", "HAN", "WH-HAN",  null,              null),

            MakeEmp("EMP-DAN-MGR",  "Khánh",  "Nguyễn", "Trưởng Chi nhánh ĐN",      "0903000011", "DAN", "OPS-DAN", "danang.manager",  AppRoles.BranchManager),
            MakeEmp("EMP-DAN-WH",   "Huy",    "Trần",   "Thủ kho Đà Nẵng",          "0903000012", "DAN", "WH-DAN",  "danang.wh",       AppRoles.WarehouseStaff),
            MakeEmp("EMP-DAN-DRV1", "Tiến",   "Lê",     "Tài xế Đà Nẵng",           "0903000013", "DAN", "OPS-DAN", null,              null),
            MakeEmp("EMP-DAN-DRV2", "Trung",  "Phạm",   "Nhân viên Giao nhận ĐN",   "0903000014", "DAN", "WH-DAN",  null,              null),

            MakeEmp("EMP-CTO-MGR",  "Tài",    "Huỳnh",  "Trưởng Chi nhánh Cần Thơ", "0904000001", "CTO", "OPS-CTO", "cantho.manager",  AppRoles.BranchManager),
            MakeEmp("EMP-CTO-OPS",  "Vy",     "Nguyễn", "Điều vận Cần Thơ",         "0904000002", "CTO", "OPS-CTO", "cantho.ops",      AppRoles.OperationsManager),
            MakeEmp("EMP-CTO-WH",   "Nhân",   "Võ",     "Thủ kho Cần Thơ",          "0904000003", "CTO", "WH-CTO",  "cantho.wh",       AppRoles.WarehouseStaff),
            MakeEmp("EMP-CTO-DRV1", "Phong",  "Trần",   "Tài xế Cần Thơ",           "0904000004", "CTO", "OPS-CTO", null,              null),
            MakeEmp("EMP-CTO-DRV2", "Quý",    "Lâm",    "Nhân viên Giao hàng ĐBSCL","0904000005", "CTO", "WH-CTO",  null,              null)
        };

        _db.Employees.AddRange(employees);
        await _db.SaveChangesAsync(ct);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 9. Grant Admin Bootstrap account branch access
    // ─────────────────────────────────────────────────────────────────────────
    private async Task EnsureBootstrapAdminBranchAccessAsync(CancellationToken ct)
    {
        var admin = await _db.UserAccounts
            .Include(u => u.BranchAccesses)
            .FirstOrDefaultAsync(u => u.Username == "admin", ct);
        if (admin == null) return;

        var branches = await _db.Branches.ToListAsync(ct);
        var existingBranchIds = admin.BranchAccesses.Select(a => a.BranchId).ToHashSet();
        var added = 0;

        foreach (var br in branches.Where(b => !existingBranchIds.Contains(b.Id)))
        {
            admin.BranchAccesses.Add(new UserBranchAccess
            {
                BranchId = br.Id,
                IsDefault = existingBranchIds.Count == 0 && added == 0,
                GrantedAt = DateTime.UtcNow,
                GrantedBy = "DevSeed"
            });
            added++;
        }

        if (added > 0)
            await _db.SaveChangesAsync(ct);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 10. Customers (120 Standard / 200 Large) + Contacts + Addresses + Contracts + Rules
    // ─────────────────────────────────────────────────────────────────────────
    private async Task SeedCustomersAsync(DevSeedScale scale, CancellationToken ct)
    {
        if (await _db.Customers.AnyAsync(ct)) return;

        var branches = await _db.Branches.ToListAsync(ct);
        var country = await _db.Countries.FirstAsync(ct);
        var provinces = await _db.Provinces.Include(p => p.Districts).ThenInclude(d => d.Wards).ToListAsync(ct);
        var csEmployees = await _db.Employees.Where(e => e.JobTitle != null && e.JobTitle.Contains("CSKH")).ToListAsync(ct);
        if (csEmployees.Count == 0) csEmployees = await _db.Employees.Take(2).ToListAsync(ct);

        var provByBranch = new Dictionary<string, Province>
        {
            ["HCM"] = provinces.FirstOrDefault(p => p.Code == "79") ?? provinces[0],
            ["HAN"] = provinces.FirstOrDefault(p => p.Code == "01") ?? provinces[0],
            ["DAN"] = provinces.FirstOrDefault(p => p.Code == "48") ?? provinces[0],
            ["CTO"] = provinces.FirstOrDefault(p => p.Code == "92") ?? provinces[0]
        };

        var streets = new[]
        {
            "Lê Duẩn", "Nguyễn Huệ", "Trần Hưng Đạo", "Lý Thường Kiệt", "Hai Bà Trưng",
            "Điện Biên Phủ", "Nguyễn Thị Minh Khai", "Cách Mạng Tháng 8", "Nguyễn Trãi",
            "Lê Lợi", "Hoàng Diệu", "Võ Văn Kiệt", "Phạm Văn Đồng", "Nguyễn Văn Cừ"
        };

        var bizPrefixes = new[]
        {
            "Công ty CP Thương mại", "Công ty TNHH Phân phối", "Công ty CP Điện máy",
            "Công ty TNHH Thời trang", "Công ty CP Thực phẩm", "Công ty TNHH Mỹ phẩm",
            "Công ty CP Dược phẩm", "Công ty TNHH Gia dụng", "Công ty CP Công nghệ",
            "Hệ thống Bán lẻ"
        };
        var bizRoots = new[]
        {
            "An Phát", "Thành Công", "Hoàng Gia", "Thiên Long", "Nam Việt",
            "Đông Dương", "Hưng Thịnh", "Kim Cương", "Phú Quý", "Đại Dương",
            "Minh Châu", "Bình Minh", "Sài Gòn Mới", "Hà Thành", "Miền Trung"
        };

        var ho = new[] { "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Huỳnh", "Phan", "Vũ", "Võ", "Đặng", "Bùi", "Đỗ" };
        var dem = new[] { "Văn", "Thị", "Minh", "Hoàng", "Thanh", "Hồng", "Quốc", "Gia", "Ngọc", "Phương" };
        var ten = new[] { "Anh", "Bình", "Châu", "Dũng", "Giang", "Hà", "Hải", "Hùng", "Khánh", "Lan", "Linh", "Long", "Mai", "Nam", "Nhi", "Phong", "Phúc", "Quân", "Thảo", "Tuấn", "Tú", "Vân" };

        var targetTotal = scale == DevSeedScale.Large ? 200 : 120;
        var corpCount = (int)(targetTotal * 0.65);

        Address MakeAddress(string contactName, string phone, Province prov)
        {
            var dist = prov.Districts.Count > 0 ? Pick(prov.Districts.ToList()) : null;
            var ward = dist != null && dist.Wards.Count > 0 ? Pick(dist.Wards.ToList()) : null;
            var line1 = $"{RndInt(1, 520)} {Pick(streets)}";
            return new Address
            {
                ContactName = contactName,
                PhoneNumber = phone,
                AddressLine1 = line1,
                CountryId = country.Id,
                ProvinceId = prov.Id,
                DistrictId = dist?.Id,
                WardId = ward?.Id,
                FormattedAddress = $"{line1}, {ward?.Name}, {dist?.Name}, {prov.Name}"
            };
        }

        var customers = new List<Customer>();
        for (var i = 0; i < targetTotal; i++)
        {
            var isCorp = i < corpCount;
            var br = branches[i % branches.Count];
            var prov = provByBranch.GetValueOrDefault(br.Code, provinces[0]);
            var phone = isCorp
                ? $"028-38{RndInt(10, 99)}-{RndInt(1000, 9999)}"
                : $"09{RndInt(0, 8)}{RndInt(1000000, 9999999)}";

            var name = isCorp
                ? $"{bizPrefixes[i % bizPrefixes.Length]} {bizRoots[(i / bizPrefixes.Length) % bizRoots.Length]} {(i / 20 > 0 ? (i / 20).ToString() : "")}".Trim()
                : $"{Pick(ho)} {Pick(dem)} {Pick(ten)}";

            var code = isCorp
                ? $"CUS-{br.Code}-{i + 1:D3}"
                : $"CUS-IND-{i + 1:D3}";

            var payType = isCorp
                ? (i % 4 == 0 ? "CodOffset" : "PostpaidMonthly")
                : (i % 3 == 0 ? "CodOffset" : "Prepaid");

            var cus = new Customer
            {
                BranchId = br.Id,
                CustomerCode = code,
                CustomerType = isCorp ? "Corporate" : "Individual",
                Name = name,
                TradeName = isCorp ? name.Replace("Công ty CP ", "").Replace("Công ty TNHH ", "") : null,
                TaxCode = isCorp ? $"03{10000000 + i}" : null,
                Email = $"contact.{code.ToLowerInvariant()}@demo-customer.vn",
                PhoneNumber = phone,
                CreditLimit = isCorp ? Pick(new[] { 30_000_000m, 50_000_000m, 100_000_000m, 200_000_000m }) : 0m,
                CreditTermDays = isCorp ? Pick(new[] { 15, 30, 45 }) : 0,
                PaymentType = payType,
                AccountManagerEmployeeId = Pick(csEmployees).Id,
                IsActive = i % 25 != 24 // ~96% active
            };

            // Contacts (1-2 per customer -> ~180-300)
            var contactName1 = $"{Pick(ho)} {Pick(dem)} {Pick(ten)}";
            cus.Contacts.Add(new CustomerContact
            {
                ContactName = contactName1,
                JobTitle = isCorp ? "Trưởng phòng Giao nhận" : "Chủ shop",
                PhoneNumber = $"090{1000000 + i}",
                Email = $"c1.{code.ToLowerInvariant()}@demo-customer.vn",
                IsPrimary = true
            });
            if (isCorp)
            {
                cus.Contacts.Add(new CustomerContact
                {
                    ContactName = $"{Pick(ho)} {Pick(dem)} {Pick(ten)}",
                    JobTitle = "Kế toán công nợ",
                    PhoneNumber = $"091{1000000 + i}",
                    Email = $"ar.{code.ToLowerInvariant()}@demo-customer.vn",
                    IsPrimary = false
                });
            }

            // Addresses (2 per customer -> ~240-400)
            cus.CustomerAddresses.Add(new CustomerAddress
            {
                Address = MakeAddress(contactName1, cus.PhoneNumber, prov),
                AddressType = "Sender",
                IsDefault = true,
                AddressLabel = "Kho lấy hàng chính"
            });
            cus.CustomerAddresses.Add(new CustomerAddress
            {
                Address = MakeAddress(contactName1, cus.PhoneNumber, Pick(provinces)),
                AddressType = isCorp ? "Billing" : "Recipient",
                IsDefault = false,
                AddressLabel = isCorp ? "Văn phòng thanh toán" : "Địa chỉ phụ"
            });

            // Contracts + Pricing rules for corporate customers (~40-60)
            if (isCorp && i < 55)
            {
                var startDate = DateOnly.FromDateTime(DaysAgo(RndInt(90, 240)));
                var contract = new CustomerContract
                {
                    ContractNumber = $"CTR-{code}-{DateTime.UtcNow.Year}",
                    Title = $"Hợp đồng dịch vụ vận tải {name}",
                    StartDate = startDate,
                    EndDate = startDate.AddYears(1),
                    Status = "Active",
                    DiscountRate = Pick(new[] { 3m, 5m, 8m, 10m }),
                    PaymentTermDays = cus.CreditTermDays > 0 ? cus.CreditTermDays : 30,
                    SignedAt = startDate
                };
                cus.Contracts.Add(contract);

                cus.PricingRules.Add(new CustomerPricingRule
                {
                    CustomerContract = contract,
                    Name = $"Ưu đãi đối tác {cus.CustomerCode}",
                    DiscountPercent = contract.DiscountRate,
                    FlatDiscountAmount = 0m,
                    Priority = 1,
                    EffectiveFrom = startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                    IsActive = true
                });
            }

            customers.Add(cus);
        }

        _db.Customers.AddRange(customers);
        await _db.SaveChangesAsync(ct);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 11. Product Catalog (10 categories + 55 products)
    // ─────────────────────────────────────────────────────────────────────────
    private async Task SeedProductCatalogAsync(CancellationToken ct)
    {
        if (await _db.ProductCategories.AnyAsync(ct)) return;

        var pcsUom = await _db.UnitsOfMeasure.FirstAsync(u => u.Code == "PCS", ct);
        var kgUom = await _db.UnitsOfMeasure.FirstAsync(u => u.Code == "KG", ct);
        var vnd = await _db.Currencies.FirstAsync(c => c.Code == "VND", ct);

        var cats = new Dictionary<string, ProductCategory>
        {
            ["ELEC"]   = new() { Code = "CAT-ELEC",    Name = "Điện tử - Điện máy",         IsActive = true },
            ["FASH"]   = new() { Code = "CAT-FASHION", Name = "Thời trang & Phụ kiện",      IsActive = true },
            ["FOOD"]   = new() { Code = "CAT-FOOD",    Name = "Thực phẩm & Đồ uống",        IsActive = true },
            ["DOC"]    = new() { Code = "CAT-DOC",     Name = "Tài liệu - Văn phòng phẩm",  IsActive = true },
            ["BULK"]   = new() { Code = "CAT-BULK",    Name = "Hàng kiện - Thùng lớn",      IsActive = true },
            ["HLTH"]   = new() { Code = "CAT-HEALTH",  Name = "Sức khỏe & Mỹ phẩm",         IsActive = true },
            ["HOME"]   = new() { Code = "CAT-HOME",    Name = "Gia dụng & Nội thất",        IsActive = true },
            ["AUTO"]   = new() { Code = "CAT-AUTO",    Name = "Phụ tùng Xe & Công nghiệp",  IsActive = true },
            ["BOOK"]   = new() { Code = "CAT-BOOK",    Name = "Sách & Thiết bị giáo dục",   IsActive = true },
            ["SPRT"]   = new() { Code = "CAT-SPORT",   Name = "Thể thao & Dã ngoại",        IsActive = true }
        };

        _db.ProductCategories.AddRange(cats.Values);
        await _db.SaveChangesAsync(ct);

        Product P(string cat, string sku, string name, decimal w, decimal l, decimal wi, decimal h, decimal val, bool kg = false, bool perish = false, bool fragile = false)
            => new()
            {
                CategoryId = cats[cat].Id,
                Sku = sku,
                Name = name,
                Barcode = $"893{sku.Replace("-", "")}",
                UnitOfMeasureId = kg ? kgUom.Id : pcsUom.Id,
                CurrencyId = vnd.Id,
                WeightKg = w,
                LengthCm = l,
                WidthCm = wi,
                HeightCm = h,
                DeclaredValue = val,
                IsPerishable = perish,
                IsActive = true
            };

        var products = new[]
        {
            P("ELEC", "ELEC-001", "Điện thoại thông minh 6.5\"", 0.25m, 17, 9, 3, 6_500_000m, fragile: true),
            P("ELEC", "ELEC-002", "Máy tính bảng 11\"", 0.55m, 26, 18, 2, 9_800_000m, fragile: true),
            P("ELEC", "ELEC-003", "Laptop Văn phòng 15.6\"", 2.1m, 38, 26, 5, 19_500_000m, fragile: true),
            P("ELEC", "ELEC-004", "Tai nghe Bluetooth ANC", 0.3m, 20, 18, 8, 2_400_000m),
            P("ELEC", "ELEC-005", "Đồng hồ thông minh Sport", 0.15m, 14, 10, 4, 3_600_000m),
            P("ELEC", "ELEC-006", "Màn hình LED 27\"", 5.2m, 65, 42, 14, 5_900_000m, fragile: true),
            P("ELEC", "ELEC-007", "Sạc dự phòng 20000mAh", 0.45m, 16, 8, 3, 650_000m),

            P("FASH", "FASH-001", "Áo thun Cotton Unisex", 0.3m, 30, 22, 4, 320_000m),
            P("FASH", "FASH-002", "Quần Jeans Nam Slimfit", 0.65m, 35, 25, 5, 620_000m),
            P("FASH", "FASH-003", "Giày Sneaker Thể thao", 1.1m, 33, 21, 14, 1_350_000m),
            P("FASH", "FASH-004", "Túi xách Da Công sở", 0.7m, 32, 12, 24, 950_000m),
            P("FASH", "FASH-005", "Áo khoác Gió Chống nước", 0.5m, 34, 24, 6, 780_000m),
            P("FASH", "FASH-006", "Vali Kéo Nhựa ABS 20\"", 3.1m, 55, 36, 23, 1_650_000m),

            P("FOOD", "FOOD-001", "Cà phê Arabica Rang Xay 500g", 0.55m, 20, 11, 6, 195_000m, perish: true),
            P("FOOD", "FOOD-002", "Trái cây sấy dẻo 1kg", 1.05m, 26, 20, 10, 260_000m, kg: true, perish: true),
            P("FOOD", "FOOD-003", "Trà Ô Long Cao Cấp 250g", 0.35m, 18, 12, 8, 310_000m, perish: true),
            P("FOOD", "FOOD-004", "Hạt điều rang muối 1kg", 1.1m, 22, 16, 10, 340_000m, kg: true, perish: true),
            P("FOOD", "FOOD-005", "Mật ong Rừng Nguyên chất 1L", 1.4m, 12, 12, 28, 420_000m, perish: true, fragile: true),

            P("DOC",  "DOC-001",  "Hồ sơ Hợp đồng A4", 0.4m, 32, 23, 2, 50_000m),
            P("DOC",  "DOC-002",  "Chứng từ Hóa đơn Chuyển phát", 0.15m, 25, 18, 1, 20_000m),
            P("DOC",  "DOC-003",  "Thùng Giấy in A4 (5 Gram)", 12.5m, 31, 22, 26, 425_000m),
            P("DOC",  "DOC-004",  "Catalogue Ấn phẩm Marketing", 2.0m, 32, 24, 10, 300_000m),

            P("BULK", "BULK-001", "Thùng hàng Thương mại 20kg", 20.0m, 60, 40, 40, 2_200_000m, kg: true),
            P("BULK", "BULK-002", "Kiện vật tư Đóng gói 15kg", 15.0m, 50, 40, 35, 1_100_000m, kg: true),
            P("BULK", "BULK-003", "Pallet Linh kiện Lắp ráp", 35.0m, 80, 60, 50, 8_500_000m, kg: true),
            P("BULK", "BULK-004", "Thùng Nguyên liệu Pha chế 10kg", 10.2m, 45, 35, 30, 1_450_000m, kg: true),

            P("HLTH", "HLTH-001", "Thực phẩm bảo vệ sức khỏe Vitamin", 0.3m, 14, 9, 9, 540_000m),
            P("HLTH", "HLTH-002", "Bộ Dưỡng da Chuyên sâu", 0.45m, 20, 15, 8, 1_150_000m, fragile: true),
            P("HLTH", "HLTH-003", "Máy đo huyết áp Điện tử", 0.6m, 18, 14, 10, 1_250_000m),
            P("HLTH", "HLTH-004", "Kem chống nắng SPF50+", 0.18m, 14, 6, 4, 380_000m),
            P("HLTH", "HLTH-005", "Nước hoa Cao cấp 100ml", 0.35m, 14, 10, 8, 2_200_000m, fragile: true),

            P("HOME", "HOME-001", "Bộ Nồi Inox 3 Đáy (3 chiếc)", 3.5m, 42, 32, 26, 1_550_000m),
            P("HOME", "HOME-002", "Bình Giữ nhiệt Inox 1L", 0.55m, 11, 11, 30, 360_000m),
            P("HOME", "HOME-003", "Nồi Chiên Không dầu 6L", 5.8m, 38, 36, 40, 2_350_000m, fragile: true),
            P("HOME", "HOME-004", "Máy Lọc không khí Mini", 3.2m, 28, 28, 42, 2_750_000m, fragile: true),
            P("HOME", "HOME-005", "Bộ Chăn Ga Cotton Cao cấp", 2.4m, 45, 35, 18, 1_290_000m),
            P("HOME", "HOME-006", "Đèn Bàn LED Chống cận", 1.1m, 40, 18, 14, 490_000m),

            P("AUTO", "AUTO-001", "Bộ Dụng cụ Sửa chữa 46 chi tiết", 2.6m, 32, 22, 8, 690_000m),
            P("AUTO", "AUTO-002", "Dầu nhớt Tổng hợp 4L", 3.9m, 24, 16, 30, 750_000m),
            P("AUTO", "AUTO-003", "Camera Hành trình Ô tô 2K", 0.45m, 18, 14, 8, 1_850_000m, fragile: true),
            P("AUTO", "AUTO-004", "Bơm Lốp Điện tử Mini", 0.85m, 20, 14, 9, 620_000m),
            P("AUTO", "AUTO-005", "Mũ Bảo hiểm Fullface", 1.5m, 36, 28, 28, 1_450_000m),

            P("BOOK", "BOOK-001", "Combo Sách Quản trị Kinh doanh", 1.8m, 25, 18, 12, 480_000m),
            P("BOOK", "BOOK-002", "Balo Học sinh Chống gù", 0.9m, 38, 28, 16, 650_000m),
            P("BOOK", "BOOK-003", "Bảng Vẽ Điện tử 10\"", 0.8m, 35, 24, 3, 1_550_000m, fragile: true),
            P("BOOK", "BOOK-004", "Máy Tính Khoa học", 0.25m, 19, 10, 3, 590_000m),

            P("SPRT", "SPRT-001", "Thảm Tập Yoga TPE 8mm", 1.2m, 62, 15, 15, 390_000m),
            P("SPRT", "SPRT-002", "Vợt Cầu lông Carbon", 0.35m, 68, 24, 4, 1_250_000m),
            P("SPRT", "SPRT-003", "Lều Cắm trại Tự bung 4 người", 3.6m, 78, 18, 18, 1_450_000m),
            P("SPRT", "SPRT-004", "Tạ Tay Bọc cao su 5kg (Cặp)", 10.0m, 30, 22, 16, 680_000m, kg: true),
            P("SPRT", "SPRT-005", "Bình Nước Thể thao 1.5L", 0.35m, 12, 12, 29, 220_000m),

            P("ELEC", "ELEC-008", "Bàn phím Cơ Không dây", 0.95m, 36, 16, 5, 1_650_000m),
            P("ELEC", "ELEC-009", "Chuột Gaming Wireless", 0.2m, 16, 11, 6, 890_000m),
            P("HOME", "HOME-007", "Ấm Siêu tốc Thủy tinh 1.8L", 1.3m, 24, 20, 26, 450_000m, fragile: true),
            P("FASH", "FASH-007", "Kính Mát Phân cực UV400", 0.15m, 17, 7, 5, 520_000m, fragile: true)
        };

        _db.Products.AddRange(products);
        await _db.SaveChangesAsync(ct);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 12. Carriers, Services, Configs & Tracking Mappings
    // ─────────────────────────────────────────────────────────────────────────
    private async Task SeedCarriersAsync(CancellationToken ct)
    {
        if (await _db.Carriers.AnyAsync(ct)) return;

        Domain.Entities.Carrier.Carrier MakeCarrier(string code, string name, string phone, string email, string api, string trackUrl, params (string sc, string sn, int hrs, decimal maxKg)[] svcs)
        {
            var c = new Domain.Entities.Carrier.Carrier
            {
                Code = code, Name = name, ContactPhone = phone, ContactEmail = email,
                ApiBaseUrl = api, TrackingUrlTemplate = trackUrl, IsActive = true
            };
            foreach (var (sc, sn, hrs, maxKg) in svcs)
                c.Services.Add(new CarrierService { ServiceCode = sc, ServiceName = sn, EstimatedTransitHours = hrs, MaxWeightKg = maxKg, IsCodSupported = true, IsInsuranceSupported = true, IsActive = true });

            c.Configurations.Add(new CarrierConfiguration
            {
                Environment = "Sandbox",
                ApiKey = $"DEV-SANDBOX-KEY-{code}",
                ClientId = $"LMS-{code}-CLIENT",
                WebhookSecret = $"DEV-WEBHOOK-SECRET-{code}",
                IsActive = true,
                UpdatedBy = "DevSeed"
            });

            c.TrackingMappings.Add(new CarrierTrackingMapping { ExternalStatusCode = "PICKED", ExternalStatusName = "Đã lấy hàng", InternalShipmentStatus = "PickedUp", IsActive = true });
            c.TrackingMappings.Add(new CarrierTrackingMapping { ExternalStatusCode = "SORTING", ExternalStatusName = "Đang phân loại", InternalShipmentStatus = "InSortingHub", IsActive = true });
            c.TrackingMappings.Add(new CarrierTrackingMapping { ExternalStatusCode = "TRANSIT", ExternalStatusName = "Đang luân chuyển", InternalShipmentStatus = "InTransit", IsActive = true });
            c.TrackingMappings.Add(new CarrierTrackingMapping { ExternalStatusCode = "DELIVERING", ExternalStatusName = "Đang giao hàng", InternalShipmentStatus = "OutForDelivery", IsActive = true });
            c.TrackingMappings.Add(new CarrierTrackingMapping { ExternalStatusCode = "DELIVERED", ExternalStatusName = "Giao thành công", InternalShipmentStatus = "Delivered", IsActive = true });
            c.TrackingMappings.Add(new CarrierTrackingMapping { ExternalStatusCode = "FAILED", ExternalStatusName = "Giao thất bại", InternalShipmentStatus = "DeliveryFailed", IsActive = true });
            return c;
        }

        _db.Carriers.AddRange(
            MakeCarrier("GHN", "Giao Hàng Nhanh", "1900-636677", "cskh@ghn.vn", "https://sandbox.ghn.vn/api", "https://donhang.ghn.vn/?order_code={TRACKING}",
                ("GHN-STD", "GHN Chuẩn", 48, 50m), ("GHN-EXP", "GHN Hỏa Tốc", 12, 20m)),
            MakeCarrier("GHTK", "Giao Hàng Tiết Kiệm", "1900-6092", "cskh@ghtk.vn", "https://sandbox.ghtk.vn/services", "https://i.ghtk.vn/{TRACKING}",
                ("GHTK-ECO", "GHTK Tiết Kiệm", 72, 30m), ("GHTK-STD", "GHTK Chuẩn", 48, 30m)),
            MakeCarrier("VTPOST", "Viettel Post", "1900-8095", "support@viettelpost.vn", "https://partner.viettelpost.vn/v2", "https://viettelpost.com.vn/tra-cuu-hanh-trinh-don/{TRACKING}",
                ("VTP-VCN", "VTP Chuyển phát nhanh", 24, 100m), ("VTP-VTK", "VTP Tiết kiệm", 72, 100m)),
            MakeCarrier("JNT", "J&T Express Vietnam", "1900-1088", "cskh@jtexpress.vn", "https://sandbox.jtexpress.vn/api", "https://jtexpress.vn/track?billcode={TRACKING}",
                ("JNT-STD", "J&T Standard", 48, 50m), ("JNT-FAST", "J&T Fast", 24, 30m))
        );
        await _db.SaveChangesAsync(ct);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 13. Shipping Rate Cards & Rules
    // ─────────────────────────────────────────────────────────────────────────
    private async Task SeedShippingRateCardAsync(CancellationToken ct)
    {
        if (await _db.ShippingRateCards.AnyAsync(ct)) return;

        var vnd = await _db.Currencies.FirstAsync(c => c.Code == "VND", ct);
        var stdCard = new ShippingRateCard
        {
            Code = "RATE-STD-2026",
            Name = "Bảng giá Tiêu chuẩn 2026",
            Description = "Bảng giá vận chuyển nội địa mặc định toàn hệ thống",
            BaseCurrencyId = vnd.Id,
            EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            IsDefault = true,
            IsActive = true
        };
        stdCard.Rules.Add(new ShippingRateRule { ServiceType = "StandardGround", FromWeightKg = 0m, ToWeightKg = 2m, BaseRate = 25_000m, AdditionalPerKgRate = 5_000m, SurchargePercent = 0m, MinimumFee = 20_000m, EstimatedDays = 3 });
        stdCard.Rules.Add(new ShippingRateRule { ServiceType = "StandardGround", FromWeightKg = 2.01m, ToWeightKg = 50m, BaseRate = 35_000m, AdditionalPerKgRate = 8_000m, SurchargePercent = 0m, MinimumFee = 35_000m, EstimatedDays = 4 });
        stdCard.Rules.Add(new ShippingRateRule { ServiceType = "NextDayExpress", FromWeightKg = 0m, ToWeightKg = 30m, BaseRate = 45_000m, AdditionalPerKgRate = 12_000m, SurchargePercent = 5m, MinimumFee = 45_000m, EstimatedDays = 1 });
        stdCard.Rules.Add(new ShippingRateRule { ServiceType = "SameDay", FromWeightKg = 0m, ToWeightKg = 15m, BaseRate = 65_000m, AdditionalPerKgRate = 15_000m, SurchargePercent = 10m, MinimumFee = 65_000m, EstimatedDays = 0 });
        stdCard.Rules.Add(new ShippingRateRule { ServiceType = "Economical", FromWeightKg = 0m, ToWeightKg = 100m, BaseRate = 18_000m, AdditionalPerKgRate = 3_000m, SurchargePercent = 0m, MinimumFee = 15_000m, EstimatedDays = 7 });

        var vipCard = new ShippingRateCard
        {
            Code = "RATE-CORP-2026",
            Name = "Bảng giá Doanh nghiệp 2026",
            Description = "Bảng giá ưu đãi cho khách hàng ký hợp đồng sản lượng lớn",
            BaseCurrencyId = vnd.Id,
            EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            IsDefault = false,
            IsActive = true
        };
        vipCard.Rules.Add(new ShippingRateRule { ServiceType = "StandardGround", FromWeightKg = 0m, ToWeightKg = 50m, BaseRate = 20_000m, AdditionalPerKgRate = 4_500m, SurchargePercent = 0m, MinimumFee = 18_000m, EstimatedDays = 3 });
        vipCard.Rules.Add(new ShippingRateRule { ServiceType = "NextDayExpress", FromWeightKg = 0m, ToWeightKg = 30m, BaseRate = 38_000m, AdditionalPerKgRate = 10_000m, SurchargePercent = 3m, MinimumFee = 38_000m, EstimatedDays = 1 });

        _db.ShippingRateCards.AddRange(stdCard, vipCard);
        await _db.SaveChangesAsync(ct);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 14. Notification Templates & Report Definitions
    // ─────────────────────────────────────────────────────────────────────────
    private async Task SeedNotificationTemplatesAndReportsAsync(CancellationToken ct)
    {
        if (!await _db.NotificationTemplates.AnyAsync(ct))
        {
            _db.NotificationTemplates.AddRange(
                new NotificationTemplate { Code = "ORDER_CONFIRMED",    Name = "Xác nhận đơn hàng",    Channel = "InApp", SubjectTemplate = "Đơn hàng {{OrderNumber}} đã được xác nhận",       BodyTemplate = "Đơn hàng {{OrderNumber}} của {{CustomerName}} đã được xác nhận.",          IsActive = true },
                new NotificationTemplate { Code = "SHIPMENT_DISPATCHED",Name = "Vận đơn xuất kho",     Channel = "InApp", SubjectTemplate = "Vận đơn {{TrackingNumber}} đã xuất kho",            BodyTemplate = "Vận đơn {{TrackingNumber}} đang được giao tới người nhận.",                IsActive = true },
                new NotificationTemplate { Code = "SHIPMENT_DELIVERED", Name = "Giao hàng thành công", Channel = "InApp", SubjectTemplate = "Vận đơn {{TrackingNumber}} đã giao thành công",    BodyTemplate = "Vận đơn {{TrackingNumber}} đã được giao thành công tới {{RecipientName}}.",IsActive = true },
                new NotificationTemplate { Code = "INVOICE_ISSUED",     Name = "Phát hành hóa đơn",    Channel = "InApp", SubjectTemplate = "Hóa đơn {{InvoiceNumber}} đã phát hành",            BodyTemplate = "Hóa đơn {{InvoiceNumber}} tổng tiền {{TotalAmount}} đã phát hành.",         IsActive = true },
                new NotificationTemplate { Code = "COD_COLLECTED",      Name = "Thu hộ COD",           Channel = "InApp", SubjectTemplate = "Đã thu COD vận đơn {{TrackingNumber}}",            BodyTemplate = "Tài xế {{DriverName}} đã thu {{Amount}} COD từ vận đơn {{TrackingNumber}}.",IsActive = true },
                new NotificationTemplate { Code = "LOW_STOCK_ALERT",    Name = "Cảnh báo Tồn kho thấp",Channel = "InApp", SubjectTemplate = "Tồn kho thấp: {{Sku}}",                             BodyTemplate = "Sản phẩm {{Sku}} tại {{WarehouseCode}} đã xuống dưới định mức đặt lại.", IsActive = true }
            );
            await _db.SaveChangesAsync(ct);
        }

        if (!await _db.ReportDefinitions.AnyAsync(ct))
        {
            var hcmBranch = await _db.Branches.FirstAsync(b => b.Code == "HCM", ct);
            var defs = new List<ReportDefinition>
            {
                new() { Code = "RPT-DAILY-OPS", Name = "Báo cáo Vận hành Hàng ngày", Category = "Operations", Description = "Tổng hợp đơn hàng, vận đơn & tỷ lệ giao thành công trong ngày", QueryDefinition = "OperationsDailySummary", IsActive = true },
                new() { Code = "RPT-COD-RECON", Name = "Báo cáo Đối soát COD", Category = "Finance", Description = "Đối soát tiền thu hộ tài xế, thủ quỹ và hoàn trả khách hàng", QueryDefinition = "CodReconciliationSummary", IsActive = true },
                new() { Code = "RPT-INV-AGING", Name = "Báo cáo Tuổi nợ Hóa đơn", Category = "Finance", Description = "Theo dõi công nợ phải thu theo kỳ hạn thanh toán", QueryDefinition = "ReceivablesAgingSummary", IsActive = true },
                new() { Code = "RPT-WH-STOCK",  Name = "Báo cáo Tồn kho & Cảnh báo", Category = "Warehouse", Description = "Tổng hợp tồn kho thực tế và danh mục dưới mức tối thiểu", QueryDefinition = "InventoryStockAndLowStock", IsActive = true },
                new() { Code = "RPT-CARRIER-SLA", Name = "Báo cáo SLA Hãng vận chuyển", Category = "Fleet", Description = "Đánh giá tỷ lệ giao đúng hạn và xử lý ngoại lệ theo đối tác", QueryDefinition = "CarrierSlaPerformance", IsActive = true }
            };
            _db.ReportDefinitions.AddRange(defs);
            await _db.SaveChangesAsync(ct);

            _db.ScheduledReports.AddRange(
                new ScheduledReport { ReportDefinitionId = defs[0].Id, BranchId = hcmBranch.Id, ScheduleCron = "0 18 * * *", TimeZoneId = "SE Asia Standard Time", OutputFormat = "Pdf", RecipientEmails = "ops.manager@lms-logistics.vn;branch.manager@lms-logistics.vn", LastRunAt = DaysAgo(1), NextRunAt = DateTime.UtcNow.AddDays(1), IsActive = true },
                new ScheduledReport { ReportDefinitionId = defs[1].Id, BranchId = hcmBranch.Id, ScheduleCron = "0 19 * * 1-5", TimeZoneId = "SE Asia Standard Time", OutputFormat = "Excel", RecipientEmails = "finance.staff@lms-logistics.vn", LastRunAt = DaysAgo(1), NextRunAt = DateTime.UtcNow.AddDays(1), IsActive = true },
                new ScheduledReport { ReportDefinitionId = defs[3].Id, BranchId = null, ScheduleCron = "0 8 * * 1", TimeZoneId = "SE Asia Standard Time", OutputFormat = "Csv", RecipientEmails = "wh.staff@lms-logistics.vn", LastRunAt = DaysAgo(4), NextRunAt = DateTime.UtcNow.AddDays(3), IsActive = true }
            );
            await _db.SaveChangesAsync(ct);
        }
    }
}
