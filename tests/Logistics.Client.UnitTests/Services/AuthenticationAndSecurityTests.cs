using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Logistics.Client.Application.Contracts.Auth;
using Logistics.Client.Application.Interfaces;
using Logistics.Client.Infrastructure.Http;
using Logistics.Client.Infrastructure.Security;
using Logistics.Client.Infrastructure.Services;
using Logistics.Shared.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Logistics.Client.UnitTests.Services;

public sealed class AuthenticationAndSecurityTests
{
    private sealed class ConcurrentCountingHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => responder(request);
    }

    private static UserProfileDto CreateProfile(params string[] roles) => new(
        10,
        Guid.NewGuid(),
        "ops.user",
        "ops@logistics.local",
        "Ops User",
        100,
        "EMP-100",
        "Dispatcher",
        1,
        "HCM Hub",
        [new AuthorizedBranchDto(1, Guid.NewGuid(), "HCM", "HCM Hub", true)],
        roles.Length == 0 ? ["Dispatcher"] : roles,
        ["shipping.shipments.read", "sales.orders.read"],
        true);

    private static HttpResponseMessage JsonOk<T>(T data)
    {
        var body = JsonSerializer.Serialize(new ApiResponse<T> { Success = true, Message = "OK", Data = data });
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
    }

    [Fact]
    public async Task LoginDetailedAsync_Success_ShouldPopulateAuthAndNeverPersistPassword()
    {
        var tokenStore = new InMemorySecureTokenStore();
        var branchService = new BranchContextService();
        var handler = new ConcurrentCountingHandler(_ =>
        {
            var resp = new LoginResponse(
                "access-jwt-1",
                DateTime.UtcNow.AddMinutes(30),
                "refresh-opaque-1",
                DateTime.UtcNow.AddDays(7),
                CreateProfile());
            return Task.FromResult(JsonOk(resp));
        });

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient("LogisticsAuth"))
            .Returns(() => new HttpClient(handler, disposeHandler: false) { BaseAddress = new Uri("http://localhost:5289/") });

        using var auth = new ClientAuthenticationService(
            factoryMock.Object,
            new ApiClientOptions { BaseUrl = "http://localhost:5289" },
            tokenStore,
            branchService,
            NullLogger<ClientAuthenticationService>.Instance);

        var (ok, err) = await auth.LoginDetailedAsync("ops.user", "MySecretPassword123!", rememberUsername: true);

        ok.Should().BeTrue();
        err.Should().BeNull();
        auth.IsAuthenticated.Should().BeTrue();
        auth.AccessToken.Should().Be("access-jwt-1");
        auth.CurrentUserName.Should().Be("ops.user");
        branchService.ActiveBranchId.Should().Be(1);

        var stored = tokenStore.LoadRefreshSession();
        stored.Should().NotBeNull();
        stored!.RefreshToken.Should().Be("refresh-opaque-1");
        stored.Username.Should().Be("ops.user");
        var serialized = JsonSerializer.Serialize(stored);
        serialized.Should().NotContain("MySecretPassword123!");
        tokenStore.LoadRememberedUsername().Should().Be("ops.user");
    }

    [Fact]
    public async Task ConcurrentForcedRefresh_ShouldExecuteSingleFlightViaSemaphoreSlim()
    {
        var tokenStore = new InMemorySecureTokenStore();
        var branchService = new BranchContextService();
        int refreshCalls = 0;

        var handler = new ConcurrentCountingHandler(async req =>
        {
            var path = req.RequestUri!.AbsolutePath;
            if (path.EndsWith("/login", StringComparison.OrdinalIgnoreCase))
            {
                return JsonOk(new LoginResponse(
                    "initial-jwt",
                    DateTime.UtcNow.AddMinutes(10),
                    "refresh-1",
                    DateTime.UtcNow.AddDays(7),
                    CreateProfile()));
            }

            if (path.EndsWith("/refresh", StringComparison.OrdinalIgnoreCase))
            {
                Interlocked.Increment(ref refreshCalls);
                await Task.Delay(50);
                return JsonOk(new LoginResponse(
                    "rotated-jwt-2",
                    DateTime.UtcNow.AddMinutes(30),
                    "refresh-2",
                    DateTime.UtcNow.AddDays(7),
                    CreateProfile()));
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient("LogisticsAuth"))
            .Returns(() => new HttpClient(handler, disposeHandler: false) { BaseAddress = new Uri("http://localhost:5289/") });

        using var auth = new ClientAuthenticationService(
            factoryMock.Object,
            new ApiClientOptions { BaseUrl = "http://localhost:5289" },
            tokenStore,
            branchService,
            NullLogger<ClientAuthenticationService>.Instance);

        await auth.LoginAsync("ops.user", "secret");

        // Trigger 8 concurrent force refreshes simultaneously; SemaphoreSlim + token-rotation check must coalesce them.
        var tasks = Enumerable.Range(0, 8)
            .Select(_ => auth.GetValidAccessTokenAsync(forceRefresh: true))
            .ToArray();
        var tokens = await Task.WhenAll(tasks);

        refreshCalls.Should().Be(1);
        tokens.Should().OnlyContain(t => t == "rotated-jwt-2");
        tokenStore.LoadRefreshSession()!.RefreshToken.Should().Be("refresh-2");
    }

    [Fact]
    public async Task FailedRefresh_ShouldClearSessionAndRaiseSessionExpired()
    {
        var tokenStore = new InMemorySecureTokenStore();
        tokenStore.SaveRefreshSession(new StoredRefreshSession("expired-refresh", DateTime.UtcNow.AddDays(1), "ops.user"));
        var branchService = new BranchContextService();

        var handler = new ConcurrentCountingHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)));
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient("LogisticsAuth"))
            .Returns(() => new HttpClient(handler, disposeHandler: false) { BaseAddress = new Uri("http://localhost:5289/") });

        using var auth = new ClientAuthenticationService(
            factoryMock.Object,
            new ApiClientOptions { BaseUrl = "http://localhost:5289" },
            tokenStore,
            branchService,
            NullLogger<ClientAuthenticationService>.Instance);

        var expiredRaised = false;
        auth.SessionExpired += (_, _) => expiredRaised = true;

        var restored = await auth.TryRestoreSessionAsync();

        restored.Should().BeFalse();
        auth.IsAuthenticated.Should().BeFalse();
        tokenStore.LoadRefreshSession().Should().BeNull();
        expiredRaised.Should().BeFalse(); // TryRestore failing at startup shouldn't fire mid-session dialog

        // Now verify mid-session invalidation clears state & fires SessionExpired
        await auth.HandleSessionInvalidatedAsync("Revoked");
        expiredRaised.Should().BeTrue();
    }

    [Fact]
    public async Task LogoutAllAsync_ShouldInvokeLogoutAllAndClearLocalState()
    {
        var tokenStore = new InMemorySecureTokenStore();
        var branchService = new BranchContextService();
        bool calledLogoutAll = false;

        var handler = new ConcurrentCountingHandler(req =>
        {
            var path = req.RequestUri!.AbsolutePath;
            if (path.EndsWith("/login", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(JsonOk(new LoginResponse(
                    "jwt-1",
                    DateTime.UtcNow.AddMinutes(30),
                    "refresh-1",
                    DateTime.UtcNow.AddDays(7),
                    CreateProfile())));
            }
            if (path.EndsWith("/logout-all", StringComparison.OrdinalIgnoreCase))
            {
                calledLogoutAll = true;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient("LogisticsAuth"))
            .Returns(() => new HttpClient(handler, disposeHandler: false) { BaseAddress = new Uri("http://localhost:5289/") });

        using var auth = new ClientAuthenticationService(
            factoryMock.Object,
            new ApiClientOptions { BaseUrl = "http://localhost:5289" },
            tokenStore,
            branchService,
            NullLogger<ClientAuthenticationService>.Instance);

        await auth.LoginAsync("ops.user", "pw");
        await auth.LogoutAllAsync();

        calledLogoutAll.Should().BeTrue();
        auth.IsAuthenticated.Should().BeFalse();
        tokenStore.LoadRefreshSession().Should().BeNull();
        branchService.AccessibleBranches.Should().BeEmpty();
    }
}
