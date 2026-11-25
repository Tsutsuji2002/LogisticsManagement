using System.Text.Json;
using Logistics.Client.Application.Interfaces;
using Logistics.Client.Application.Localization;
using Logistics.Client.Infrastructure.Http;
using Microsoft.Extensions.Configuration;

namespace Logistics.Client.Infrastructure.Configuration;

/// <summary>
/// Manages externalized environment-based API configuration (Development / Production) and user preferences.
/// Never falls back silently to localhost in Production.
/// </summary>
public sealed class ClientSettingsService : IClientSettingsService
{
    private readonly string _userSettingsFilePath;
    private readonly ApiClientOptions _liveOptions;
    private ClientAppSettings _settings;

    public ClientSettingsService(ApiClientOptions liveOptions, string? userSettingsFilePath = null)
    {
        _liveOptions = liveOptions ?? throw new ArgumentNullException(nameof(liveOptions));
        _userSettingsFilePath = userSettingsFilePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LogisticsManagement",
            "desktop-settings.json");

        _settings = LoadInitialSettings();
        SyncLiveOptions(_settings);
        AppStrings.SetCulture(_settings.Culture);
    }

    public ClientAppSettings Settings => _settings;

    public IReadOnlyList<string> AvailableEnvironments => ["Development", "Production"];

    public event EventHandler? SettingsChanged;

    public bool ValidateSettings(string environment, string apiBaseUrl, int timeoutSeconds, out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(environment) ||
            (!string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase) &&
             !string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase)))
        {
            errorMessage = "Môi trường phải là Development hoặc Production.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(apiBaseUrl) || !Uri.TryCreate(apiBaseUrl.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            errorMessage = "API Base URL không hợp lệ (cần http:// hoặc https://).";
            return false;
        }

        if (string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase) &&
            (uri.IsLoopback || string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)))
        {
            errorMessage = "Môi trường Production không cho phép trỏ về localhost / loopback.";
            return false;
        }

        if (timeoutSeconds is < 5 or > 300)
        {
            errorMessage = "Timeout phải nằm trong khoảng 5 đến 300 giây.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    public void UpdateSettings(string environment, string apiBaseUrl, int timeoutSeconds, string culture)
    {
        if (!ValidateSettings(environment, apiBaseUrl, timeoutSeconds, out var err))
            throw new ArgumentException(err);

        var normalizedEnv = string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase)
            ? "Production"
            : "Development";

        var urls = new Dictionary<string, string>(_settings.EnvironmentBaseUrls, StringComparer.OrdinalIgnoreCase)
        {
            [normalizedEnv] = apiBaseUrl.Trim().TrimEnd('/')
        };

        _settings = new ClientAppSettings
        {
            Environment = normalizedEnv,
            ApiBaseUrl = apiBaseUrl.Trim().TrimEnd('/'),
            TimeoutSeconds = timeoutSeconds,
            Culture = string.Equals(culture, "en-US", StringComparison.OrdinalIgnoreCase) ? "en-US" : "vi-VN",
            EnvironmentBaseUrls = urls
        };

        PersistUserSettings(_settings);
        SyncLiveOptions(_settings);
        AppStrings.SetCulture(_settings.Culture);
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SyncLiveOptions(ClientAppSettings s)
    {
        _liveOptions.BaseUrl = s.ApiBaseUrl;
        _liveOptions.TimeoutSeconds = s.TimeoutSeconds;
    }

    private ClientAppSettings LoadInitialSettings()
    {
        var baseDir = AppContext.BaseDirectory;
        var envFromProcess = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                          ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                          ?? "Development";

        var config = new ConfigurationBuilder()
            .SetBasePath(baseDir)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile($"appsettings.{envFromProcess}.json", optional: true, reloadOnChange: false)
            .Build();

        var envUrls = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        config.GetSection("ApiClient:EnvironmentUrls").Bind(envUrls);

        var envName = config["ApiClient:Environment"] ?? envFromProcess;
        var configuredUrl = config["ApiClient:BaseUrl"];
        if (string.IsNullOrWhiteSpace(configuredUrl) && envUrls.TryGetValue(envName, out var mappedUrl))
        {
            configuredUrl = mappedUrl;
        }

        // Only default to localhost when running in Development; never silently fallback to localhost in Production.
        if (string.IsNullOrWhiteSpace(configuredUrl))
        {
            configuredUrl = string.Equals(envName, "Development", StringComparison.OrdinalIgnoreCase)
                ? _liveOptions.BaseUrl
                : string.Empty;
        }

        var timeout = int.TryParse(config["ApiClient:TimeoutSeconds"], out var parsedTimeout) && parsedTimeout >= 5
            ? parsedTimeout
            : _liveOptions.TimeoutSeconds;

        var culture = config["Localization:DefaultCulture"] ?? AppStrings.DefaultCulture;

        // Merge local user settings override if present
        try
        {
            if (File.Exists(_userSettingsFilePath))
            {
                var json = File.ReadAllText(_userSettingsFilePath);
                var saved = JsonSerializer.Deserialize<ClientAppSettings>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (saved != null && !string.IsNullOrWhiteSpace(saved.ApiBaseUrl))
                {
                    return saved;
                }
            }
        }
        catch
        {
            // Ignore malformed user override and use baseline configuration
        }

        return new ClientAppSettings
        {
            Environment = string.Equals(envName, "Production", StringComparison.OrdinalIgnoreCase) ? "Production" : "Development",
            ApiBaseUrl = configuredUrl.TrimEnd('/'),
            TimeoutSeconds = timeout,
            Culture = culture,
            EnvironmentBaseUrls = envUrls
        };
    }

    private void PersistUserSettings(ClientAppSettings settings)
    {
        var dir = Path.GetDirectoryName(_userSettingsFilePath);
        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_userSettingsFilePath, json);
    }
}
