using System.Diagnostics;
using AntdUI;
using Logistics.Client.Application.Contracts.Auth;
using Logistics.Client.Application.Interfaces;
using Logistics.Client.WinForms.Controls.Common;
using Logistics.Client.WinForms.Helpers;
using Logistics.Client.WinForms.Themes;
using Microsoft.Extensions.Logging;
using Label = System.Windows.Forms.Label;

namespace Logistics.Client.WinForms.Controls.Settings;

public sealed class SettingsWorkspaceControl : ModuleViewBase, IThemeAware
{
    private readonly IAuthSessionApiClient _authSessionApi;
    private readonly IAuthenticationService _authService;
    private readonly IClientSettingsService _settingsService;
    private readonly IHealthApiClient _healthClient;
    private readonly ILogger<SettingsWorkspaceControl> _logger;

    private readonly AntdUI.Tabs _tabs;
    private AntdUI.Panel _profileCard = null!;
    private AntdUI.TabPage _configTab = null!;
    private AntdUI.Panel _configCard = null!;
    private readonly Label _profileSummaryLabel = new() { AutoSize = true, Font = AppTheme.BodyFont, BackColor = Color.Transparent };
    private readonly PagedDataGridControl _sessionsGrid;

    private AntdUI.Select _envCombo = null!;
    private AntdUI.Input _urlBox = null!;
    private AntdUI.InputNumber _timeoutBox = null!;
    private AntdUI.Select _langCombo = null!;
    private AntdUI.Select _themeCombo = null!;

    public SettingsWorkspaceControl(
        IAuthSessionApiClient authSessionApi,
        IAuthenticationService authService,
        IClientSettingsService settingsService,
        IHealthApiClient healthClient,
        ILogger<SettingsWorkspaceControl> logger)
    {
        _authSessionApi = authSessionApi;
        _authService = authService;
        _settingsService = settingsService;
        _healthClient = healthClient;
        _logger = logger;

        Padding = new Padding(8);
        _tabs = AppTheme.CreateAntdTabs();

        // Tab 1: Profile & Security
        var profileTab = new AntdUI.TabPage { Text = "Hồ sơ & Mật khẩu", Padding = new Padding(12), AutoScroll = true };
        _profileCard = AppTheme.CreateCardPanel(new Padding(22));
        _profileCard.Dock = DockStyle.Top;
        _profileCard.Height = 210;
        _profileSummaryLabel.Dock = DockStyle.Top;
        _profileSummaryLabel.AutoSize = true;
        _profileSummaryLabel.Padding = new Padding(0, 0, 0, 16);

        var btnBar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true, BackColor = Color.Transparent, Margin = new Padding(0) };
        var changePwdBtn = AppTheme.CreatePrimaryButton("Đổi mật khẩu", 140);
        changePwdBtn.Click += async (_, _) => await ChangePasswordAsync();
        var refreshProfileBtn = AppTheme.CreateSecondaryButton("Đồng bộ quyền (/auth/me)", 180);
        refreshProfileBtn.Click += async (_, _) => await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            await _authService.RefreshUserProfileAsync();
            RenderProfile();
            UiAsyncRunner.ShowInfo(this, "Đã cập nhật hồ sơ & phân quyền mới nhất từ máy chủ.");
        }, refreshProfileBtn, logger: _logger);
        btnBar.Controls.AddRange([changePwdBtn, refreshProfileBtn]);
        _profileCard.Controls.Add(btnBar);
        _profileCard.Controls.Add(_profileSummaryLabel);
        AppTheme.BindAutoHeightCard(_profileCard, 210);
        profileTab.Controls.Add(_profileCard);

        // Tab 2: Active Sessions
        var sessionsTab = new AntdUI.TabPage { Text = "Phiên đăng nhập" };
        _sessionsGrid = new PagedDataGridControl("Phiên làm việc hiện tại", "Quản lý thiết bị đang đăng nhập vào tài khoản của bạn.", showSearch: false, showPagination: false);
        var terminateBtn = AppTheme.CreateDangerButton("Ngắt phiên chọn", 135);
        terminateBtn.Click += async (_, _) => await TerminateSelectedSessionAsync();
        var logoutAllBtn = AppTheme.CreateDangerButton("Đăng xuất tất cả", 145);
        logoutAllBtn.Click += async (_, _) =>
        {
            if (UiAsyncRunner.Confirm(this, "Đăng xuất tất cả thiết bị? Phiên hiện tại cũng sẽ đóng."))
                await _authService.LogoutAllAsync();
        };
        _sessionsGrid.AddActionButton(logoutAllBtn);
        _sessionsGrid.AddActionButton(terminateBtn);
        _sessionsGrid.ReloadRequested += async (_, _) => await LoadSessionsAsync();
        sessionsTab.Controls.Add(_sessionsGrid);

        // Tab 3: Application Config & Diagnostics
        _configTab = new AntdUI.TabPage { Text = "Cấu hình API, Giao diện & Nhật ký", Padding = new Padding(12), AutoScroll = true };
        BuildConfigTab(_configTab);

        _tabs.Pages.AddRange([profileTab, sessionsTab, _configTab]);
        Controls.Add(_tabs);
        ApplyTheme();
    }

    private void BuildConfigTab(AntdUI.TabPage tab)
    {
        _configCard = AppTheme.CreateCardPanel(new Padding(24));
        _configCard.Dock = DockStyle.Top;
        _configCard.Height = 360;

        var table = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, MaximumSize = new Size(680, 0), BackColor = Color.Transparent };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));

        _envCombo = new AntdUI.Select { Dock = DockStyle.Fill, Height = 34, Radius = 6, List = true, Font = AppTheme.BodyFont };
        _envCombo.Items.AddRange([new SelectItem("Development", "Development"), new SelectItem("Production", "Production")]);

        _urlBox = new AntdUI.Input { Dock = DockStyle.Fill, Height = 34, Radius = 6, Font = AppTheme.BodyFont };
        _timeoutBox = new AntdUI.InputNumber { Dock = DockStyle.Fill, Height = 34, Radius = 6, Minimum = 5, Maximum = 300, DecimalPlaces = 0, Value = 30, Font = AppTheme.BodyFont };

        _langCombo = new AntdUI.Select { Dock = DockStyle.Fill, Height = 34, Radius = 6, List = true, Font = AppTheme.BodyFont };
        _langCombo.Items.AddRange([new SelectItem("Tiếng Việt (vi-VN)", "vi-VN"), new SelectItem("English (en-US)", "en-US")]);

        _themeCombo = new AntdUI.Select { Dock = DockStyle.Fill, Height = 34, Radius = 6, List = true, Font = AppTheme.BodyFont };
        _themeCombo.Items.AddRange([new SelectItem("Light Theme (Sáng)", "Light"), new SelectItem("Dark Theme (Tối)", "Dark")]);

        AddRow(table, 0, "Môi trường:", _envCombo);
        AddRow(table, 1, "API Base URL:", _urlBox);
        AddRow(table, 2, "Timeout (giây):", _timeoutBox);
        AddRow(table, 3, "Ngôn ngữ giao diện:", _langCombo);
        AddRow(table, 4, "Chủ đề giao diện:", _themeCombo);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true, Padding = new Padding(0, 14, 0, 0), BackColor = Color.Transparent };
        var saveBtn = AppTheme.CreatePrimaryButton("Lưu cấu hình", 130);
        saveBtn.Click += (_, _) =>
        {
            var env = _envCombo.SelectedValue?.ToString() ?? "Development";
            var lang = _langCombo.SelectedValue?.ToString() ?? "vi-VN";
            var themeRaw = _themeCombo.SelectedValue?.ToString() ?? "Light";
            try
            {
                _settingsService.UpdateSettings(env, _urlBox.Text, (int)_timeoutBox.Value, lang);
                if (Enum.TryParse<AppThemeMode>(themeRaw, true, out var nextMode))
                    AppTheme.SetTheme(nextMode, FindForm());
                UiAsyncRunner.ShowInfo(this, "Đã lưu cấu hình API, ngôn ngữ và giao diện.");
            }
            catch (Exception ex)
            {
                UiAsyncRunner.ShowModalAlert(this, "Cấu hình không hợp lệ", ex.Message, TType.Warn);
            }
        };
        var testBtn = AppTheme.CreateSecondaryButton("Kiểm tra API", 120);
        testBtn.Click += async (_, _) => await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            var h = await _healthClient.GetHealthAsync();
            UiAsyncRunner.ShowInfo(this, h != null ? $"API {h.Status} ({h.Environment})" : "Không phản hồi.", "Trạng thái API");
        }, testBtn, logger: _logger);

        var openLogsBtn = AppTheme.CreateSecondaryButton("Mở thư mục Log", 135);
        openLogsBtn.Click += (_, _) =>
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LogisticsManagement", "logs");
            Directory.CreateDirectory(dir);
            Process.Start(new ProcessStartInfo("explorer.exe", dir) { UseShellExecute = true });
        };

        actions.Controls.AddRange([saveBtn, testBtn, openLogsBtn]);
        _configCard.Controls.Add(actions);
        _configCard.Controls.Add(table);
        AppTheme.BindAutoHeightCard(_configCard, 360);
        tab.Controls.Add(_configCard);
    }

    private static void AddRow(TableLayoutPanel t, int row, string label, Control input)
    {
        t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        t.Controls.Add(new Label { Text = label, AutoSize = true, Font = AppTheme.BodyBoldFont, Padding = new Padding(0, 8, 8, 8), BackColor = Color.Transparent }, 0, row);
        input.Margin = new Padding(0, 4, 0, 8);
        t.Controls.Add(input, 1, row);
    }

    protected override async Task RefreshDataAsync()
    {
        RenderProfile();
        var s = _settingsService.Settings;
        _envCombo.SelectedValue = s.Environment;
        _urlBox.Text = s.ApiBaseUrl;
        _timeoutBox.Value = Math.Clamp(s.TimeoutSeconds, 5, 300);
        _langCombo.SelectedValue = string.Equals(s.Culture, "en-US", StringComparison.OrdinalIgnoreCase) ? "en-US" : "vi-VN";
        _themeCombo.SelectedValue = AppTheme.CurrentMode.ToString();
        await LoadSessionsAsync();
    }

    private void RenderProfile()
    {
        var u = _authService.CurrentUser;
        if (u == null) return;
        _profileSummaryLabel.Text =
            $"Tài khoản: {u.Username} ({u.DisplayName})\n" +
            $"Email: {u.Email} | Chức danh: {u.JobTitle ?? "-"}\n" +
            $"Vai trò: {string.Join(", ", u.Roles)}\n" +
            $"Chi nhánh mặc định: {u.DefaultBranchName ?? "Không giới hạn"} | Quyền hiện có: {u.Permissions.Count} quyền";
    }

    private async Task LoadSessionsAsync()
    {
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            _sessionsGrid.SetLoading(true);
            var list = await _authSessionApi.GetSessionsAsync();
            _sessionsGrid.BindListData(list);
        }, logger: _logger, showMessageBoxOnError: false);
        _sessionsGrid.SetLoading(false);
    }

    private async Task TerminateSelectedSessionAsync()
    {
        var row = _sessionsGrid.GetSelectedRow<UserSessionDto>();
        if (row == null) return;
        if (row.IsCurrentSession)
        {
            UiAsyncRunner.ShowInfo(this, "Vui lòng dùng nút Đăng xuất để đóng phiên hiện tại.", "Lưu ý");
            return;
        }
        if (!UiAsyncRunner.Confirm(this, $"Thu hồi phiên #{row.Id} ({row.DeviceInfo})?")) return;

        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            await _authSessionApi.TerminateSessionAsync(row.Id);
            await LoadSessionsAsync();
        }, logger: _logger);
    }

    private async Task ChangePasswordAsync()
    {
        var dlg = new ModalFormDialog("Đổi mật khẩu", [
            new ModalFieldSpec { Key = "CurrentPassword", Label = "Mật khẩu hiện tại", Kind = ModalFieldKind.Password, Required = true },
            new ModalFieldSpec { Key = "NewPassword", Label = "Mật khẩu mới", Kind = ModalFieldKind.Password, Required = true }
        ], async values =>
        {
            await _authSessionApi.ChangePasswordAsync(new ChangePasswordRequest(
                values["CurrentPassword"]?.ToString() ?? string.Empty,
                values["NewPassword"]?.ToString() ?? string.Empty));
        });

        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            UiAsyncRunner.ShowInfo(this, "Đổi mật khẩu thành công. Hệ thống sẽ yêu cầu đăng nhập lại.");
            await _authService.LogoutAsync();
        }
    }

    public void ApplyTheme()
    {
        BackColor = AppTheme.SurfaceBg;
        _profileCard.Back = AppTheme.CardBg;
        _profileCard.BorderColor = AppTheme.BorderSubtle;
        _profileSummaryLabel.ForeColor = AppTheme.TextPrimary;
        _configCard.Back = AppTheme.CardBg;
        _configCard.BorderColor = AppTheme.BorderSubtle;
    }
}
