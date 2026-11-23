namespace Logistics.Client.Application.Views;

/// <summary>
/// View contract for the Dashboard screen following the Model-View-Presenter (MVP) pattern.
/// </summary>
public interface IDashboardView
{
    /// <summary>
    /// Displays a welcome banner message on the view.
    /// </summary>
    void SetWelcomeMessage(string message);

    /// <summary>
    /// Updates the status text displayed on the view.
    /// </summary>
    void SetStatusMessage(string status);

    /// <summary>
    /// Displays key operational metrics on the dashboard summary cards.
    /// </summary>
    void SetDashboardMetrics(int totalOrders, int activeShipments, int totalCustomers, int activeWarehouses);
}
