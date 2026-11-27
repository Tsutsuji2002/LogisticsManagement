using AntdUI;
using Logistics.Client.Application.Interfaces;
using Logistics.Client.Application.Localization;
using Logistics.Client.Application.Navigation;
using Logistics.Client.Infrastructure.Http;
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
using Label = System.Windows.Forms.Label;
using Timer = System.Windows.Forms.Timer;

namespace Logistics.Client.WinForms;

/// <summary>
/// AntdUI Main Application Shell with collapsible Menu sidebar, Breadcrumb header,
/// branch selector, Light/Dark theme switch, language switch, notification badge,
/// user dropdown menu, and cached RBAC-gated workspace hosting.
/// </summary>
public partial class MainForm : AntdUI.Window, IThemeAware
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IAuthenticationService _authService;
    private readonly IAuthorizationService _authorization;
    private readonly IBranchContextService _branchContext;
    private readonly INavigationService _navigation;
    private readonly IClientSettingsService _settingsService;
    private readonly INotificationApiClient _notificationApi;
    private readonly ApiClientOptions _apiOptions;
    private readonly ILogger<MainForm> _logger;

    private readonly AntdUI.PageHeader _windowChrome;
    private readonly System.Windows.Forms.Panel _sidebarContainer;
    private readonly System.Windows.Forms.Panel _sidebarBrandPanel;
    private readonly Label _brandTitleLabel;
    private readonly Label _brandSubLabel;
    private readonly AntdUI.Menu _sidebarMenu;

    private readonly System.Windows.Forms.Panel _subHeaderPanel;
    private readonly System.Windows.Forms.Panel _subHeaderDivider;
    private readonly AntdUI.Button _collapseBtn;
    private readonly AntdUI.Breadcrumb _breadcrumb;
    private readonly AntdUI.Select _branchSelect;
    private readonly AntdUI.Select _languageSelect;
    private readonly AntdUI.Button _themeToggleBtn;
    private readonly AntdUI.Button _bellBtn;
    private readonly AntdUI.Badge _unreadBadge;
    private readonly AntdUI.Dropdown _userDropdown;

    private readonly System.Windows.Forms.Panel _contentHost;
    private readonly System.Windows.Forms.Panel _footerPanel;
    private readonly Label _footerInfoLabel;
    private readonly Label _footerStatusLabel;
    private readonly Timer _notificationTimer;

    private readonly Dictionary<string, Control> _cachedModules = new(StringComparer.OrdinalIgnoreCase);
    private bool _sidebarCollapsed;
    private bool _suppressSelectionEvents;
    private NavigationItemDescriptor? _currentNavItem;
    private NavigationGroupDescriptor? _currentNavGroup;

    public bool ReloginRequested { get; private set; }

    public MainForm(
        IServiceProvider serviceProvider,
        IAuthenticationService authService,
        IAuthorizationService authorization,
        IBranchContextService branchContext,
        INavigationService navigation,
        IClientSettingsService settingsService,
        INotificationApiClient notificationApi,
        ApiClientOptions apiOptions,
        ILogger<MainForm> logger)
    {
        InitializeComponent();

        _serviceProvider = serviceProvider;
        _authService = authService;
        _authorization = authorization;
        _branchContext = branchContext;
        _navigation = navigation;
        _settingsService = settingsService;
        _notificationApi = notificationApi;
        _apiOptions = apiOptions;
        _logger = logger;

        FormBorderStyle = FormBorderStyle.None;
        Resizable = true;
        Mode = AppTheme.IsDark ? TAMode.Dark : TAMode.Light;

        // Top OS Window Chrome (AntdUI frameless bar)
        _windowChrome = new AntdUI.PageHeader
        {
            Dock = DockStyle.Top,
            Height = 42,
            Text = "Logistics Management System",
            SubText = $"Enterprise Operations • {_settingsService.Settings.Environment}",
            Font = AppTheme.BodyBoldFont,
            DragMove = true,
            ShowButton = true,
            MinimizeBox = true,
            MaximizeBox = true,
            CancelButton = true,
            DividerShow = true,
            DividerThickness = 1F
        };

        // Left Collapsible Navigation Sidebar
        _sidebarContainer = new System.Windows.Forms.Panel
        {
            Dock = DockStyle.Left,
            Width = 264,
            BackColor = AppTheme.SidebarBg
        };

        _sidebarBrandPanel = new System.Windows.Forms.Panel
        {
            Dock = DockStyle.Top,
            Height = 64,
            Padding = new Padding(16, 12, 12, 8),
            BackColor = AppTheme.SidebarBg
        };

        _brandTitleLabel = new Label
        {
            Text = "LOGISTICS ERP",
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(16, 12),
            BackColor = Color.Transparent
        };
        _brandSubLabel = new Label
        {
            Text = "WMS • TMS • COD • FINANCE",
            Font = AppTheme.SmallFont,
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize = true,
            Location = new Point(16, 36),
            BackColor = Color.Transparent
        };
        _sidebarBrandPanel.Controls.Add(_brandTitleLabel);
        _sidebarBrandPanel.Controls.Add(_brandSubLabel);

        _sidebarMenu = new AntdUI.Menu
        {
            Dock = DockStyle.Fill,
            Mode = TMenuMode.Inline,
            ColorScheme = TAMode.Dark,
            BackColor = AppTheme.SidebarBg,
            ForeColor = Color.FromArgb(203, 213, 225),
            ForeActive = Color.White,
            BackActive = AppTheme.PrimaryBlue,
            BackHover = AppTheme.SidebarHover,
            Radius = 6,
            Unique = true,
            AutoCollapse = false,
            ShowSubBack = true,
            Indent = true,
            UseMnemonic = false,
            Font = AppTheme.BodyFont
        };
        _sidebarMenu.SelectChanged += async (_, e) =>
        {
            if (_suppressSelectionEvents) return;
            if (e.Value?.Tag is NavigationItemDescriptor item)
                await NavigateToItemAsync(item);
        };

        _sidebarContainer.Controls.Add(_sidebarMenu);
        _sidebarContainer.Controls.Add(_sidebarBrandPanel);

        // Top Workspace Toolbar (Breadcrumb + Context Switches + User Menu)
        _subHeaderPanel = new System.Windows.Forms.Panel
        {
            Dock = DockStyle.Top,
            Height = 56,
            Padding = new Padding(14, 10, 14, 10)
        };

        var headerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        headerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var leftNavPanel = new System.Windows.Forms.Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 12, 0)
        };

        _collapseBtn = AppTheme.CreateSecondaryButton("☰", 40);
        _collapseBtn.Height = 34;
        _collapseBtn.MinimumSize = new Size(40, 34);
        _collapseBtn.Dock = DockStyle.Left;
        _collapseBtn.Click += (_, _) => ToggleSidebar();

        _breadcrumb = new AntdUI.Breadcrumb
        {
            Dock = DockStyle.Fill,
            Height = 34,
            Font = AppTheme.BodyBoldFont,
            Separator = "/",
            Padding = new Padding(10, 2, 4, 0)
        };

        leftNavPanel.Controls.Add(_breadcrumb);
        leftNavPanel.Controls.Add(_collapseBtn);

        var rightContextFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };

        _userDropdown = new AntdUI.Dropdown
        {
            Height = 34,
            Width = 152,
            Radius = 6,
            Type = TTypeMini.Default,
            BorderWidth = 1F,
            Trigger = Trigger.Click,
            Placement = TAlignFrom.BR,
            Font = AppTheme.BodyBoldFont,
            Text = "Tài khoản",
            Margin = new Padding(6, 0, 0, 0)
        };
        _userDropdown.Items.AddRange([
            new SelectItem("Hồ sơ & Cài đặt", "SETTINGS"),
            new SelectItem("Làm mới màn hình", "REFRESH"),
            new SelectItem("Đăng xuất", "LOGOUT"),
            new SelectItem("Đăng xuất tất cả thiết bị", "LOGOUT_ALL")
        ]);
        _userDropdown.SelectedValueChanged += async (_, e) => await HandleUserDropdownActionAsync(e.Value?.ToString());

        var bellHost = new System.Windows.Forms.Panel
        {
            Width = 88,
            Height = 34,
            Margin = new Padding(6, 0, 0, 0),
            BackColor = Color.Transparent
        };
        _bellBtn = AppTheme.CreateSecondaryButton("TB", 58);
        _bellBtn.Location = new Point(0, 0);
        _bellBtn.Click += async (_, _) => await NavigateByKeyAsync("Notifications.Center");

        _unreadBadge = new AntdUI.Badge
        {
            Location = new Point(58, 8),
            Size = new Size(28, 18),
            State = TState.Default,
            Text = "0",
            Font = AppTheme.SmallFont
        };
        bellHost.Controls.Add(_bellBtn);
        bellHost.Controls.Add(_unreadBadge);

        _themeToggleBtn = AppTheme.CreateSecondaryButton(AppTheme.IsDark ? "Light" : "Dark", 82);
        _themeToggleBtn.Margin = new Padding(6, 0, 0, 0);
        _themeToggleBtn.Click += (_, _) =>
        {
            AppTheme.ToggleTheme(this);
            UpdateThemeToggleText();
            UpdateFooterStatus();
        };

        _languageSelect = new AntdUI.Select
        {
            Width = 96,
            Height = 34,
            Radius = 6,
            List = true,
            Font = AppTheme.BodyFont,
            Margin = new Padding(6, 0, 0, 0)
        };
        _languageSelect.Items.Add(new SelectItem("Tiếng Việt", "vi-VN"));
        _languageSelect.Items.Add(new SelectItem("English", "en-US"));
        _languageSelect.SelectedValue = _settingsService.Settings.Culture;
        _languageSelect.SelectedValueChanged += (_, e) => OnLanguageChanged(e.Value?.ToString());

        _branchSelect = new AntdUI.Select
        {
            Width = 205,
            Height = 34,
            Radius = 6,
            List = true,
            PlaceholderText = "Chọn chi nhánh...",
            Font = AppTheme.BodyFont,
            Margin = new Padding(6, 0, 0, 0)
        };
        _branchSelect.SelectedIndexChanged += async (_, _) => await OnBranchSelectedAsync();

        rightContextFlow.Controls.Add(_userDropdown);
        rightContextFlow.Controls.Add(bellHost);
        rightContextFlow.Controls.Add(_themeToggleBtn);
        rightContextFlow.Controls.Add(_languageSelect);
        rightContextFlow.Controls.Add(_branchSelect);

        headerLayout.Controls.Add(leftNavPanel, 0, 0);
        headerLayout.Controls.Add(rightContextFlow, 1, 0);
        _subHeaderPanel.Controls.Add(headerLayout);

        _subHeaderDivider = new System.Windows.Forms.Panel
        {
            Dock = DockStyle.Top,
            Height = 1
        };

        // Main Workspace Area
        _contentHost = new System.Windows.Forms.Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14)
        };

        // Footer Bar
        _footerPanel = new System.Windows.Forms.Panel
        {
            Dock = DockStyle.Bottom,
            Height = 30,
            Padding = new Padding(14, 4, 14, 4)
        };

        _footerInfoLabel = new Label
        {
            Dock = DockStyle.Left,
            AutoSize = false,
            Width = 720,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = AppTheme.SmallFont,
            BackColor = Color.Transparent
        };
        _footerStatusLabel = new Label
        {
            Dock = DockStyle.Right,
            AutoSize = false,
            Width = 420,
            TextAlign = ContentAlignment.MiddleRight,
            Font = AppTheme.SmallFont,
            BackColor = Color.Transparent
        };
        _footerPanel.Controls.Add(_footerInfoLabel);
        _footerPanel.Controls.Add(_footerStatusLabel);

        // Assemble Shell Layout
        var workspaceColumn = new System.Windows.Forms.Panel
        {
            Dock = DockStyle.Fill
        };
        workspaceColumn.Controls.Add(_contentHost);
        workspaceColumn.Controls.Add(_subHeaderDivider);
        workspaceColumn.Controls.Add(_subHeaderPanel);
        workspaceColumn.Controls.Add(_footerPanel);

        Controls.Add(workspaceColumn);
        Controls.Add(_sidebarContainer);
        Controls.Add(_windowChrome);

        // Background notification polling
        _notificationTimer = new Timer { Interval = 60_000 };
        _notificationTimer.Tick += async (_, _) => await PollUnreadNotificationsAsync();

        _navigation.NavigationRequested += OnExternalNavigationRequested;
        _authService.SessionExpired += OnSessionExpired;
        AppTheme.ThemeChanged += OnAppThemeChanged;

        Shown += async (_, _) => await InitializeShellAsync();
        ApplyTheme();
    }

    private async Task InitializeShellAsync()
    {
        PopulateUserContext();
        PopulateBranchSelector();
        PopulateSidebarMenu();
        UpdateFooterStatus();

        await NavigateByKeyAsync("Dashboard");
        await PollUnreadNotificationsAsync();
        _notificationTimer.Start();
    }

    private void PopulateUserContext()
    {
        var user = _authService.CurrentUser;
        var displayName = user?.DisplayName ?? user?.Username ?? "User";
        _userDropdown.Text = displayName;
    }

    private void PopulateBranchSelector()
    {
        _suppressSelectionEvents = true;
        try
        {
            _branchSelect.Items.Clear();

            if (_branchContext.IsCompanyWideScope)
                _branchSelect.Items.Add(new SelectItem("Toàn hệ thống (Tất cả chi nhánh)", -1L));

            foreach (var b in _branchContext.AccessibleBranches)
                _branchSelect.Items.Add(new SelectItem($"{b.Code} - {b.Name}", b.BranchId));

            var targetId = _branchContext.ActiveBranchId ?? -1L;
            _branchSelect.SelectedValue = targetId;
            if (_branchSelect.SelectedIndex < 0 && _branchSelect.Items.Count > 0)
                _branchSelect.SelectedIndex = 0;

            _branchSelect.Enabled = !_branchContext.HasSingleBranchOnly;
        }
        finally
        {
            _suppressSelectionEvents = false;
        }
    }

    private void PopulateSidebarMenu()
    {
        _suppressSelectionEvents = true;
        try
        {
            _sidebarMenu.Items.Clear();
            var visibleGroups = NavigationCatalog.GetVisibleGroups(_authorization);

            foreach (var group in visibleGroups)
            {
                if (group.Items.Count == 1 && group.GroupKey.Equals("Dashboard", StringComparison.OrdinalIgnoreCase))
                {
                    var dash = group.Items[0];
                    var item = new AntdUI.MenuItem(GetGroupOrItemTitle(dash.TitleKey, dash.DefaultTitle))
                    {
                        ID = dash.Key,
                        Tag = dash,
                        IconSvg = ResolveGroupIconSvg(group.GroupKey)
                    };
                    _sidebarMenu.Items.Add(item);
                    continue;
                }

                var groupItem = new AntdUI.MenuItem(GetGroupOrItemTitle(group.TitleKey, group.DefaultTitle))
                {
                    ID = group.GroupKey,
                    IconSvg = ResolveGroupIconSvg(group.GroupKey),
                    Expand = true
                };

                foreach (var child in group.Items)
                {
                    var childItem = new AntdUI.MenuItem(GetGroupOrItemTitle(child.TitleKey, child.DefaultTitle))
                    {
                        ID = child.Key,
                        Tag = child
                    };
                    groupItem.Sub.Add(childItem);
                }

                _sidebarMenu.Items.Add(groupItem);
            }
        }
        finally
        {
            _suppressSelectionEvents = false;
        }
    }

    private static string GetGroupOrItemTitle(string titleKey, string fallback)
    {
        var localized = AppStrings.T(titleKey);
        return string.Equals(localized, titleKey, StringComparison.Ordinal) ? fallback : localized;
    }

    private void ToggleSidebar()
    {
        _sidebarCollapsed = !_sidebarCollapsed;
        _sidebarMenu.Collapsed = _sidebarCollapsed;
        _sidebarContainer.Width = _sidebarCollapsed ? 68 : 264;
        _brandTitleLabel.Text = _sidebarCollapsed ? "L-ERP" : "LOGISTICS ERP";
        _brandSubLabel.Visible = !_sidebarCollapsed;
    }

    private async void OnExternalNavigationRequested(object? sender, string moduleKey)
    {
        if (IsDisposed) return;
        await NavigateByKeyAsync(moduleKey);
    }

    private Task NavigateByKeyAsync(string navKey)
    {
        var visible = NavigationCatalog.GetVisibleGroups(_authorization);
        foreach (var g in visible)
        {
            var match = g.Items.FirstOrDefault(i =>
                string.Equals(i.Key, navKey, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(i.ParentGroup, navKey, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                SelectMenuNode(match.Key);
                return NavigateToItemAsync(match, g);
            }
        }
        return Task.CompletedTask;
    }

    private void SelectMenuNode(string key)
    {
        _suppressSelectionEvents = true;
        try
        {
            foreach (var root in _sidebarMenu.Items)
            {
                if (string.Equals(root.ID, key, StringComparison.OrdinalIgnoreCase))
                {
                    root.Select = true;
                    return;
                }
                foreach (var sub in root.Sub)
                {
                    sub.Select = string.Equals(sub.ID, key, StringComparison.OrdinalIgnoreCase);
                    if (sub.Select)
                        root.Expand = true;
                }
            }
        }
        finally
        {
            _suppressSelectionEvents = false;
        }
    }

    private async Task NavigateToItemAsync(NavigationItemDescriptor item, NavigationGroupDescriptor? group = null)
    {
        group ??= NavigationCatalog.GetVisibleGroups(_authorization)
            .FirstOrDefault(g => g.Items.Any(i => string.Equals(i.Key, item.Key, StringComparison.OrdinalIgnoreCase)));

        _currentNavItem = item;
        _currentNavGroup = group;
        UpdateBreadcrumb(group, item);

        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            var control = GetOrCreateModuleControl(item.ParentGroup);
            if (_contentHost.Controls.Count == 0 || _contentHost.Controls[0] != control)
            {
                _contentHost.SuspendLayout();
                _contentHost.Controls.Clear();
                control.Dock = DockStyle.Fill;
                _contentHost.Controls.Add(control);
                _contentHost.ResumeLayout(true);
            }

            SelectModuleSubSection(control, item.Key);
            AppTheme.ApplyThemeToTree(control);

            if (control is IModuleView moduleView)
                await moduleView.LoadInitialAsync();
        }, logger: _logger, showMessageBoxOnError: true);
    }

    private Control GetOrCreateModuleControl(string moduleName)
    {
        if (_cachedModules.TryGetValue(moduleName, out var existing))
            return existing;

        Control created = moduleName switch
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
            "Notifications" => _serviceProvider.GetRequiredService<NotificationsWorkspaceControl>(),
            "Reports" => _serviceProvider.GetRequiredService<ReportsWorkspaceControl>(),
            "Settings" => _serviceProvider.GetRequiredService<SettingsWorkspaceControl>(),
            _ => _serviceProvider.GetRequiredService<ExecutiveDashboardControl>()
        };

        _cachedModules[moduleName] = created;
        return created;
    }

    private static void SelectModuleSubSection(Control control, string navKey)
    {
        switch (control)
        {
            case OrganizationWorkspaceControl org: org.SelectSection(navKey); break;
            case CatalogWorkspaceControl cat: cat.SelectSection(navKey); break;
            case ShippingWorkspaceControl ship: ship.SelectSection(navKey); break;
            case CarrierWorkspaceControl carrier: carrier.SelectSection(navKey); break;
            case WarehouseWorkspaceControl wh: wh.SelectSection(navKey); break;
            case InventoryWorkspaceControl inv: inv.SelectSection(navKey); break;
            case FinanceWorkspaceControl fin: fin.SelectSection(navKey); break;
            case CodWorkspaceControl cod: cod.SelectSection(navKey); break;
            case NotificationsWorkspaceControl notif: notif.SelectSection(navKey); break;
            case ReportsWorkspaceControl rep: rep.SelectSection(navKey); break;
        }
    }

    private void UpdateBreadcrumb(NavigationGroupDescriptor? group, NavigationItemDescriptor item)
    {
        _breadcrumb.Items.Clear();
        _breadcrumb.Items.Add(new BreadcrumbItem { Text = "Logistics ERP" });
        if (group != null && !string.Equals(group.GroupKey, "Dashboard", StringComparison.OrdinalIgnoreCase))
            _breadcrumb.Items.Add(new BreadcrumbItem { Text = GetGroupOrItemTitle(group.TitleKey, group.DefaultTitle) });
        _breadcrumb.Items.Add(new BreadcrumbItem { Text = GetGroupOrItemTitle(item.TitleKey, item.DefaultTitle) });
        _windowChrome.SubText = $"{_settingsService.Settings.Environment} • {GetGroupOrItemTitle(item.TitleKey, item.DefaultTitle)}";
    }

    private async Task OnBranchSelectedAsync()
    {
        if (_suppressSelectionEvents) return;
        if (_branchSelect.SelectedIndex < 0 || _branchSelect.SelectedIndex >= _branchSelect.Items.Count)
            return;

        if (_branchSelect.Items[_branchSelect.SelectedIndex] is SelectItem item && item.Tag is long rawId)
        {
            long? nextBranch = rawId < 0 ? null : rawId;
            if (_branchContext.TrySetActiveBranch(nextBranch))
            {
                UpdateFooterStatus();
                if (_contentHost.Controls.Count > 0 && _contentHost.Controls[0] is IModuleView mv)
                {
                    await UiAsyncRunner.RunSafeAsync(this, () => mv.OnBranchContextChangedAsync(), logger: _logger, showMessageBoxOnError: false);
                }
                UiAsyncRunner.ShowToastInfo(this, $"Đã chuyển phạm vi chi nhánh: {item.Text}");
            }
        }
    }

    private void OnLanguageChanged(string? culture)
    {
        if (_suppressSelectionEvents || string.IsNullOrWhiteSpace(culture)) return;
        var s = _settingsService.Settings;
        if (string.Equals(s.Culture, culture, StringComparison.OrdinalIgnoreCase)) return;

        _settingsService.UpdateSettings(s.Environment, s.ApiBaseUrl, s.TimeoutSeconds, culture);
        PopulateSidebarMenu();
        if (_currentNavItem != null)
        {
            SelectMenuNode(_currentNavItem.Key);
            UpdateBreadcrumb(_currentNavGroup, _currentNavItem);
        }
    }

    private async Task PollUnreadNotificationsAsync()
    {
        if (IsDisposed || !_authService.IsAuthenticated) return;
        if (!_authorization.HasPermission(Logistics.Client.Application.Permissions.ClientPermissions.Notification.NotificationsReadOwn))
            return;

        try
        {
            var unread = await _notificationApi.GetUnreadCountAsync();
            if (!IsDisposed)
            {
                _unreadBadge.Text = unread.UnreadCount.ToString();
                _unreadBadge.State = unread.UnreadCount > 0 ? TState.Error : TState.Default;
            }
        }
        catch
        {
            // Transient poll failure should not interrupt UI workflow
        }
    }

    private async Task HandleUserDropdownActionAsync(string? action)
    {
        if (string.IsNullOrWhiteSpace(action)) return;
        switch (action)
        {
            case "SETTINGS":
                await NavigateByKeyAsync("Settings.Workspace");
                break;
            case "REFRESH":
                if (_contentHost.Controls.Count > 0 && _contentHost.Controls[0] is IModuleView mv)
                    await UiAsyncRunner.RunSafeAsync(this, () => mv.RefreshViewAsync(), logger: _logger, showMessageBoxOnError: true);
                break;
            case "LOGOUT":
                await ExecuteLogoutAsync(allDevices: false);
                break;
            case "LOGOUT_ALL":
                await ExecuteLogoutAsync(allDevices: true);
                break;
        }
    }

    private async Task ExecuteLogoutAsync(bool allDevices)
    {
        var prompt = allDevices
            ? "Đăng xuất khỏi TẤT CẢ các thiết bị và quay lại màn hình đăng nhập?"
            : "Bạn có chắc chắn muốn đăng xuất?";
        if (!UiAsyncRunner.Confirm(this, prompt, "Xác nhận đăng xuất")) return;

        _notificationTimer.Stop();
        if (allDevices)
            await _authService.LogoutAllAsync();
        else
            await _authService.LogoutAsync();

        ReloginRequested = true;
        Close();
    }

    private void OnSessionExpired(object? sender, string? reason)
    {
        if (IsDisposed) return;
        BeginInvoke(() =>
        {
            _notificationTimer.Stop();
            UiAsyncRunner.ShowModalAlert(this, "Phiên đăng nhập hết hạn", reason ?? "Vui lòng đăng nhập lại.", TType.Warn);
            ReloginRequested = true;
            Close();
        });
    }

    private void UpdateThemeToggleText()
    {
        _themeToggleBtn.Text = AppTheme.IsDark ? "Light" : "Dark";
    }

    private void UpdateFooterStatus()
    {
        var s = _settingsService.Settings;
        var user = _authService.CurrentUser?.Username ?? "-";
        var roles = _authorization.Roles.Count > 0 ? string.Join(", ", _authorization.Roles) : "-";
        var branch = _branchContext.ActiveBranch?.Name ?? "Toàn hệ thống";
        _footerInfoLabel.Text = $"API: {s.ApiBaseUrl} ({s.Environment})  |  Người dùng: {user} [{roles}]";
        _footerStatusLabel.Text = $"Chi nhánh: {branch}  |  Giao diện: {AppTheme.CurrentMode}";
    }

    private void OnAppThemeChanged(object? sender, EventArgs e)
    {
        if (!IsDisposed)
        {
            UpdateThemeToggleText();
            UpdateFooterStatus();
        }
    }

    public void ApplyTheme()
    {
        Mode = AppTheme.IsDark ? TAMode.Dark : TAMode.Light;
        BackColor = AppTheme.SurfaceBg;
        ForeColor = AppTheme.TextPrimary;

        _windowChrome.BackColor = AppTheme.HeaderBg;
        _windowChrome.ForeColor = AppTheme.TextPrimary;
        _windowChrome.DividerColor = AppTheme.BorderSubtle;

        _subHeaderPanel.BackColor = AppTheme.HeaderBg;
        _subHeaderDivider.BackColor = AppTheme.BorderSubtle;
        _breadcrumb.ForeColor = AppTheme.TextSecondary;
        _breadcrumb.ForeActive = AppTheme.TextPrimary;

        _contentHost.BackColor = AppTheme.SurfaceBg;
        _footerPanel.BackColor = AppTheme.RowAltBg;
        _footerInfoLabel.ForeColor = AppTheme.TextSecondary;
        _footerStatusLabel.ForeColor = AppTheme.TextSecondary;
        UpdateThemeToggleText();
    }

    private static string ResolveGroupIconSvg(string groupKey) => groupKey switch
    {
        "Dashboard" => "<svg viewBox=\"0 0 24 24\"><path d=\"M3 13h8V3H3v10zm0 8h8v-6H3v6zm10 0h8V11h-8v10zm0-18v6h8V3h-8z\"/></svg>",
        "Organization" => "<svg viewBox=\"0 0 24 24\"><path d=\"M12 7V3H2v18h20V7H12zM6 19H4v-2h2v2zm0-4H4v-2h2v2zm0-4H4V9h2v2zm0-4H4V5h2v2zm4 12H8v-2h2v2zm0-4H8v-2h2v2zm0-4H8V9h2v2zm0-4H8V5h2v2zm10 12h-8v-2h2v-2h-2v-2h2v-2h-2V9h8v10z\"/></svg>",
        "CRM" => "<svg viewBox=\"0 0 24 24\"><path d=\"M16 11c1.66 0 2.99-1.34 2.99-3S17.66 5 16 5s-3 1.34-3 3 1.34 3 3 3zm-8 0c1.66 0 2.99-1.34 2.99-3S9.66 5 8 5 5 6.34 5 8s1.34 3 3 3zm0 2c-2.33 0-7 1.17-7 3.5V19h14v-2.5c0-2.33-4.67-3.5-7-3.5zm8 0c-.29 0-.62.02-.97.05 1.16.84 1.97 1.97 1.97 3.45V19h6v-2.5c0-2.33-4.67-3.5-7-3.5z\"/></svg>",
        "Catalog" => "<svg viewBox=\"0 0 24 24\"><path d=\"M20 6h-2.18c.11-.31.18-.65.18-1a2.996 2.996 0 0 0-5.5-1.65l-.5.67-.5-.68C10.96 2.54 10.05 2 9 2 7.34 2 6 3.34 6 5c0 .35.07.69.18 1H4c-1.11 0-1.99.89-1.99 2L2 19c0 1.11.89 2 2 2h16c1.11 0 2-.89 2-2V8c0-1.11-.89-2-2-2z\"/></svg>",
        "Sales" => "<svg viewBox=\"0 0 24 24\"><path d=\"M7 18c-1.1 0-1.99.9-1.99 2S5.9 22 7 22s2-.9 2-2-.9-2-2-2zM1 2v2h2l3.6 7.59-1.35 2.45c-.16.28-.25.61-.25.96 0 1.1.9 2 2 2h12v-2H7.42c-.14 0-.25-.11-.25-.25l.03-.12.9-1.63h7.45c.75 0 1.41-.41 1.75-1.03l3.58-6.49A1 1 0 0 0 20 4H5.21l-.94-2H1zm16 16c-1.1 0-1.99.9-1.99 2s.89 2 1.99 2 2-.9 2-2-.9-2-2-2z\"/></svg>",
        "Shipping" => "<svg viewBox=\"0 0 24 24\"><path d=\"M20 8h-3V4H3c-1.1 0-2 .9-2 2v11h2c0 1.66 1.34 3 3 3s3-1.34 3-3h6c0 1.66 1.34 3 3 3s3-1.34 3-3h2v-5l-3-4z\"/></svg>",
        "Carrier" => "<svg viewBox=\"0 0 24 24\"><path d=\"M12 2L4.5 20.29l.71.71L12 18l6.79 3 .71-.71z\"/></svg>",
        "Warehouse" => "<svg viewBox=\"0 0 24 24\"><path d=\"M22 21V7L12 3 2 7v14h5v-9h10v9h5zm-11-2H9v2h2v-2zm2-3h-2v2h2v-2zm2 3h-2v2h2v-2z\"/></svg>",
        "Inventory" => "<svg viewBox=\"0 0 24 24\"><path d=\"M4 6H2v14c0 1.1.9 2 2 2h14v-2H4V6zm16-4H8c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V4c0-1.1-.9-2-2-2z\"/></svg>",
        "Finance" => "<svg viewBox=\"0 0 24 24\"><path d=\"M11.8 10.9c-2.27-.59-3-1.2-3-2.15 0-1.09 1.01-1.85 2.7-1.85 1.78 0 2.44.85 2.5 2.1h2.21c-.07-1.72-1.12-3.3-3.21-3.81V3h-3v2.16c-1.94.42-3.5 1.68-3.5 3.61 0 2.31 1.91 3.46 4.7 4.13 2.5.6 3 1.48 3 2.41 0 .69-.49 1.79-2.7 1.79-2.06 0-2.87-.92-2.98-2.1h-2.2c.12 2.19 1.76 3.42 3.68 3.83V21h3v-2.15c1.95-.37 3.5-1.5 3.5-3.55 0-2.84-2.43-3.81-4.7-4.4z\"/></svg>",
        "COD" => "<svg viewBox=\"0 0 24 24\"><path d=\"M21 18v1c0 1.1-.9 2-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h14c1.1 0 2 .9 2 2v1h-9a2 2 0 0 0-2 2v8a2 2 0 0 0 2 2h9zm-9-2h10V8H12v8z\"/></svg>",
        "Notifications" => "<svg viewBox=\"0 0 24 24\"><path d=\"M12 22c1.1 0 2-.9 2-2h-4a2 2 0 0 0 2 2zm6-6v-5c0-3.07-1.64-5.64-4.5-6.32V4c0-.83-.67-1.5-1.5-1.5s-1.5.67-1.5 1.5v.68C7.63 5.36 6 7.92 6 11v5l-2 2v1h16v-1l-2-2z\"/></svg>",
        "Reports" => "<svg viewBox=\"0 0 24 24\"><path d=\"M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zM9 17H7v-7h2v7zm4 0h-2V7h2v10zm4 0h-2v-4h2v4z\"/></svg>",
        _ => "<svg viewBox=\"0 0 24 24\"><path d=\"M19.14 12.94c.04-.3.06-.61.06-.94s-.02-.64-.07-.94l2.03-1.58a.49.49 0 0 0 .12-.61l-1.92-3.32a.49.49 0 0 0-.59-.22l-2.39.96c-.5-.38-1.03-.7-1.62-.94l-.36-2.54a.484.484 0 0 0-.48-.41h-3.84c-.24 0-.43.17-.47.41l-.36 2.54c-.59.24-1.13.57-1.62.94l-2.39-.96c-.22-.08-.47 0-.59.22L2.74 8.87c-.12.21-.08.47.12.61l2.03 1.58c-.05.3-.09.63-.09.94s.02.64.07.94l-2.03 1.58a.49.49 0 0 0-.12.61l1.92 3.32c.12.22.37.29.59.22l2.39-.96c.5.38 1.03.7 1.62.94l.36 2.54c.05.24.24.41.48.41h3.84c.24 0 .44-.17.47-.41l.36-2.54c.59-.24 1.13-.56 1.62-.94l2.39.96c.22.08.47 0 .59-.22l1.92-3.32c.12-.22.07-.47-.12-.61l-2.01-1.58zM12 15.6c-1.98 0-3.6-1.62-3.6-3.6s1.62-3.6 3.6-3.6 3.6 1.62 3.6 3.6-1.62 3.6-3.6 3.6z\"/></svg>"
    };

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _navigation.NavigationRequested -= OnExternalNavigationRequested;
            _authService.SessionExpired -= OnSessionExpired;
            AppTheme.ThemeChanged -= OnAppThemeChanged;
            _notificationTimer.Dispose();
            components?.Dispose();
        }
        base.Dispose(disposing);
    }
}
