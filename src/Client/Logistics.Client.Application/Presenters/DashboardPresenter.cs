using Logistics.Client.Application.Interfaces;
using Logistics.Client.Application.Views;

namespace Logistics.Client.Application.Presenters;

/// <summary>
/// Presenter for the Dashboard view in accordance with the Model-View-Presenter (MVP) pattern.
/// </summary>
public class DashboardPresenter
{
    private readonly IDashboardView _view;
    private readonly IApiClient _apiClient;

    public DashboardPresenter(IDashboardView view, IApiClient apiClient)
    {
        _view = view ?? throw new ArgumentNullException(nameof(view));
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    /// <summary>
    /// Initializes dashboard data and updates view state.
    /// </summary>
    public async Task InitializeAsync()
    {
        _view.SetWelcomeMessage("Welcome to Logistics Management System");
        _view.SetStatusMessage("Connecting to system...");

        try
        {
            // Placeholder: When backend endpoints are integrated, load metrics here.
            _view.SetDashboardMetrics(totalOrders: 0, activeShipments: 0, totalCustomers: 0, activeWarehouses: 0);
            _view.SetStatusMessage("System operational.");
        }
        catch (Exception ex)
        {
            _view.SetStatusMessage($"Initialization notice: {ex.Message}");
        }

        await Task.CompletedTask;
    }
}
