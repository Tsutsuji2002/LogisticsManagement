using Logistics.Client.Application.Contracts.Auth;
using Logistics.Client.Application.Interfaces;
using Logistics.Client.Application.Localization;
using Logistics.Client.Application.Navigation;
using Logistics.Client.Application.Permissions;
using Logistics.Client.WinForms.Controls.Carrier;
using Logistics.Client.WinForms.Controls.Catalog;
using Logistics.Client.WinForms.Controls.COD;
using Logistics.Client.WinForms.Controls.Common;
using Logistics.Client.WinForms.Controls.CRM;
using Logistics.Client.WinForms.Controls.Dashboard;
using Logistics.Client.WinForms.Controls.Finance;
using Logistics.Client.WinForms.Controls.Inventory;
using Logistics.Client.WinForms.Controls.Notifications;
using Logistics.Client.WinForms.Controls.Organization;
using Logistics.Client.WinForms.Controls.Reports;
using Logistics.Client.WinForms.Controls.Sales;
using Logistics.Client.WinForms.Controls.Settings;
using Logistics.Client.WinForms.Controls.Shipping;
using Logistics.Client.WinForms.Controls.Warehouse;
using Logistics.Client.WinForms.Helpers;
using Logistics.Client.WinForms.Themes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Logistics.Client.WinForms;

public partial class MainForm : Form
{
    private readonly INavigationService _navigationService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IAuthenticationService _authService;
    private readonly IAuthorizationService _authorization;
    private readonly IBranchContextService _branchContext;
    private readonly IClientSettingsService _settingsService;
    private readonly IHealthApiClient _healthClient;
    private readonly INotificationApiClient _notificationClient;
    private readonly ILogger<MainForm> _logger;

    private readonly Dictionary<string, Control> _moduleControls = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Button> _navButtons = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _expandedGroups = new(StringComparer.OrdinalIgnoreCase) { "Dashboard" };

    private Panel _sidebarPanel = null!;
    private Panel _sidebarHeader = null!;
    private Label _brandTitleLabel = null!;
    private Button _collapseBtn = null!;
    private Panel _navScrollPanel = null!;

    private Panel _headerPanel = null!;
    private Label _breadcrumbLabel = null!;
    private Label _pageTitleLabel = null!;
    private ComboBox _branchCombo = null!;
    private Button _bellBtn = null!;
    private Button _userMenuBtn = null!;
    private ComboBox _langCombo = null!;

    private Panel _contentHostPanel = null!;
    private StatusStrip _statusStrip = null!;
    private ToolStripStatusLabel _statusApiLabel = null!;
    private ToolStripStatusLabel _statusEnvLabel = null!;
    private ToolStripStatusLabel _statusUserLabel = null!;
    private ToolStripStatusLabel _statusBranchLabel = null!;

    private System.Windows.Forms.Timer _unreadTimer = null!;
    private bool _sidebarCollapsed;
    private bool _suppressBranchEvent;
    private string _activeNavKey = "Dashboard";
    private bool _isClosingForLogout;

    public bool ReloginRequested { get; private set; }

    public MainForm(
        INavigationService navigationService,
        IServiceProvider serviceProvider,
        IAuthenticationService authService,
        IAuthorizationService authorization,
        IBranchContextService branchContext,
        IClientSettingsService settingsService,
        IHealthApiClient healthClient,
        INotificationApiClient notificationClient,
        ILogger<MainForm> logger)
    {
        _navigationService = navigationService;
        _serviceProvider = serviceProvider;
        _authService = authService;
        _authorization = authorization;
        _branchContext = branchContext;
        _settingsService = settingsService;
        _healthClient = healthClient;
        _notificationClient = notificationClient;
        _logger = logger;

        InitializeComponent();
        BuildShellLayout();

        _navigationService.NavigationRequested += OnNavigationRequested;
        _branchContext.ActiveBranchChanged += OnBranchContextChanged;
        _authService.SessionExpired += OnSessionExpired;
        _settingsService.SettingsChanged += OnSettingsChanged;
        AppStrings.LanguageChanged += OnLanguageChanged;

        Load += async (_, _) => await OnFormLoadedAsync();
        FormClosed += (_, _) => UnsubscribeEvents();
    }

    private void BuildShellLayout()
    {
        Text = $"{AppStrings.T("AppTitle")} - Desktop Client";
        BackColor = AppTheme.SurfaceBg;
        Font = AppTheme.BodyFont;

        // 1. Sidebar
        _sidebarPanel = new Panel
        {
            Dock = DockStyle.Left,
            Width = 250,
            BackColor = AppTheme.NavyDark
        };

        _sidebarHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 58,
            BackColor = Color.FromArgb(10, 17, 32),
            Padding = new Padding(12, 0, 8, 0)
        };
        _brandTitleLabel = new Label
        {
            Text = "LOGISTICS ERP",
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _collapseBtn = new Button
        {
            Text = "◀",
            Width = 34,
            Height = 34,
            Dock = DockStyle.Right,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(30, 41, 59),
            Cursor = Cursors.Hand
        };
        _collapseBtn.FlatAppearance.BorderSize = 0;
        _collapseBtn.Click += (_, _) => ToggleSidebarCollapse();
        _sidebarHeader.Controls.Add(_brandTitleLabel);
        _sidebarHeader.Controls.Add(_collapseBtn);

        _navScrollPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = AppTheme.NavyDark,
            Padding = new Padding(6, 8, 6, 12)
        };

        _sidebarPanel.Controls.Add(_navScrollPanel);
        _sidebarPanel.Controls.Add(_sidebarHeader);

        // 2. Top Header
        _headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 58,
            BackColor = Color.White,
            Padding = new Padding(18, 8, 16, 8)
        };
        _headerPanel.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.BorderSubtle);
            e.Graphics.DrawLine(pen, 0, _headerPanel.Height - 1, _headerPanel.Width, _headerPanel.Height - 1);
        };

        var titleStack = new Panel { Dock = DockStyle.Left, Width = 360 };
        _breadcrumbLabel = new Label
        {
            Text = AppStrings.T("Nav.Dashboard"),
            Font = AppTheme.SmallFont,
            ForeColor = AppTheme.TextMuted,
            Dock = DockStyle.Top,
            Height = 16
        };
        _pageTitleLabel = new Label
        {
            Text = "Tổng quan điều hành",
            Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
            ForeColor = AppTheme.TextPrimary,
            Dock = DockStyle.Fill
        };
        titleStack.Controls.Add(_pageTitleLabel);
        titleStack.Controls.Add(_breadcrumbLabel);

        var rightBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 4, 0, 0)
        };

        var branchLbl = new Label { Text = AppStrings.T("BranchLabel") + ":", AutoSize = true, Font = AppTheme.BodyBoldFont, ForeColor = AppTheme.TextSecondary, Margin = new Padding(0, 7, 4, 0) };
        _branchCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 210, Font = AppTheme.BodyFont, Margin = new Padding(0, 2, 10, 0) };
        _branchCombo.SelectedIndexChanged += (_, _) => OnBranchDropdownChanged();

        _langCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 80, Font = AppTheme.BodyFont, Margin = new Padding(0, 2, 10, 0) };
        _langCombo.Items.AddRange(["vi-VN", "en-US"]);
        _langCombo.SelectedItem = AppStrings.CurrentCulture;
        _langCombo.SelectedIndexChanged += (_, _) =>
        {
            var lang = _langCombo.SelectedItem?.ToString() ?? "vi-VN";
            if (!string.Equals(lang, AppStrings.CurrentCulture, StringComparison.OrdinalIgnoreCase))
            {
                var s = _settingsService.Settings;
                _settingsService.UpdateSettings(s.Environment, s.ApiBaseUrl, s.TimeoutSeconds, lang);
            }
        };

        _bellBtn = AppTheme.CreateSecondaryButton("🔔 0", 72);
        _bellBtn.Visible = _authorization.HasPermission(ClientPermissions.Notification.NotificationsReadOwn);
        _bellBtn.Click += (_, _) => _navigationService.NavigateTo("Notifications.Center");

        _userMenuBtn = AppTheme.CreatePrimaryButton(_authService.CurrentUser?.DisplayName ?? "User", 160);
        _userMenuBtn.ContextMenuStrip = BuildUserContextMenu();
        _userMenuBtn.Click += (_, _) => _userMenuBtn.ContextMenuStrip?.Show(_userMenuBtn, new Point(0, _userMenuBtn.Height));

        rightBar.Controls.AddRange([branchLbl, _branchCombo, _langCombo, _bellBtn, _userMenuBtn]);
        _headerPanel.Controls.Add(titleStack);
        _headerPanel.Controls.Add(rightBar);

        // 3. Content Host
        _contentHostPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceBg,
            Padding = new Padding(12)
        };

        // 4. Status Strip
        _statusStrip = new StatusStrip { BackColor = Color.White };
        _statusApiLabel = new ToolStripStatusLabel("API: Đang kiểm tra...");
        _statusEnvLabel = new ToolStripStatusLabel($"Môi trường: {_settingsService.Settings.Environment}");
        _statusUserLabel = new ToolStripStatusLabel();
        _statusBranchLabel = new ToolStripStatusLabel { Spring = true, TextAlign = ContentAlignment.MiddleRight };
        _statusStrip.Items.AddRange([_statusApiLabel, new ToolStripSeparator(), _statusEnvLabel, new ToolStripSeparator(), _statusUserLabel, _statusBranchLabel]);

        var workspaceContainer = new Panel { Dock = DockStyle.Fill };
        workspaceContainer.Controls.Add(_contentHostPanel);
        workspaceContainer.Controls.Add(_headerPanel);

        Controls.Add(workspaceContainer);
        Controls.Add(_sidebarPanel);
        Controls.Add(_statusStrip);

        _unreadTimer = new System.Windows.Forms.Timer { Interval = 60_000 };
        _unreadTimer.Tick += async (_, _) => await RefreshNotificationBadgeAndHealthAsync();
    }

    private ContextMenuStrip BuildUserContextMenu()
    {
        var menu = new ContextMenuStrip { Font = AppTheme.BodyFont };
        menu.Items.Add("Hồ sơ & Bảo mật", null, (_, _) => _navigationService.NavigateTo("Settings.Workspace"));
        menu.Items.Add("Làm mới quyền (/auth/me)", null, async (_, _) =>
        {
            await UiAsyncRunner.RunSafeAsync(this, async () =>
            {
                await _authService.RefreshUserProfileAsync();
                RebuildNavigationSidebar();
                PopulateBranchCombo();
                UpdateUserStatusLabels();
            }, null, null, _logger);
        });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(AppStrings.T("Logout"), null, async (_, _) => await ExecuteLogoutAsync(false));
        menu.Items.Add(AppStrings.T("LogoutAll"), null, async (_, _) => await ExecuteLogoutAsync(true));
        return menu;
    }

    private async Task OnFormLoadedAsync()
    {
        PopulateBranchCombo();
        RebuildNavigationSidebar();
        UpdateUserStatusLabels();

        // Pick first permitted navigation key (Dashboard or first visible module)
        var visible = NavigationCatalog.GetVisibleGroups(_authorization);
        var initialKey = visible.FirstOrDefault()?.Items.FirstOrDefault()?.Key ?? "Settings.Workspace";
        await NavigateInternalAsync(initialKey);

        _unreadTimer.Start();
        await RefreshNotificationBadgeAndHealthAsync();
    }

    private void ToggleSidebarCollapse()
    {
        _sidebarCollapsed = !_sidebarCollapsed;
        _sidebarPanel.Width = _sidebarCollapsed ? 58 : 250;
        _brandTitleLabel.Visible = !_sidebarCollapsed;
        _collapseBtn.Text = _sidebarCollapsed ? "▶" : "◀";
        RebuildNavigationSidebar();
    }

    private void RebuildNavigationSidebar()
    {
        _navScrollPanel.SuspendLayout();
        _navScrollPanel.Controls.Clear();
        _navButtons.Clear();

        var groups = NavigationCatalog.GetVisibleGroups(_authorization);

        // Add in reverse because DockStyle.Top stacks bottom-to-top when added sequentially
        for (int g = groups.Count - 1; g >= 0; g--)
        {
            var group = groups[g];
            var expanded = _expandedGroups.Contains(group.GroupKey) || _sidebarCollapsed;

            if (expanded)
            {
                for (int i = group.Items.Count - 1; i >= 0; i--)
                {
                    var item = group.Items[i];
                    var itemText = _sidebarCollapsed ? group.IconCode : $"   • {AppStrings.T(item.TitleKey) switch { var t when t == item.TitleKey => item.DefaultTitle, var t => t }}";
                    var itemBtn = new Button
                    {
                        Dock = DockStyle.Top,
                        Height = 34,
                        FlatStyle = FlatStyle.Flat,
                        Text = itemText,
                        TextAlign = _sidebarCollapsed ? ContentAlignment.MiddleCenter : ContentAlignment.MiddleLeft,
                        Font = AppTheme.SmallFont,
                        ForeColor = Color.FromArgb(226, 232, 240),
                        BackColor = AppTheme.SidebarBg,
                        Padding = _sidebarCollapsed ? new Padding(0) : new Padding(12, 0, 6, 0),
                        Cursor = Cursors.Hand
                    };
                    itemBtn.FlatAppearance.BorderSize = 0;
                    itemBtn.FlatAppearance.MouseOverBackColor = AppTheme.SidebarHover;
                    itemBtn.Click += (_, _) => _navigationService.NavigateTo(item.Key);
                    _navButtons[item.Key] = itemBtn;
                    _navScrollPanel.Controls.Add(itemBtn);
                }
            }

            if (!_sidebarCollapsed)
            {
                var groupLabel = AppStrings.T(group.TitleKey);
                var groupBtn = new Button
                {
                    Dock = DockStyle.Top,
                    Height = 38,
                    FlatStyle = FlatStyle.Flat,
                    Text = $"{group.IconCode}  {groupLabel}  {(expanded ? "▾" : "▸")}",
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = AppTheme.BodyBoldFont,
                    ForeColor = Color.White,
                    BackColor = AppTheme.NavyDark,
                    Padding = new Padding(8, 0, 8, 0),
                    Cursor = Cursors.Hand
                };
                groupBtn.FlatAppearance.BorderSize = 0;
                groupBtn.FlatAppearance.MouseOverBackColor = AppTheme.SidebarHover;
                var keyCopy = group.GroupKey;
                groupBtn.Click += (_, _) =>
                {
                    if (!_expandedGroups.Add(keyCopy))
                        _expandedGroups.Remove(keyCopy);
                    RebuildNavigationSidebar();
                };
                _navScrollPanel.Controls.Add(groupBtn);
            }
        }

        HighlightActiveNavButton();
        _navScrollPanel.ResumeLayout();
    }

    private void HighlightActiveNavButton()
    {
        foreach (var (key, btn) in _navButtons)
        {
            var isActive = string.Equals(key, _activeNavKey, StringComparison.OrdinalIgnoreCase);
            btn.BackColor = isActive ? AppTheme.PrimaryBlue : AppTheme.SidebarBg;
            btn.ForeColor = isActive ? Color.White : Color.FromArgb(226, 232, 240);
        }
    }

    private void PopulateBranchCombo()
    {
        _suppressBranchEvent = true;
        _branchCombo.Items.Clear();

        if (_branchContext.IsCompanyWideScope)
        {
            _branchCombo.Items.Add(new ModalComboOption(null, AppStrings.T("AllBranchesScope")));
        }

        foreach (var b in _branchContext.AccessibleBranches)
        {
            _branchCombo.Items.Add(new ModalComboOption(b.BranchId, $"{b.Code} - {b.Name}"));
        }

        _branchCombo.Enabled = !_branchContext.HasSingleBranchOnly;

        for (int i = 0; i < _branchCombo.Items.Count; i++)
        {
            if (_branchCombo.Items[i] is ModalComboOption opt && Equals(opt.Value, _branchContext.ActiveBranchId))
            {
                _branchCombo.SelectedIndex = i;
                break;
            }
        }
        if (_branchCombo.SelectedIndex < 0 && _branchCombo.Items.Count > 0)
            _branchCombo.SelectedIndex = 0;

        _suppressBranchEvent = false;
    }

    private void OnBranchDropdownChanged()
    {
        if (_suppressBranchEvent) return;
        if (_branchCombo.SelectedItem is ModalComboOption opt)
        {
            var targetId = opt.Value is long id ? id : (long?)null;
            _branchContext.TrySetActiveBranch(targetId);
        }
    }

    private async void OnBranchContextChanged(object? sender, EventArgs e)
    {
        UpdateUserStatusLabels();
        if (_contentHostPanel.Controls.Count > 0 && _contentHostPanel.Controls[0] is IModuleView activeView)
        {
            await UiAsyncRunner.RunSafeAsync(this, () => activeView.OnBranchContextChangedAsync(), null, null, _logger);
        }
    }

    private void OnNavigationRequested(object? sender, string moduleKey)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => _ = NavigateInternalAsync(moduleKey));
            return;
        }
        _ = NavigateInternalAsync(moduleKey);
    }

    private async Task NavigateInternalAsync(string navKey)
    {
        if (IsDisposed) return;

        // Verify permission for target item before opening
        var match = NavigationCatalog.Groups
            .SelectMany(g => g.Items.Select(i => (Group: g, Item: i)))
            .FirstOrDefault(x => string.Equals(x.Item.Key, navKey, StringComparison.OrdinalIgnoreCase));

        if (match.Item != null &&
            match.Item.RequiredAnyPermissions.Length > 0 &&
            !_authorization.HasAnyPermission(match.Item.RequiredAnyPermissions))
        {
            MessageBox.Show(this, "Bạn không có quyền truy cập chức năng này.", "Không đủ quyền", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _activeNavKey = navKey;
        if (match.Group != null && _expandedGroups.Add(match.Group.GroupKey))
            RebuildNavigationSidebar();
        else
            HighlightActiveNavButton();

        if (match.Item != null && match.Group != null)
        {
            var groupTitle = AppStrings.T(match.Group.TitleKey);
            var itemTitle = AppStrings.T(match.Item.TitleKey);
            if (itemTitle == match.Item.TitleKey) itemTitle = match.Item.DefaultTitle;
            _breadcrumbLabel.Text = $"{groupTitle}  ›  {itemTitle}";
            _pageTitleLabel.Text = itemTitle;
        }

        var controlKey = ResolveModuleControlKey(navKey);
        if (!_moduleControls.TryGetValue(controlKey, out var control))
        {
            control = CreateModuleControl(controlKey);
            control.Dock = DockStyle.Fill;
            _moduleControls[controlKey] = control;
        }

        SelectModuleSubSection(control, navKey);

        _contentHostPanel.SuspendLayout();
        _contentHostPanel.Controls.Clear();
        _contentHostPanel.Controls.Add(control);
        _contentHostPanel.ResumeLayout();

        if (control is IModuleView moduleView)
        {
            await UiAsyncRunner.RunSafeAsync(this, () => moduleView.LoadInitialAsync(), null, null, _logger);
        }
    }

    private static string ResolveModuleControlKey(string navKey)
    {
        if (navKey.StartsWith("Org.", StringComparison.OrdinalIgnoreCase)) return "Organization";
        if (navKey.StartsWith("Crm.", StringComparison.OrdinalIgnoreCase)) return "CRM";
        if (navKey.StartsWith("Catalog.", StringComparison.OrdinalIgnoreCase)) return "Catalog";
        if (navKey.StartsWith("Sales.", StringComparison.OrdinalIgnoreCase)) return "Sales";
        if (navKey.StartsWith("Shipping.", StringComparison.OrdinalIgnoreCase)) return "Shipping";
        if (navKey.StartsWith("Carrier.", StringComparison.OrdinalIgnoreCase)) return "Carrier";
        if (navKey.StartsWith("Warehouse.", StringComparison.OrdinalIgnoreCase)) return "Warehouse";
        if (navKey.StartsWith("Inventory.", StringComparison.OrdinalIgnoreCase)) return "Inventory";
        if (navKey.StartsWith("Finance.", StringComparison.OrdinalIgnoreCase)) return "Finance";
        if (navKey.StartsWith("Cod.", StringComparison.OrdinalIgnoreCase)) return "COD";
        if (navKey.StartsWith("Notifications.", StringComparison.OrdinalIgnoreCase)) return "Notifications";
        if (navKey.StartsWith("Reports.", StringComparison.OrdinalIgnoreCase)) return "Reports";
        if (navKey.StartsWith("Settings.", StringComparison.OrdinalIgnoreCase)) return "Settings";
        return "Dashboard";
    }

    private Control CreateModuleControl(string controlKey) => controlKey switch
    {
        "Dashboard" => _serviceProvider.GetRequiredService<ExecutiveDashboardControl>(),
        "Organization" => _serviceProvider.GetRequiredService<OrganizationWorkspaceControl>(),
        "CRM" => _serviceProvider.GetRequiredService<CrmWorkspaceControl>(),
        "Catalog" => _serviceProvider.GetRequiredService<CatalogWorkspaceControl>(),
        "Sales" => _serviceProvider.GetRequiredService<SalesOrdersWorkspaceControl>(),
        "Shipping" => _serviceProvider.GetRequiredService<ShippingWorkspaceControl>(),
        "Carrier" => _serviceProvider.GetRequiredService<CarrierWorkspaceControl>(),
        "Warehouse" => _serviceProvider.GetRequiredService<WarehouseWorkspaceControl>(),
        "Inventory" => _serviceProvider.GetRequiredService<InventoryWorkspaceControl>(),
        "Finance" => _serviceProvider.GetRequiredService<FinanceWorkspaceControl>(),
        "COD" => _serviceProvider.GetRequiredService<CodWorkspaceControl>(),
        "Notifications" => CreateNotificationsControl(),
        "Reports" => _serviceProvider.GetRequiredService<ReportsWorkspaceControl>(),
        "Settings" => _serviceProvider.GetRequiredService<SettingsWorkspaceControl>(),
        _ => _serviceProvider.GetRequiredService<ExecutiveDashboardControl>()
    };

    private NotificationsWorkspaceControl CreateNotificationsControl()
    {
        var ctrl = _serviceProvider.GetRequiredService<NotificationsWorkspaceControl>();
        ctrl.UnreadCountChanged += async (_, _) => await RefreshNotificationBadgeAndHealthAsync();
        return ctrl;
    }

    private static void SelectModuleSubSection(Control control, string navKey)
    {
        switch (control)
        {
            case OrganizationWorkspaceControl org: org.SelectSection(navKey); break;
            case CatalogWorkspaceControl cat: cat.SelectSection(navKey); break;
            case ShippingWorkspaceControl ship: ship.SelectSection(navKey); break;
            case CarrierWorkspaceControl car: car.SelectSection(navKey); break;
            case WarehouseWorkspaceControl wh: wh.SelectSection(navKey); break;
            case InventoryWorkspaceControl inv: inv.SelectSection(navKey); break;
            case FinanceWorkspaceControl fin: fin.SelectSection(navKey); break;
            case CodWorkspaceControl cod: cod.SelectSection(navKey); break;
            case NotificationsWorkspaceControl n: n.SelectSection(navKey); break;
            case ReportsWorkspaceControl rep: rep.SelectSection(navKey); break;
        }
    }

    private async Task RefreshNotificationBadgeAndHealthAsync()
    {
        if (IsDisposed) return;

        try
        {
            if (_authorization.HasPermission(ClientPermissions.Notification.NotificationsReadOwn))
            {
                var unread = await _notificationClient.GetUnreadCountAsync();
                _bellBtn.Text = $"🔔 {unread.UnreadCount}";
                _bellBtn.BackColor = unread.UnreadCount > 0 ? Color.FromArgb(254, 243, 199) : Color.White;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not poll unread notification count.");
        }

        try
        {
            var health = await _healthClient.GetHealthAsync();
            _statusApiLabel.Text = health != null
                ? $"API: {health.Status} ({_settingsService.Settings.ApiBaseUrl})"
                : $"API: Mất kết nối ({_settingsService.Settings.ApiBaseUrl})";
            _statusApiLabel.ForeColor = health != null ? AppTheme.SuccessEmerald : AppTheme.DangerRed;
        }
        catch
        {
            _statusApiLabel.Text = $"API: Mất kết nối ({_settingsService.Settings.ApiBaseUrl})";
            _statusApiLabel.ForeColor = AppTheme.DangerRed;
        }
    }

    private void UpdateUserStatusLabels()
    {
        var user = _authService.CurrentUser;
        _userMenuBtn.Text = user?.DisplayName ?? user?.Username ?? "User";
        var rolesText = user != null ? string.Join(", ", user.Roles) : "-";
        _statusUserLabel.Text = $"Người dùng: {user?.Username ?? "-"} ({rolesText})";
        _statusEnvLabel.Text = $"Môi trường: {_settingsService.Settings.Environment}";
        _statusBranchLabel.Text = _branchContext.ActiveBranch != null
            ? $"Chi nhánh: {_branchContext.ActiveBranch.Code} - {_branchContext.ActiveBranch.Name}"
            : $"Phạm vi: {AppStrings.T("AllBranchesScope")}";
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        if (InvokeRequired) { BeginInvoke(UpdateUserStatusLabels); return; }
        UpdateUserStatusLabels();
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        if (InvokeRequired) { BeginInvoke(() => OnLanguageChanged(sender, e)); return; }
        Text = $"{AppStrings.T("AppTitle")} - Desktop Client";
        PopulateBranchCombo();
        RebuildNavigationSidebar();
        UpdateUserStatusLabels();
    }

    private async Task ExecuteLogoutAsync(bool allDevices)
    {
        if (_isClosingForLogout) return;
        _isClosingForLogout = true;
        _unreadTimer.Stop();

        try
        {
            if (allDevices) await _authService.LogoutAllAsync();
            else await _authService.LogoutAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during logout.");
        }

        ReloginRequested = true;
        Close();
    }

    private void OnSessionExpired(object? sender, string? reason)
    {
        if (IsDisposed || _isClosingForLogout) return;
        if (InvokeRequired) { BeginInvoke(() => OnSessionExpired(sender, reason)); return; }

        _isClosingForLogout = true;
        _unreadTimer.Stop();
        var msg = string.IsNullOrWhiteSpace(reason) ? AppStrings.T("SessionExpiredMessage") : reason;
        MessageBox.Show(this, msg, AppStrings.T("SessionExpiredTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        ReloginRequested = true;
        Close();
    }

    private void UnsubscribeEvents()
    {
        _unreadTimer?.Stop();
        _unreadTimer?.Dispose();
        _branchContext.ActiveBranchChanged -= OnBranchContextChanged;
        _authService.SessionExpired -= OnSessionExpired;
        _settingsService.SettingsChanged -= OnSettingsChanged;
        AppStrings.LanguageChanged -= OnLanguageChanged;
    }
}
