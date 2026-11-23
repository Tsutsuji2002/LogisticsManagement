using Logistics.Client.Application.Interfaces;

namespace Logistics.Client.WinForms.Services;

/// <summary>
/// WinForms implementation of <see cref="INavigationService"/> for managing active view transitions.
/// </summary>
public class NavigationService : INavigationService
{
    public event EventHandler<string>? NavigationRequested;

    public void NavigateTo(string moduleName)
    {
        NavigationRequested?.Invoke(this, moduleName);
    }
}
