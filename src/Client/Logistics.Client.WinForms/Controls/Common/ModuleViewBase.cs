using Logistics.Client.WinForms.Themes;

namespace Logistics.Client.WinForms.Controls.Common;

public interface IModuleView
{
    Task LoadInitialAsync();
    Task OnBranchContextChangedAsync();
    Task RefreshViewAsync();
}

public class ModuleViewBase : UserControl, IModuleView
{
    private bool _initialized;

    public ModuleViewBase()
    {
        Dock = DockStyle.Fill;
        BackColor = AppTheme.SurfaceBg;
    }

    public virtual async Task LoadInitialAsync()
    {
        if (_initialized) return;
        _initialized = true;
        await RefreshDataAsync();
    }

    public virtual Task OnBranchContextChangedAsync() => RefreshDataAsync();

    public virtual Task RefreshViewAsync() => RefreshDataAsync();

    protected virtual Task RefreshDataAsync() => Task.CompletedTask;
}
