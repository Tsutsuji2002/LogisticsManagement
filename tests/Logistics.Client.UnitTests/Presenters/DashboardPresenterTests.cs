using Logistics.Client.Application.Interfaces;
using Logistics.Client.Application.Presenters;
using Logistics.Client.Application.Views;
using Moq;
using Xunit;

namespace Logistics.Client.UnitTests.Presenters;

public class DashboardPresenterTests
{
    private readonly Mock<IDashboardView> _viewMock;
    private readonly Mock<IApiClient> _apiClientMock;
    private readonly DashboardPresenter _presenter;

    public DashboardPresenterTests()
    {
        _viewMock = new Mock<IDashboardView>();
        _apiClientMock = new Mock<IApiClient>();
        _presenter = new DashboardPresenter(_viewMock.Object, _apiClientMock.Object);
    }

    [Fact]
    public async Task InitializeAsync_SetsWelcomeMessageOnView()
    {
        await _presenter.InitializeAsync();

        _viewMock.Verify(v => v.SetWelcomeMessage(It.Is<string>(s => s.Contains("Welcome"))), Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_SetsMetricsOnView()
    {
        await _presenter.InitializeAsync();

        _viewMock.Verify(v => v.SetDashboardMetrics(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_SetsOperationalStatusOnView()
    {
        await _presenter.InitializeAsync();

        _viewMock.Verify(v => v.SetStatusMessage(It.Is<string>(s => s.Contains("operational") || s.Contains("Connecting"))), Times.AtLeastOnce);
    }
}
