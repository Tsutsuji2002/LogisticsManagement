using Logistics.Client.Application.Contracts.Auth;

namespace Logistics.Client.Application.Interfaces;

public interface ISecureTokenStore
{
    void SaveRefreshSession(StoredRefreshSession session);
    StoredRefreshSession? LoadRefreshSession();
    void ClearRefreshSession();

    void SaveRememberedUsername(string? username);
    string? LoadRememberedUsername();
}

public interface IAuthorizationService
{
    IReadOnlySet<string> Permissions { get; }
    IReadOnlyList<string> Roles { get; }

    bool IsInRole(string roleCode);
    bool HasPermission(string permissionCode);
    bool HasAnyPermission(params string[] permissionCodes);
}

public interface IBranchContextService
{
    IReadOnlyList<AuthorizedBranchDto> AccessibleBranches { get; }
    long? ActiveBranchId { get; }
    AuthorizedBranchDto? ActiveBranch { get; }
    bool IsCompanyWideScope { get; }
    bool HasSingleBranchOnly { get; }

    void InitializeFromProfile(UserProfileDto profile);
    void Clear();
    bool TrySetActiveBranch(long? branchId);

    event EventHandler? ActiveBranchChanged;
}

public sealed record ClientEnvironmentProfile(string Name, string BaseUrl, int TimeoutSeconds);

public sealed record ClientAppSettings
{
    public string Environment { get; init; } = "Development";
    public string ApiBaseUrl { get; init; } = "http://localhost:5289";
    public int TimeoutSeconds { get; init; } = 30;
    public string Culture { get; init; } = "vi-VN";
    public Dictionary<string, string> EnvironmentBaseUrls { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public interface IClientSettingsService
{
    ClientAppSettings Settings { get; }
    IReadOnlyList<string> AvailableEnvironments { get; }

    void UpdateSettings(string environment, string apiBaseUrl, int timeoutSeconds, string culture);
    bool ValidateSettings(string environment, string apiBaseUrl, int timeoutSeconds, out string errorMessage);

    event EventHandler? SettingsChanged;
}
