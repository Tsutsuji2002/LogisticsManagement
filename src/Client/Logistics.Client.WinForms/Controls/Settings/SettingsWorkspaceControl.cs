using System.Diagnostics;
using Logistics.Client.Application.Contracts.Auth;
using Logistics.Client.Application.Interfaces;
using Logistics.Client.WinForms.Controls.Common;
using Logistics.Client.WinForms.Helpers;
using Logistics.Client.WinForms.Themes;
using Microsoft.Extensions.Logging;

namespace Logistics.Client.WinForms.Controls.Settings;

public sealed class SettingsWorkspaceControl : ModuleViewBase
{
    private readonly IAuthSessionApiClient _authSessionApi;
    private readonly IAuthenticationService _authService;
    private readonly IClientSettingsService _settingsService;
    private readonly IHealthApiClient _healthClient;
    private readonly ILogger<SettingsWorkspaceControl> _logger;

    private readonly TabControl _tabs;
    private readonly Label _profileSummaryLabel = new() { AutoSize = true, Font = AppTheme.BodyFont };
    private readonly PagedDataGridControl _sessionsGrid;

    private ComboBox _envCombo = null!;
    private TextBox _urlBox = null!;
    private NumericUpDown _timeoutBox = null!;
    private ComboBox _langCombo = null!;

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

        Padding = new Padding(16);
        _tabs = new TabControl { Dock = DockStyle.Fill, Font = AppTheme.BodyFont };

        // Tab 1: Profile & Security
        var profileTab = new TabPage("Hồ sơ & Mật khẩu") { BackColor = AppTheme.SurfaceBg, Padding = new Padding(16) };
        var profileCard = new Panel { Dock = DockStyle.Top, Height = 260, BackColor = AppTheme.CardBg, Padding = new Padding(20) };
        _profileSummaryLabel.Location = new Point(20, 20);
        var btnBar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44 };
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
        profileCard.Controls.Add(_profileSummaryLabel);
        profileCard.Controls.Add(btnBar);
        profileTab.Controls.Add(profileCard);

        // Tab 2: Active Sessions
        var sessionsTab = new TabPage("Phiên đăng nhập") { BackColor = AppTheme.SurfaceBg };
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
        var configTab = new TabPage("Cấu hình API & Nhật ký") { BackColor = AppTheme.CardBg, Padding = new Padding(24) };
        BuildConfigTab(configTab);

        _tabs.TabPages.AddRange([profileTab, sessionsTab, configTab]);
        Controls.Add(_tabs);
    }

    private void BuildConfigTab(TabPage tab)
    {
        var table = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, MaximumSize = new Size(640, 0) };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));

        _envCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _envCombo.Items.AddRange(["Development", "Production"]);
        _urlBox = new TextBox { Dock = DockStyle.Fill };
        _timeoutBox = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 5, Maximum = 300, Value = 30 };
        _langCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _langCombo.Items.AddRange([new ModalComboOption("vi-VN", "Tiếng Việt (vi-VN)"), new ModalComboOption("en-US", "English (en-US)")]);

        AddRow(table, 0, "Môi trường:", _envCombo);
        AddRow(table, 1, "API Base URL:", _urlBox);
        AddRow(table, 2, "Timeout (giây):", _timeoutBox);
        AddRow(table, 3, "Ngôn ngữ giao diện:", _langCombo);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 52, Padding = new Padding(0, 12, 0, 0) };
        var saveBtn = AppTheme.CreatePrimaryButton("Lưu cấu hình", 130);
        saveBtn.Click += (_, _) =>
        {
            var env = _envCombo.SelectedItem?.ToString() ?? "Development";
            var lang = (_langCombo.SelectedItem as ModalComboOption)?.Value?.ToString() ?? "vi-VN";
            try
            {
                _settingsService.UpdateSettings(env, _urlBox.Text, (int)_timeoutBox.Value, lang);
                UiAsyncRunner.ShowInfo(this, "Đã lưu cấu hình API và ngôn ngữ.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Cấu hình không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
        tab.Controls.Add(actions);
        tab.Controls.Add(table);
    }

    private static void AddRow(TableLayoutPanel t, int row, string label, Control input)
    {
        t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        t.Controls.Add(new Label { Text = label, AutoSize = true, Font = AppTheme.BodyBoldFont, Padding = new Padding(0, 6, 8, 8) }, 0, row);
        input.Margin = new Padding(0, 4, 0, 8);
        t.Controls.Add(input, 1, row);
    }

    protected override async Task RefreshDataAsync()
    {
        RenderProfile();
        var s = _settingsService.Settings;
        _envCombo.SelectedItem = s.Environment;
        _urlBox.Text = s.ApiBaseUrl;
        _timeoutBox.Value = Math.Clamp(s.TimeoutSeconds, 5, 300);
        _langCombo.SelectedIndex = string.Equals(s.Culture, "en-US", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
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
}
