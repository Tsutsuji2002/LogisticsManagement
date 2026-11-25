using Logistics.Client.Application.Interfaces;
using Logistics.Client.Application.Localization;
using Logistics.Client.WinForms.Controls.Common;
using Logistics.Client.WinForms.Themes;

namespace Logistics.Client.WinForms.Forms.Authentication;

public sealed class LoginForm : Form
{
    private readonly IAuthenticationService _authService;
    private readonly ISecureTokenStore _tokenStore;
    private readonly IClientSettingsService _settingsService;
    private readonly IHealthApiClient _healthClient;

    private readonly TextBox _usernameBox;
    private readonly TextBox _passwordBox;
    private readonly CheckBox _showPasswordCheck;
    private readonly CheckBox _rememberUsernameCheck;
    private readonly Button _loginBtn;
    private readonly Button _settingsBtn;
    private readonly Button _testConnBtn;
    private readonly Label _errorLabel;
    private readonly Label _envLabel;
    private readonly ProgressBar _progressBar;

    public LoginForm(
        IAuthenticationService authService,
        ISecureTokenStore tokenStore,
        IClientSettingsService settingsService,
        IHealthApiClient healthClient)
    {
        _authService = authService;
        _tokenStore = tokenStore;
        _settingsService = settingsService;
        _healthClient = healthClient;

        Text = $"{AppStrings.T("LoginTitle")} - Logistics Management";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(460, 500);
        Size = new Size(480, 540);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        BackColor = AppTheme.SurfaceBg;
        Font = AppTheme.BodyFont;

        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.CardBg,
            Padding = new Padding(32, 24, 32, 20)
        };

        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 76,
            BackColor = AppTheme.NavyDark
        };
        var titleLbl = new Label
        {
            Text = AppStrings.T("AppTitle"),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(28, 16)
        };
        var subLbl = new Label
        {
            Text = AppStrings.T("LoginTitle"),
            ForeColor = Color.FromArgb(148, 163, 184),
            Font = AppTheme.BodyFont,
            AutoSize = true,
            Location = new Point(30, 44)
        };
        header.Controls.AddRange([titleLbl, subLbl]);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            AutoScroll = true
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        _envLabel = new Label
        {
            AutoSize = true,
            ForeColor = AppTheme.TextMuted,
            Font = AppTheme.SmallFont,
            Margin = new Padding(0, 0, 0, 12)
        };
        RefreshEnvSummary();

        var lblUser = new Label { Text = AppStrings.T("UsernameOrEmail"), AutoSize = true, Font = AppTheme.BodyBoldFont, ForeColor = AppTheme.TextSecondary, Margin = new Padding(0, 4, 0, 4) };
        _usernameBox = new TextBox
        {
            Dock = DockStyle.Top,
            Height = 30,
            Font = new Font("Segoe UI", 10.5F),
            Text = _tokenStore.LoadRememberedUsername() ?? string.Empty
        };

        var lblPass = new Label { Text = AppStrings.T("Password"), AutoSize = true, Font = AppTheme.BodyBoldFont, ForeColor = AppTheme.TextSecondary, Margin = new Padding(0, 12, 0, 4) };
        _passwordBox = new TextBox
        {
            Dock = DockStyle.Top,
            Height = 30,
            Font = new Font("Segoe UI", 10.5F),
            UseSystemPasswordChar = true
        };

        var checksFlow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 32, Margin = new Padding(0, 6, 0, 4) };
        _showPasswordCheck = new CheckBox { Text = AppStrings.T("ShowPassword"), AutoSize = true, Margin = new Padding(0, 4, 16, 0) };
        _showPasswordCheck.CheckedChanged += (_, _) => _passwordBox.UseSystemPasswordChar = !_showPasswordCheck.Checked;
        _rememberUsernameCheck = new CheckBox { Text = AppStrings.T("RememberUsername"), AutoSize = true, Checked = !string.IsNullOrWhiteSpace(_usernameBox.Text), Margin = new Padding(0, 4, 0, 0) };
        checksFlow.Controls.AddRange([_showPasswordCheck, _rememberUsernameCheck]);

        _errorLabel = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(390, 0),
            ForeColor = AppTheme.DangerRed,
            BackColor = Color.FromArgb(254, 242, 242),
            Padding = new Padding(10),
            Visible = false,
            Margin = new Padding(0, 8, 0, 8)
        };

        _progressBar = new ProgressBar
        {
            Dock = DockStyle.Top,
            Style = ProgressBarStyle.Marquee,
            Height = 6,
            Visible = false,
            Margin = new Padding(0, 4, 0, 8)
        };

        _loginBtn = AppTheme.CreatePrimaryButton(AppStrings.T("SignIn"), 160);
        _loginBtn.Dock = DockStyle.Top;
        _loginBtn.Height = 40;
        _loginBtn.Click += async (_, _) => await SignInAsync();

        var bottomActions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Margin = new Padding(0, 12, 0, 0) };
        _settingsBtn = AppTheme.CreateSecondaryButton("Cấu hình API", 125);
        _settingsBtn.Click += (_, _) => OpenQuickSettingsDialog();
        _testConnBtn = AppTheme.CreateSecondaryButton("Kiểm tra kết nối", 145);
        _testConnBtn.Click += async (_, _) => await TestConnectionAsync();
        bottomActions.Controls.AddRange([_settingsBtn, _testConnBtn]);

        layout.Controls.Add(_envLabel);
        layout.Controls.Add(lblUser);
        layout.Controls.Add(_usernameBox);
        layout.Controls.Add(lblPass);
        layout.Controls.Add(_passwordBox);
        layout.Controls.Add(checksFlow);
        layout.Controls.Add(_progressBar);
        layout.Controls.Add(_errorLabel);
        layout.Controls.Add(_loginBtn);
        layout.Controls.Add(bottomActions);

        card.Controls.Add(layout);
        Controls.Add(card);
        Controls.Add(header);

        AcceptButton = _loginBtn;
    }

    private void RefreshEnvSummary()
    {
        var s = _settingsService.Settings;
        var urlText = string.IsNullOrWhiteSpace(s.ApiBaseUrl) ? "(Chưa cấu hình URL)" : s.ApiBaseUrl;
        _envLabel.Text = $"Môi trường: {s.Environment} | API: {urlText}";
    }

    private async Task SignInAsync()
    {
        _errorLabel.Visible = false;
        var user = _usernameBox.Text.Trim();
        var pass = _passwordBox.Text;

        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
        {
            ShowError("Vui lòng nhập tên đăng nhập và mật khẩu.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_settingsService.Settings.ApiBaseUrl))
        {
            ShowError("API Base URL chưa được cấu hình cho môi trường hiện tại. Nhấn 'Cấu hình API' để thiết lập.");
            return;
        }

        ToggleBusy(true);
        try
        {
            var (success, err) = await _authService.LoginDetailedAsync(user, pass, _rememberUsernameCheck.Checked);
            if (success)
            {
                _passwordBox.Clear();
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            ShowError(err ?? "Đăng nhập không thành công.");
        }
        finally
        {
            if (!IsDisposed) ToggleBusy(false);
        }
    }

    private async Task TestConnectionAsync()
    {
        _errorLabel.Visible = false;
        ToggleBusy(true);
        try
        {
            var health = await _healthClient.GetHealthAsync();
            if (health != null)
            {
                MessageBox.Show(this,
                    $"Kết nối thành công!\nTrạng thái: {health.Status}\nMôi trường API: {health.Environment}\nPhiên bản: {health.Version}",
                    "Kiểm tra kết nối",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                ShowError(AppStrings.T("ApiUnreachable"));
            }
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            if (!IsDisposed) ToggleBusy(false);
        }
    }

    private void OpenQuickSettingsDialog()
    {
        var cur = _settingsService.Settings;
        var dlg = new ModalFormDialog("Cấu hình kết nối API", [
            new ModalFieldSpec
            {
                Key = "Environment", Label = "Môi trường", Kind = ModalFieldKind.ComboBox, Required = true, InitialValue = cur.Environment,
                Options = [new("Development", "Development"), new("Production", "Production")]
            },
            new ModalFieldSpec { Key = "ApiBaseUrl", Label = "API Base URL", Kind = ModalFieldKind.Text, Required = true, InitialValue = cur.ApiBaseUrl },
            new ModalFieldSpec { Key = "TimeoutSeconds", Label = "Timeout (giây)", Kind = ModalFieldKind.Integer, Required = true, InitialValue = cur.TimeoutSeconds },
            new ModalFieldSpec
            {
                Key = "Culture", Label = "Ngôn ngữ", Kind = ModalFieldKind.ComboBox, Required = true, InitialValue = cur.Culture,
                Options = [new("vi-VN", "Tiếng Việt (vi-VN)"), new("en-US", "English (en-US)")]
            }
        ]);

        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            try
            {
                _settingsService.UpdateSettings(
                    dlg.GetString("Environment"),
                    dlg.GetString("ApiBaseUrl"),
                    dlg.GetInt("TimeoutSeconds"),
                    dlg.GetString("Culture"));
                RefreshEnvSummary();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }
    }

    private void ToggleBusy(bool busy)
    {
        _loginBtn.Enabled = !busy;
        _settingsBtn.Enabled = !busy;
        _testConnBtn.Enabled = !busy;
        _loginBtn.Text = busy ? AppStrings.T("SigningIn") : AppStrings.T("SignIn");
        _progressBar.Visible = busy;
        UseWaitCursor = busy;
    }

    private void ShowError(string message)
    {
        _errorLabel.Text = message;
        _errorLabel.Visible = true;
    }
}
