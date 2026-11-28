namespace Logistics.Shared.Contracts.DevData;

public enum DevSeedMode
{
    FullSeed = 0,
    Incremental = 1,
    ResetAndReseed = 2,
    ModuleSpecific = 3
}

public enum DevSeedScale
{
    Standard = 0,
    Large = 1
}

public sealed class DevTestDataStatusDto
{
    public bool IsDevelopmentEnvironment { get; init; }
    public bool IsDevSeedEnabled { get; init; }
    public string EnvironmentName { get; init; } = string.Empty;
    public string DatabaseProvider { get; init; } = string.Empty;
    public string DatabaseServer { get; init; } = string.Empty;
    public string DatabaseName { get; init; } = string.Empty;
    public DateTime? LastSeededAtUtc { get; init; }
    public string? LastSeedMode { get; init; }
    public string? LastSeedScale { get; init; }
    public string? LastSeedSummary { get; init; }
    public long TotalRecordsAcrossModules { get; init; }
    public IReadOnlyList<DevModuleCoverageDto> Modules { get; init; } = [];
}

public sealed class DevModuleCoverageDto
{
    public string Module { get; init; } = string.Empty;
    public string TableName { get; init; } = string.Empty;
    public long CurrentCount { get; init; }
    public long StandardTargetCount { get; init; }
    public long LargeTargetCount { get; init; }
    public string Status { get; init; } = string.Empty;
}

public sealed class ExecuteDevSeedRequest
{
    public DevSeedMode Mode { get; init; } = DevSeedMode.FullSeed;
    public DevSeedScale Scale { get; init; } = DevSeedScale.Standard;
    public int RandomSeed { get; init; } = 42;
    public bool ConfirmReset { get; init; }
    public IReadOnlyList<string>? Modules { get; init; }
}

public sealed class ExecuteDevSeedResultDto
{
    public bool Succeeded { get; init; }
    public DevSeedMode Mode { get; init; }
    public DevSeedScale Scale { get; init; }
    public int RandomSeed { get; init; }
    public long DurationMs { get; init; }
    public DateTime ExecutedAtUtc { get; init; } = DateTime.UtcNow;
    public string Summary { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, int> RecordsAddedByEntity { get; init; } = new Dictionary<string, int>();
    public IReadOnlyList<DevModuleCoverageDto> UpdatedCoverage { get; init; } = [];
    public DevTestDataIntegrityReportDto? IntegrityReport { get; init; }
}

public sealed class DevTestDataIntegrityReportDto
{
    public bool AllPassed { get; init; }
    public DateTime CheckedAtUtc { get; init; } = DateTime.UtcNow;
    public IReadOnlyList<DevIntegrityCheckItemDto> Checks { get; init; } = [];
}

public sealed class DevIntegrityCheckItemDto
{
    public string CheckName { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public bool Passed { get; init; }
    public int ViolationCount { get; init; }
    public string Details { get; init; } = string.Empty;
}
