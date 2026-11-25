using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Logistics.Client.Application.Contracts.Auth;
using Logistics.Client.Application.Interfaces;

namespace Logistics.Client.WinForms.Security;

/// <summary>
/// Persists ONLY refresh session metadata encrypted via Windows DPAPI (<see cref="DataProtectionScope.CurrentUser"/>).
/// Never persists access tokens or user passwords.
/// </summary>
public sealed class DpapiSecureTokenStore : ISecureTokenStore
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("Logistics.Desktop.DPAPI.v1");
    private readonly string _sessionFilePath;
    private readonly string _usernameFilePath;

    public DpapiSecureTokenStore(string? storageDirectory = null)
    {
        var dir = storageDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LogisticsManagement");

        _sessionFilePath = Path.Combine(dir, "session.dat");
        _usernameFilePath = Path.Combine(dir, "user.pref");
    }

    public void SaveRefreshSession(StoredRefreshSession session)
    {
        var dir = Path.GetDirectoryName(_sessionFilePath);
        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(session);
        var plainBytes = Encoding.UTF8.GetBytes(json);
        var encrypted = ProtectedData.Protect(plainBytes, Entropy, DataProtectionScope.CurrentUser);
        Array.Clear(plainBytes, 0, plainBytes.Length);
        File.WriteAllBytes(_sessionFilePath, encrypted);
    }

    public StoredRefreshSession? LoadRefreshSession()
    {
        try
        {
            if (!File.Exists(_sessionFilePath)) return null;
            var encrypted = File.ReadAllBytes(_sessionFilePath);
            var plainBytes = ProtectedData.Unprotect(encrypted, Entropy, DataProtectionScope.CurrentUser);
            var json = Encoding.UTF8.GetString(plainBytes);
            Array.Clear(plainBytes, 0, plainBytes.Length);
            return JsonSerializer.Deserialize<StoredRefreshSession>(json);
        }
        catch
        {
            ClearRefreshSession();
            return null;
        }
    }

    public void ClearRefreshSession()
    {
        try
        {
            if (File.Exists(_sessionFilePath))
                File.Delete(_sessionFilePath);
        }
        catch
        {
            // Ignore cleanup failure
        }
    }

    public void SaveRememberedUsername(string? username)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                if (File.Exists(_usernameFilePath)) File.Delete(_usernameFilePath);
                return;
            }

            var dir = Path.GetDirectoryName(_usernameFilePath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(_usernameFilePath, username.Trim());
        }
        catch
        {
            // Best-effort preference persistence
        }
    }

    public string? LoadRememberedUsername()
    {
        try
        {
            return File.Exists(_usernameFilePath) ? File.ReadAllText(_usernameFilePath).Trim() : null;
        }
        catch
        {
            return null;
        }
    }
}
