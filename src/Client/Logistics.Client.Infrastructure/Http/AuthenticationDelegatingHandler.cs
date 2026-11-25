using System.Net;
using System.Net.Http.Headers;
using Logistics.Client.Application.Interfaces;

namespace Logistics.Client.Infrastructure.Http;

/// <summary>
/// Attaches JWT Bearer tokens and current branch context header, performing single-flight token refresh on expiry or 401.
/// </summary>
public sealed class AuthenticationDelegatingHandler : DelegatingHandler
{
    private readonly IAuthenticationService _authService;
    private readonly IBranchContextService _branchContext;

    public AuthenticationDelegatingHandler(
        IAuthenticationService authService,
        IBranchContextService branchContext)
    {
        _authService = authService;
        _branchContext = branchContext;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        var isAnonymousAuthEndpoint =
            path.EndsWith("/api/v1/auth/login", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith("/api/v1/auth/refresh", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith("/api/health", StringComparison.OrdinalIgnoreCase);

        if (!isAnonymousAuthEndpoint)
        {
            var token = await _authService.GetValidAccessTokenAsync(forceRefresh: false, cancellationToken);
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        if (_branchContext.ActiveBranchId.HasValue)
        {
            request.Headers.Remove("X-Branch-Id");
            request.Headers.TryAddWithoutValidation("X-Branch-Id", _branchContext.ActiveBranchId.Value.ToString());
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (!isAnonymousAuthEndpoint && response.StatusCode == HttpStatusCode.Unauthorized && _authService.IsAuthenticated)
        {
            // Retry at most once after forced refresh
            var refreshedToken = await _authService.GetValidAccessTokenAsync(forceRefresh: true, cancellationToken);
            if (!string.IsNullOrWhiteSpace(refreshedToken))
            {
                response.Dispose();
                using var retryRequest = await CloneRequestAsync(request, cancellationToken);
                retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshedToken);
                return await base.SendAsync(retryRequest, cancellationToken);
            }

            await _authService.HandleSessionInvalidatedAsync("Phiên đăng nhập đã hết hạn hoặc bị thu hồi.");
        }

        return response;
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage original, CancellationToken ct)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);
        foreach (var header in original.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (original.Content != null)
        {
            var bytes = await original.Content.ReadAsByteArrayAsync(ct);
            clone.Content = new ByteArrayContent(bytes);
            foreach (var header in original.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return clone;
    }
}
