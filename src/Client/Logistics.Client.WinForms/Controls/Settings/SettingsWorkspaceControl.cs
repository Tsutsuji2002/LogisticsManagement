using System.Diagnostics;
using AntdUI;
using Logistics.Client.Application.Contracts.Auth;
using Logistics.Client.Application.Interfaces;
using Logistics.Client.Application.Permissions;
using Logistics.Client.WinForms.Controls.Common;
using Logistics.Client.WinForms.Helpers;
using Logistics.Client.WinForms.Themes;
using Logistics.Shared.Contracts.DevData;
using Microsoft.Extensions.Logging;
using Label = System.Windows.Forms.Label;

namespace Logistics.Client.WinForms.Controls.Settings;

public sealed class SettingsWorkspaceControl : ModuleViewBase, IThemeAware
{
    private readonly IAuthSessionApiClient _authSessionApi;
    private readonly IAuthenticationService _authService;
    private readonly IAuthorizationService _authorization;
    private readonly IClientSettingsService _settingsService;
    private readonly IHealthApiClient _healthClient;
    private readonly IDevTestDataApiClient _devTestDataClient;
    private readonly ILogger<SettingsWorkspaceControl> _logger;

    private readonly AntdUI.Tabs _tabs;
    private AntdUI.Panel _profileCard = null!;
    private AntdUI.TabPage _configTab = null!;
    private AntdUI.Panel _configCard = null!;
    private AntdUI.TabPage? _devSeedTab;
    private AntdUI.Panel? _devControlCard;
    private PagedDataGridControl? _devCoverageGrid;
    private readonly Label _profileSummaryLabel = new() { AutoSize = true, Font = AppTheme.BodyFont, BackColor = Color.Transparent };
    private readonly Label _devEnvSummaryLabel = new() { AutoSize = true, Font = AppTheme.BodyFont, BackColor = Color.Transparent };
    private readonly Label _devResultLabel = new() { AutoSize = true, Font = AppTheme.SmallFont, BackColor = Color.Transparent };
    private readonly PagedDataGridControl _sessionsGrid;

    private AntdUI.Select _envCombo = null!;
    private AntdUI.Input _urlBox = null!;
    private AntdUI.InputNumber _timeoutBox = null!;
    private AntdUI.Select _langCombo = null!;
    private AntdUI.Select _themeCombo = null!;

    private AntdUI.Select? _seedModeCombo;
    private AntdUI.Select? _seedScaleCombo;
    private AntdUI.InputNumber? _randomSeedBox;
    private AntdUI.Input? _modulesInput;

    public SettingsWorkspaceControl(
        IAuthSessionApiClient authSessionApi,
        IAuthenticationService authService,
        IAuthorizationService authorization,
        IClientSettingsService settingsService,
        IHealthApiClient healthClient,
        IDevTestDataApiClient devTestDataClient,
        ILogger<SettingsWorkspaceControl> logger)
    {
        _authSessionApi = authSessionApi;
        _authService = authService;
        _authorization = authorization;
        _settingsService = settingsService;
        _healthClient = healthClient;
        _devTestDataClient = devTestDataClient;
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

        // Tab 4: Development Test Data Manager (strictly visible only in Development for authorized admins)
        if (CanShowDevTestDataTab())
        {
            _devSeedTab = new AntdUI.TabPage { Text = "Dữ liệu Test (Dev)", Padding = new Padding(12) };
            BuildDevSeedTab(_devSeedTab);
            _tabs.Pages.Add(_devSeedTab);
        }

        Controls.Add(_tabs);
        ApplyTheme();
    }

    private bool CanShowDevTestDataTab()
    {
        var isDevClient = string.Equals(_settingsService.Settings.Environment, "Development", StringComparison.OrdinalIgnoreCase);
        var isAdmin = _authorization.IsInRole(ClientPermissions.Roles.SuperAdmin)
                      || _authorization.HasPermission(ClientPermissions.Configuration.SettingsManage);
        return isDevClient && isAdmin;
    }

    private void BuildDevSeedTab(AntdUI.TabPage tab)
    {
        _devControlCard = AppTheme.CreateCardPanel(new Padding(16));
        _devControlCard.Dock = DockStyle.Top;
        _devControlCard.Height = 205;

        _devEnvSummaryLabel.Dock = DockStyle.Top;
        _devEnvSummaryLabel.Text = "Đang tải trạng thái cơ sở dữ liệu phát triển...";
        _devEnvSummaryLabel.Padding = new Padding(0, 0, 0, 8);

        var controlsFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            WrapContents = true,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 4, 0, 6)
        };

        _seedModeCombo = new AntdUI.Select { Width = 195, Height = 34, Radius = 6, List = true, Font = AppTheme.BodyFont, Margin = new Padding(0, 4, 8, 4) };
        _seedModeCombo.Items.AddRange([
            new SelectItem("FullSeed (Đầy đủ)", DevSeedMode.FullSeed),
            new SelectItem("Incremental (Bổ sung)", DevSeedMode.Incremental),
            new SelectItem("ResetAndReseed (Làm mới)", DevSeedMode.ResetAndReseed),
            new SelectItem("ModuleSpecific (Theo module)", DevSeedMode.ModuleSpecific)
        ]);
        _seedModeCombo.SelectedValue = DevSeedMode.FullSeed;

        _seedScaleCombo = new AntdUI.Select { Width = 155, Height = 34, Radius = 6, List = true, Font = AppTheme.BodyFont, Margin = new Padding(0, 4, 8, 4) };
        _seedScaleCombo.Items.AddRange([
            new SelectItem("Standard (~600 đơn)", DevSeedScale.Standard),
            new SelectItem("Large (~950 đơn)", DevSeedScale.Large)
        ]);
        _seedScaleCombo.SelectedValue = DevSeedScale.Standard;

        _randomSeedBox = new AntdUI.InputNumber { Width = 95, Height = 34, Radius = 6, Minimum = 1, Maximum = 999999, DecimalPlaces = 0, Value = 42, Font = AppTheme.BodyFont, Margin = new Padding(0, 4, 8, 4) };

        _modulesInput = new AntdUI.Input
        {
            Width = 220,
            Height = 34,
            Radius = 6,
            PlaceholderText = "Modules (vd: SalesShipping,Finance,COD)",
            Font = AppTheme.BodyFont,
            Margin = new Padding(0, 4, 8, 4)
        };

        var runBtn = AppTheme.CreatePrimaryButton("Chạy Seed dữ liệu", 150);
        runBtn.Margin = new Padding(0, 4, 8, 4);
        runBtn.Click += async (_, _) => await ExecuteDevSeedAsync(runBtn);

        var verifyBtn = AppTheme.CreateSecondaryButton("Kiểm tra toàn vẹn", 145);
        verifyBtn.Margin = new Padding(0, 4, 8, 4);
        verifyBtn.Click += async (_, _) => await VerifyDevIntegrityAsync(verifyBtn);

        controlsFlow.Controls.AddRange([_seedModeCombo, _seedScaleCombo, _randomSeedBox, _modulesInput, runBtn, verifyBtn]);

        _devResultLabel.Dock = DockStyle.Top;
        _devResultLabel.Padding = new Padding(0, 4, 0, 0);
        _devResultLabel.Text = "Sẵn sàng. Lưu ý: ResetAndReseed chỉ khả dụng ở môi trường Development và có xác nhận.";

        _devControlCard.Controls.Add(_devResultLabel);
        _devControlCard.Controls.Add(controlsFlow);
        _devControlCard.Controls.Add(_devEnvSummaryLabel);
        AppTheme.BindAutoHeightCard(_devControlCard, 195);

        _devCoverageGrid = new PagedDataGridControl(
            "Ma trận dữ liệu hiện tại theo Module / Bảng",
            "Số lượng bản ghi thực tế so với mục tiêu Standard & Large.",
            showSearch: false,
            showPagination: false);
        _devCoverageGrid.Margin = new Padding(0, 10, 0, 0);
        _devCoverageGrid.ReloadRequested += async (_, _) => await LoadDevStatusAsync();

        var spacer = new System.Windows.Forms.Panel { Dock = DockStyle.Top, Height = 10, BackColor = Color.Transparent };
        tab.Controls.Add(_devCoverageGrid);
        tab.Controls.Add(spacer);
        tab.Controls.Add(_devControlCard);
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
        if (_devSeedTab != null)
            await LoadDevStatusAsync();
    }

    private async Task LoadDevStatusAsync()
    {
        if (_devCoverageGrid == null) return;

        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            _devCoverageGrid.SetLoading(true);
            var status = await _devTestDataClient.GetStatusAsync();
            var lastRun = status.LastSeededAtUtc.HasValue
                ? $"{status.LastSeededAtUtc.Value.ToLocalTime():dd/MM/yyyy HH:mm:ss} ({status.LastSeedMode}/{status.LastSeedScale})"
                : "Chưa có lịch sử chạy thủ công";

            _devEnvSummaryLabel.Text =
                $"Môi trường API: {status.EnvironmentName} (DevSeed: {(status.IsDevSeedEnabled ? "ENABLED" : "DISABLED")}) | " +
                $"Database: {status.DatabaseServer} / {status.DatabaseName}\n" +
                $"Tổng bản ghi hiện có: {status.TotalRecordsAcrossModules:N0} | Lần chạy gần nhất: {lastRun}";

            _devCoverageGrid.BindListData(status.Modules);
        }, logger: _logger, showMessageBoxOnError: false);

        _devCoverageGrid.SetLoading(false);
    }

    private async Task ExecuteDevSeedAsync(Control triggerBtn)
    {
        if (_seedModeCombo == null || _seedScaleCombo == null || _randomSeedBox == null) return;

        var mode = _seedModeCombo.SelectedValue is DevSeedMode m ? m : DevSeedMode.FullSeed;
        var scale = _seedScaleCombo.SelectedValue is DevSeedScale sc ? sc : DevSeedScale.Standard;
        var seed = (int)_randomSeedBox.Value;

        if (mode == DevSeedMode.ResetAndReseed)
        {
            if (!UiAsyncRunner.Confirm(this,
                "CẢNH BÁO: ResetAndReseed sẽ XÓA toàn bộ dữ liệu nghiệp vụ hiện tại trong database Development và tạo lại từ đầu (giữ nguyên Roles/Permissions & admin).\n\nBạn chắc chắn muốn tiếp tục?"))
            {
                return;
            }
        }

        IReadOnlyList<string>? modules = null;
        if (mode == DevSeedMode.ModuleSpecific && !string.IsNullOrWhiteSpace(_modulesInput?.Text))
        {
            modules = _modulesInput.Text
                .Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }

        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            _devResultLabel.Text = "Đang thực thi tạo dữ liệu phát triển... vui lòng đợi.";
            var res = await _devTestDataClient.ExecuteAsync(new ExecuteDevSeedRequest
            {
                Mode = mode,
                Scale = scale,
                RandomSeed = seed,
                ConfirmReset = mode == DevSeedMode.ResetAndReseed,
                Modules = modules
            });

            var integrityTxt = res.IntegrityReport?.AllPassed == true
                ? "Toàn vẹn dữ liệu: ĐẠT 100%"
                : $"Cảnh báo toàn vẹn: {res.IntegrityReport?.Checks.Count(c => !c.Passed) ?? 0} mục cần kiểm tra";
            _devResultLabel.Text = $"{res.Summary} | {integrityTxt}";
            _devCoverageGrid?.BindListData(res.UpdatedCoverage);
            await LoadDevStatusAsync();
            UiAsyncRunner.ShowInfo(this, $"{res.Summary}\n\n{integrityTxt}", "Dev Test Data");
        }, triggerBtn, logger: _logger);
    }

    private async Task VerifyDevIntegrityAsync(Control triggerBtn)
    {
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            var report = await _devTestDataClient.VerifyIntegrityAsync();
            var lines = report.Checks.Select(c =>
                $"[{(c.Passed ? "PASS" : "FAIL")}] {c.Category} - {c.CheckName}: {c.Details}");
            _devResultLabel.Text = report.AllPassed
                ? $"Kiểm tra toàn vẹn lúc {report.CheckedAtUtc.ToLocalTime():HH:mm:ss}: ĐẠT TẤT CẢ ({report.Checks.Count}/{report.Checks.Count})."
                : $"Kiểm tra toàn vẹn: Phát hiện {report.Checks.Count(c => !c.Passed)} lỗi!";
            UiAsyncRunner.ShowInfo(this, string.Join("\n", lines), "Báo cáo Toàn vẹn Dữ liệu");
        }, triggerBtn, logger: _logger);
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
        if (_devControlCard != null)
        {
            _devControlCard.Back = AppTheme.CardBg;
            _devControlCard.BorderColor = AppTheme.BorderSubtle;
            _devEnvSummaryLabel.ForeColor = AppTheme.TextPrimary;
            _devResultLabel.ForeColor = AppTheme.TextSecondary;
        }
    }
}
