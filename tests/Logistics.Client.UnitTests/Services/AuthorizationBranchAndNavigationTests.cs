using FluentAssertions;
using Logistics.Client.Application.Contracts.Auth;
using Logistics.Client.Application.Interfaces;
using Logistics.Client.Application.Navigation;
using Logistics.Client.Application.Permissions;
using Logistics.Client.Infrastructure.Configuration;
using Logistics.Client.Infrastructure.Http;
using Logistics.Client.Infrastructure.Services;
using Moq;
using Xunit;

namespace Logistics.Client.UnitTests.Services;

public sealed class AuthorizationBranchAndNavigationTests
{
    private static UserProfileDto BuildProfile(string[] roles, string[] permissions, AuthorizedBranchDto[] branches, long? defaultBranchId) => new(
        1,
        Guid.NewGuid(),
        "tester",
        "tester@logistics.local",
        "Tester",
        null,
        null,
        null,
        defaultBranchId,
        branches.FirstOrDefault(b => b.BranchId == defaultBranchId)?.Name,
        branches,
        roles,
        permissions,
        true);

    [Fact]
    public void ClientAuthorizationService_ShouldAllowAllForSuperAdminAndFilterRegularUsers()
    {
        var authServiceMock = new Mock<IAuthenticationService>();
        authServiceMock.SetupGet(a => a.IsAuthenticated).Returns(true);

        // 1. Regular dispatcher user
        authServiceMock.SetupGet(a => a.CurrentUser).Returns(BuildProfile(
            ["Dispatcher"],
            [ClientPermissions.Shipping.ShipmentsRead, ClientPermissions.Sales.OrdersRead],
            [new AuthorizedBranchDto(1, Guid.NewGuid(), "HCM", "HCM Hub", true)],
            1));

        var auth = new ClientAuthorizationService(authServiceMock.Object);
        auth.HasPermission(ClientPermissions.Shipping.ShipmentsRead).Should().BeTrue();
        auth.HasPermission(ClientPermissions.Finance.InvoicesIssue).Should().BeFalse();

        var visibleGroups = NavigationCatalog.GetVisibleGroups(auth);
        visibleGroups.Select(g => g.GroupKey).Should().Contain(["Sales", "Shipping", "Settings"]);
        visibleGroups.Select(g => g.GroupKey).Should().NotContain(["Finance", "COD", "Organization"]);

        // 2. SuperAdmin sees everything
        authServiceMock.SetupGet(a => a.CurrentUser).Returns(BuildProfile(
            [ClientPermissions.Roles.SuperAdmin],
            [],
            [],
            null));

        auth.HasPermission(ClientPermissions.Finance.InvoicesIssue).Should().BeTrue();
        var superGroups = NavigationCatalog.GetVisibleGroups(auth);
        superGroups.Select(g => g.GroupKey).Should().BeEquivalentTo(NavigationCatalog.Groups.Select(g => g.GroupKey));
    }

    [Fact]
    public void BranchContextService_ShouldEnforceBranchAccessScope()
    {
        var branches = new[]
        {
            new AuthorizedBranchDto(1, Guid.NewGuid(), "HCM", "HCM Hub", true),
            new AuthorizedBranchDto(2, Guid.NewGuid(), "HN", "Hanoi Hub", false)
        };

        var svc = new BranchContextService();

        // Multi-branch non-super-admin user
        svc.InitializeFromProfile(BuildProfile(["BranchManager"], [], [branches[0]], 1));
        svc.HasSingleBranchOnly.Should().BeTrue();
        svc.ActiveBranchId.Should().Be(1);
        svc.TrySetActiveBranch(null).Should().BeFalse("single-branch user cannot clear branch scope");
        svc.TrySetActiveBranch(999).Should().BeFalse("unauthorized branch must be rejected");

        // Multi-branch user can switch between assigned branches and view All accessible
        svc.InitializeFromProfile(BuildProfile(["RegionalManager"], [], branches, 1));
        svc.HasSingleBranchOnly.Should().BeFalse();
        svc.TrySetActiveBranch(2).Should().BeTrue();
        svc.ActiveBranchId.Should().Be(2);
        svc.TrySetActiveBranch(999).Should().BeFalse();
        svc.TrySetActiveBranch(null).Should().BeTrue();
        svc.ActiveBranchId.Should().BeNull();

        // SuperAdmin gets company-wide scope when null
        svc.InitializeFromProfile(BuildProfile([ClientPermissions.Roles.SuperAdmin], [], branches, null));
        svc.IsCompanyWideScope.Should().BeTrue();
        svc.TrySetActiveBranch(2).Should().BeTrue();
        svc.IsCompanyWideScope.Should().BeFalse();
    }

    [Fact]
    public void ClientSettingsService_ShouldRejectLocalhostInProductionEnvironment()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"logistics-test-settings-{Guid.NewGuid():N}.json");
        try
        {
            var svc = new ClientSettingsService(new ApiClientOptions { BaseUrl = "http://localhost:5289" }, tempFile);

            svc.ValidateSettings("Production", "http://localhost:5289", 30, out var err).Should().BeFalse();
            err.Should().Contain("Production");

            svc.ValidateSettings("Production", "https://api.logistics.example.com", 30, out var okErr).Should().BeTrue();
            okErr.Should().BeEmpty();

            svc.UpdateSettings("Production", "https://api.logistics.example.com", 45, "en-US");
            svc.Settings.Environment.Should().Be("Production");
            svc.Settings.ApiBaseUrl.Should().Be("https://api.logistics.example.com");
            svc.Settings.TimeoutSeconds.Should().Be(45);
            svc.Settings.Culture.Should().Be("en-US");
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
