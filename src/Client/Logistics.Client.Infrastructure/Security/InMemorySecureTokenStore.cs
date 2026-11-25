using Logistics.Client.Application.Contracts.Auth;
using Logistics.Client.Application.Interfaces;

namespace Logistics.Client.Infrastructure.Security;

/// <summary>
/// Cross-platform in-memory fallback token store used in unit tests and non-Windows hosts.
/// The WinForms Windows runtime replaces this with a DPAPI CurrentUser encrypted store.
/// </summary>
public sealed class InMemorySecureTokenStore : ISecureTokenStore
{
    private StoredRefreshSession? _session;
    private string? _rememberedUsername;

    public void SaveRefreshSession(StoredRefreshSession session) => _session = session;
    public StoredRefreshSession? LoadRefreshSession() => _session;
    public void ClearRefreshSession() => _session = null;
    public void SaveRememberedUsername(string? username) => _rememberedUsername = username;
    public string? LoadRememberedUsername() => _rememberedUsername;
}
