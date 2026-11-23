namespace Logistics.Client.Application.Interfaces;

/// <summary>
/// Service for navigating between different module views in the desktop client.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Event triggered when a navigation request is issued.
    /// </summary>
    event EventHandler<string>? NavigationRequested;

    /// <summary>
    /// Requests navigation to the specified module screen.
    /// </summary>
    void NavigateTo(string moduleName);
}
