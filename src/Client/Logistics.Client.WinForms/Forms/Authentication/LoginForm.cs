using AntdUI;
using Logistics.Client.Application.Interfaces;
using Logistics.Client.Application.Localization;
using Logistics.Client.WinForms.Controls.Common;
using Logistics.Client.WinForms.Helpers;
using Logistics.Client.WinForms.Themes;
using Microsoft.Extensions.Logging;
using Label = System.Windows.Forms.Label;

namespace Logistics.Client.WinForms.Forms.Authentication;

/// <summary>
/// AntdUI Login form supporting credentials sign-in, quick API environment settings,
/// live health connectivity check, Light/Dark theme switching, and DPAPI username recall.
/// </summary>
public sealed class LoginForm : AntdUI.Window, IThemeAware
{
    private readonly IAuthenticationService _authService;
    private readonly ISecureTokenStore _tokenStore;
    private readonly IClientSettingsService _settingsService;
    private readonly IHealthApiClient _healthApi;
    private readonly ILogger<LoginForm> _logger;

    private readonly AntdUI.PageHeader _windowBar;
    private readonly System.Windows.Forms.Panel _heroPanel;
    private readonly System.Windows.Forms.Panel _formHost;
    private readonly AntdUI.Panel _loginCard;
    private readonly AntdUI.Input _usernameInput;
    private readonly AntdUI.Input _passwordInput;
    private readonly AntdUI.Checkbox _showPasswordCheck;
    private readonly AntdUI.Checkbox _rememberCheck;
    private readonly AntdUI.Button _signInBtn;
    private readonly AntdUI.Button _testApiBtn;
    private readonly AntdUI.Button _settingsBtn;
    private readonly AntdUI.Button _themeBtn;
    private readonly AntdUI.Alert _statusAlert;
    private readonly Label _welcomeTitle;
    private readonly Label _welcomeSub;
    private readonly Label _userLabel;
    private readonly Label _passLabel;
    private readonly Label _apiFootLabel;

    public LoginForm(
        IAuthenticationService authService,
        ISecureTokenStore tokenStore,
        IClientSettingsService settingsService,
        IHealthApiClient healthApi,
        ILogger<LoginForm> logger)
    {
        _authService = authService;
        _tokenStore = tokenStore;
        _settingsService = settingsService;
        _healthApi = healthApi;
        _logger = logger;

        Text = "Logistics ERP — Đăng nhập hệ thống";
        Size = new Size(940, 580);
        MinimumSize = new Size(640, 480);
        MaximumSize = new Size(1160, 760);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.None;
        Resizable = true;
        MaximizeBox = false;
        Mode = AppTheme.IsDark ? TAMode.Dark : TAMode.Light;

        _windowBar = new AntdUI.PageHeader
        {
            Dock = DockStyle.Top,
            Height = 44,
            Text = "Logistics Management Desktop",
            SubText = "Enterprise TMS • WMS • Finance • COD",
            Font = AppTheme.BodyBoldFont,
            DragMove = true,
            ShowButton = true,
            MaximizeBox = false,
            MinimizeBox = true,
            CancelButton = true,
            DividerShow = true,
            DividerThickness = 1F
        };

        // Left Enterprise Hero Brand Panel
        _heroPanel = new System.Windows.Forms.Panel
        {
            Dock = DockStyle.Left,
            Width = 350,
            BackColor = AppTheme.SidebarBg,
            Padding = new Padding(28, 34, 24, 24)
        };

        var brandBadge = new AntdUI.Tag
        {
            Text = "LOGISTICS ERP v2",
            Type = TTypeMini.Primary,
            Radius = 6,
            Location = new Point(28, 34),
            AutoSize = true,
            Font = AppTheme.SmallFont
        };

        var heroTitle = new Label
        {
            Text = "Điều hành Logistics\nToàn diện & Tập trung",
            Font = new Font("Segoe UI", 17F, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(28, 76),
            Size = new Size(295, 72),
            BackColor = Color.Transparent
        };

        var heroDesc = new Label
        {
            Text =
                "• Quản trị Tổ chức, CRM, Sản phẩm\n" +
                "• Quy trình Đơn hàng -> Vận đơn -> 3PL\n" +
                "• Kho bãi WMS, Tồn kho Lot/Serial\n" +
                "• Tài chính, Công nợ & Đối soát COD\n" +
                "• Bảo mật JWT, DPAPI & Phân quyền Chi nhánh",
            Font = AppTheme.BodyFont,
            ForeColor = Color.FromArgb(203, 213, 225),
            Location = new Point(28, 166),
            Size = new Size(295, 195),
            BackColor = Color.Transparent
        };

        _apiFootLabel = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            Font = AppTheme.SmallFont,
            ForeColor = Color.FromArgb(148, 163, 184),
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.BottomLeft
        };

        _heroPanel.Controls.Add(_apiFootLabel);
        _heroPanel.Controls.Add(heroDesc);
        _heroPanel.Controls.Add(heroTitle);
        _heroPanel.Controls.Add(brandBadge);

        // Right Login Form Surface
        _formHost = new System.Windows.Forms.Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(28, 18, 28, 20)
        };

        var topTools = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(0, 38),
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = true,
            BackColor = Color.Transparent
        };

        Resize += (_, _) =>
        {
            var showHero = ClientSize.Width >= 760;
            if (_heroPanel.Visible != showHero)
                _heroPanel.Visible = showHero;

            var padX = ClientSize.Width < 760 ? 18 : 28;
            if (_formHost.Padding.Left != padX)
                _formHost.Padding = new Padding(padX, 18, padX, 20);
        };

        _themeBtn = AppTheme.CreateSecondaryButton(AppTheme.IsDark ? "☀ Sáng" : "🌙 Tối", 94);
        _themeBtn.Click += (_, _) =>
        {
            AppTheme.ToggleTheme(this);
            _themeBtn.Text = AppTheme.IsDark ? "☀ Sáng" : "🌙 Tối";
        };

        _settingsBtn = AppTheme.CreateSecondaryButton("⚙ Cấu hình API", 132);
        _settingsBtn.Click += async (_, _) => await OpenQuickSettingsAsync();

        _testApiBtn = AppTheme.CreateSecondaryButton("Kiểm tra kết nối", 134);
        _testApiBtn.Click += async (_, _) => await TestApiHealthAsync();

        topTools.Controls.Add(_themeBtn);
        topTools.Controls.Add(_settingsBtn);
        topTools.Controls.Add(_testApiBtn);

        _loginCard = AppTheme.CreateCardPanel(new Padding(28, 22, 28, 22), radius: 10);
        _loginCard.Dock = DockStyle.Fill;
        _loginCard.Margin = new Padding(0, 12, 0, 0);

        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 9,
            BackColor = Color.Transparent
        };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 8; i++)
            stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        stack.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _welcomeTitle = new Label
        {
            Text = AppStrings.T("Login"),
            Font = AppTheme.PageTitleFont,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 4),
            BackColor = Color.Transparent
        };

        _welcomeSub = new Label
        {
            Text = "Đăng nhập bằng tài khoản nhân viên hoặc quản trị viên được cấp quyền.",
            Font = AppTheme.SmallFont,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 16),
            BackColor = Color.Transparent
        };

        _userLabel = new Label
        {
            Text = AppStrings.T("Username"),
            Font = AppTheme.BodyBoldFont,
            AutoSize = true,
            Margin = new Padding(0, 4, 0, 4),
            BackColor = Color.Transparent
        };

        _usernameInput = new AntdUI.Input
        {
            Dock = DockStyle.Top,
            Height = 38,
            Radius = 6,
            AllowClear = true,
            PlaceholderText = "Ví dụ: admin hoặc email nhân viên",
            Font = AppTheme.BodyFont,
            Margin = new Padding(0, 0, 0, 10)
        };

        _passLabel = new Label
        {
            Text = AppStrings.T("Password"),
            Font = AppTheme.BodyBoldFont,
            AutoSize = true,
            Margin = new Padding(0, 4, 0, 4),
            BackColor = Color.Transparent
        };

        _passwordInput = new AntdUI.Input
        {
            Dock = DockStyle.Top,
            Height = 38,
            Radius = 6,
            UseSystemPasswordChar = true,
            PlaceholderText = "Nhập mật khẩu...",
            Font = AppTheme.BodyFont,
            Margin = new Padding(0, 0, 0, 8)
        };
        _passwordInput.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                await ExecuteLoginAsync();
            }
        };

        var optRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 32,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 2, 0, 10)
        };

        _rememberCheck = new AntdUI.Checkbox
        {
            Text = AppStrings.T("RememberUsername"),
            AutoSize = true,
            Font = AppTheme.BodyFont,
            Margin = new Padding(0, 4, 18, 0)
        };

        _showPasswordCheck = new AntdUI.Checkbox
        {
            Text = "Hiện mật khẩu",
            AutoSize = true,
            Font = AppTheme.BodyFont,
            Margin = new Padding(0, 4, 0, 0)
        };
        _showPasswordCheck.CheckedChanged += (_, e) => _passwordInput.UseSystemPasswordChar = !e.Value;

        optRow.Controls.Add(_rememberCheck);
        optRow.Controls.Add(_showPasswordCheck);

        _statusAlert = new AntdUI.Alert
        {
            Dock = DockStyle.Top,
            Height = 46,
            Radius = 6,
            Visible = false,
            Font = AppTheme.SmallFont,
            Margin = new Padding(0, 0, 0, 10)
        };

        _signInBtn = AppTheme.CreatePrimaryButton(AppStrings.T("Login"), 260);
        _signInBtn.Dock = DockStyle.Top;
        _signInBtn.Height = 40;
        _signInBtn.Click += async (_, _) => await ExecuteLoginAsync();

        stack.Controls.Add(_welcomeTitle, 0, 0);
        stack.Controls.Add(_welcomeSub, 0, 1);
        stack.Controls.Add(_userLabel, 0, 2);
        stack.Controls.Add(_usernameInput, 0, 3);
        stack.Controls.Add(_passLabel, 0, 4);
        stack.Controls.Add(_passwordInput, 0, 5);
        stack.Controls.Add(optRow, 0, 6);
        stack.Controls.Add(_statusAlert, 0, 7);
        stack.Controls.Add(_signInBtn, 0, 8);

        _loginCard.Controls.Add(stack);

        _formHost.Controls.Add(_loginCard);
        _formHost.Controls.Add(topTools);

        Controls.Add(_formHost);
        Controls.Add(_heroPanel);
        Controls.Add(_windowBar);

        LoadSavedUsernameAndSettings();
        ApplyTheme();
    }

    private void LoadSavedUsernameAndSettings()
    {
        var remembered = _tokenStore.LoadRememberedUsername();
        if (!string.IsNullOrWhiteSpace(remembered))
        {
            _usernameInput.Text = remembered;
            _rememberCheck.Checked = true;
            _passwordInput.Focus();
        }
        RefreshApiFooter();
    }

    private void RefreshApiFooter()
    {
        var s = _settingsService.Settings;
        _apiFootLabel.Text = $"Môi trường: {s.Environment} | Timeout: {s.TimeoutSeconds}s\nAPI: {s.ApiBaseUrl}";
    }

    private async Task ExecuteLoginAsync()
    {
        var username = _usernameInput.Text.Trim();
        var password = _passwordInput.Text;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            ShowAlert("Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.", TType.Warn);
            return;
        }

        _signInBtn.Loading = true;
        _signInBtn.Enabled = false;
        _statusAlert.Visible = false;
        UseWaitCursor = true;

        try
        {
            var (success, error) = await _authService.LoginDetailedAsync(username, password, _rememberCheck.Checked);
            if (!success)
            {
                ShowAlert(error ?? "Đăng nhập thất bại. Vui lòng kiểm tra thông tin tài khoản.", TType.Error);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed unexpectedly.");
            ShowAlert($"Không thể đăng nhập: {ex.Message}", TType.Error);
        }
        finally
        {
            if (!IsDisposed)
            {
                UseWaitCursor = false;
                _signInBtn.Loading = false;
                _signInBtn.Enabled = true;
            }
        }
    }

    private async Task TestApiHealthAsync()
    {
        _testApiBtn.Loading = true;
        _testApiBtn.Enabled = false;
        try
        {
            var health = await _healthApi.GetHealthAsync();
            if (health != null)
                ShowAlert($"Kết nối API thành công ({health.Status} - {health.Environment ?? _settingsService.Settings.Environment}).", TType.Success);
            else
                ShowAlert("API phản hồi trống.", TType.Warn);
        }
        catch (Exception ex)
        {
            ShowAlert($"Không thể kết nối API {_settingsService.Settings.ApiBaseUrl}: {ex.Message}", TType.Error);
        }
        finally
        {
            if (!IsDisposed)
            {
                _testApiBtn.Loading = false;
                _testApiBtn.Enabled = true;
            }
        }
    }

    private Task OpenQuickSettingsAsync()
    {
        var cur = _settingsService.Settings;
        var dlg = new ModalFormDialog("Cấu hình API & Giao diện", [
            new ModalFieldSpec
            {
                Key = "Environment", Label = "Môi trường", Kind = ModalFieldKind.ComboBox, Required = true,
                InitialValue = cur.Environment,
                Options = _settingsService.AvailableEnvironments.Select(e => new ModalComboOption(e, e)).ToList()
            },
            new ModalFieldSpec
            {
                Key = "ApiBaseUrl", Label = "API Base URL", Required = true, InitialValue = cur.ApiBaseUrl,
                Placeholder = "http://localhost:5289"
            },
            new ModalFieldSpec
            {
                Key = "TimeoutSeconds", Label = "Timeout (giây)", Kind = ModalFieldKind.Integer, Required = true,
                InitialValue = cur.TimeoutSeconds, MinValue = 5, MaxValue = 300
            },
            new ModalFieldSpec
            {
                Key = "Culture", Label = "Ngôn ngữ", Kind = ModalFieldKind.ComboBox, Required = true,
                InitialValue = cur.Culture,
                Options = [new("vi-VN", "Tiếng Việt (vi-VN)"), new("en-US", "English (en-US)")]
            },
            new ModalFieldSpec
            {
                Key = "Theme", Label = "Giao diện", Kind = ModalFieldKind.ComboBox, Required = true,
                InitialValue = AppTheme.CurrentMode.ToString(),
                Options = [new("Light", "Light (Sáng)"), new("Dark", "Dark (Tối)")]
            }
        ], v =>
        {
            var env = v["Environment"]!.ToString()!;
            var url = v["ApiBaseUrl"]!.ToString()!;
            var timeout = Convert.ToInt32(v["TimeoutSeconds"]);
            var culture = v["Culture"]!.ToString()!;
            _settingsService.UpdateSettings(env, url, timeout, culture);

            if (Enum.TryParse<AppThemeMode>(v["Theme"]?.ToString(), true, out var themeMode))
                AppTheme.SetTheme(themeMode, this);

            return Task.CompletedTask;
        }, "Lưu cấu hình");

        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            RefreshApiFooter();
            _themeBtn.Text = AppTheme.IsDark ? "☀ Sáng" : "🌙 Tối";
            ShowAlert("Đã cập nhật cấu hình kết nối & giao diện.", TType.Success);
        }

        return Task.CompletedTask;
    }

    private void ShowAlert(string text, TType icon)
    {
        _statusAlert.Text = text;
        _statusAlert.Icon = icon;
        _statusAlert.Visible = true;
    }

    public void ApplyTheme()
    {
        Mode = AppTheme.IsDark ? TAMode.Dark : TAMode.Light;
        BackColor = AppTheme.SurfaceBg;
        ForeColor = AppTheme.TextPrimary;
        _windowBar.BackColor = AppTheme.HeaderBg;
        _windowBar.ForeColor = AppTheme.TextPrimary;
        _windowBar.DividerColor = AppTheme.BorderSubtle;
        _formHost.BackColor = AppTheme.SurfaceBg;
        _welcomeTitle.ForeColor = AppTheme.TextPrimary;
        _welcomeSub.ForeColor = AppTheme.TextSecondary;
        _userLabel.ForeColor = AppTheme.TextPrimary;
        _passLabel.ForeColor = AppTheme.TextPrimary;
        _rememberCheck.ForeColor = AppTheme.TextPrimary;
        _showPasswordCheck.ForeColor = AppTheme.TextPrimary;
        _loginCard.Back = AppTheme.CardBg;
        _loginCard.BorderColor = AppTheme.BorderSubtle;
        _loginCard.Invalidate();
    }
}
