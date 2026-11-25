using System.Globalization;
using System.Text;
using Logistics.Client.Application.Interfaces;
using Logistics.Shared.Contracts;

namespace Logistics.Client.Infrastructure.Api;

internal static class ApiClientHelpers
{
    public static T UnwrapRequired<T>(ApiResponse<T>? response, string operation)
    {
        if (response is { Success: true, Data: not null })
            return response.Data;

        throw new Exceptions.ApiException(response?.Message ?? $"Yêu cầu '{operation}' không trả về dữ liệu hợp lệ.");
    }

    public static string BuildUrl(string basePath, params (string Key, object? Value)[] parameters)
    {
        var sb = new StringBuilder(basePath);
        var hasQuery = basePath.Contains('?');

        foreach (var (key, value) in parameters)
        {
            if (value == null) continue;
            string? str = value switch
            {
                string s => string.IsNullOrWhiteSpace(s) ? null : s.Trim(),
                DateTime dt => dt.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture),
                DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                bool b => b ? "true" : "false",
                decimal dec => dec.ToString(CultureInfo.InvariantCulture),
                IFormattable fmt => fmt.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString()
            };

            if (string.IsNullOrWhiteSpace(str)) continue;
            sb.Append(hasQuery ? '&' : '?');
            sb.Append(Uri.EscapeDataString(key));
            sb.Append('=');
            sb.Append(Uri.EscapeDataString(str));
            hasQuery = true;
        }

        return sb.ToString();
    }
}
