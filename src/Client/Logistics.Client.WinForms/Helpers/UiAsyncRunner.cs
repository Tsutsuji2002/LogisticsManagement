using AntdUI;
using Logistics.Client.Infrastructure.Exceptions;
using Logistics.Client.WinForms.Themes;
using Microsoft.Extensions.Logging;

namespace Logistics.Client.WinForms.Helpers;

/// <summary>
/// Centralizes async execution, duplicate-submit locking, AntdUI error banners/modals, and toast notifications.
/// </summary>
public static class UiAsyncRunner
{
    public static async Task RunSafeAsync(
        Control owner,
        Func<Task> action,
        Control? disableWhileRunning = null,
        Control? errorBanner = null,
        ILogger? logger = null,
        bool showMessageBoxOnError = true)
    {
        if (owner.IsDisposed) return;

        try
        {
            SetControlBusy(disableWhileRunning, true);
            HideErrorBanner(errorBanner);
            owner.UseWaitCursor = true;

            await action();
        }
        catch (OperationCanceledException)
        {
            // Deliberate cancellation on form close or filter change
        }
        catch (ApiException ex)
        {
            logger?.LogWarning("API error ({StatusCode}, {ErrorCode}): {Message}", (int)ex.StatusCode, ex.ErrorCode, ex.Message);
            var msg = FormatApiErrorMessage(ex);
            ShowErrorBanner(errorBanner, msg);

            if (showMessageBoxOnError && !owner.IsDisposed)
            {
                var icon = ex.IsForbidden ? TType.Warn : TType.Error;
                ShowModalAlert(owner, "Thông báo lỗi API", msg, icon);
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Unhandled UI operation exception.");
            var msg = $"Đã xảy ra lỗi không mong muốn: {ex.Message}";
            ShowErrorBanner(errorBanner, msg);

            if (showMessageBoxOnError && !owner.IsDisposed)
            {
                ShowModalAlert(owner, "Lỗi ứng dụng", msg, TType.Error);
            }
        }
        finally
        {
            if (!owner.IsDisposed)
            {
                owner.UseWaitCursor = false;
                SetControlBusy(disableWhileRunning, false);
            }
        }
    }

    private static void SetControlBusy(Control? ctrl, bool busy)
    {
        if (ctrl == null || ctrl.IsDisposed) return;

        if (ctrl is AntdUI.Button antBtn)
        {
            antBtn.Loading = busy;
            antBtn.Enabled = !busy;
        }
        else
        {
            ctrl.Enabled = !busy;
        }
    }

    private static void HideErrorBanner(Control? banner)
    {
        if (banner == null || banner.IsDisposed) return;
        banner.Visible = false;
    }

    private static void ShowErrorBanner(Control? banner, string msg)
    {
        if (banner == null || banner.IsDisposed) return;
        if (banner is Alert alert)
        {
            alert.Text = msg;
            alert.Icon = TType.Error;
            alert.Visible = true;
        }
        else
        {
            banner.Text = msg;
            banner.Visible = true;
        }
    }

    public static string FormatApiErrorMessage(ApiException ex)
    {
        if (ex.ValidationErrors is { Count: > 0 })
        {
            var details = string.Join("\n• ", ex.ValidationErrors.SelectMany(kv => kv.Value.Select(v => $"{kv.Key}: {v}")));
            return $"{ex.Message}\n• {details}";
        }

        if (ex.Errors is { Count: > 0 })
        {
            return $"{ex.Message}\n• {string.Join("\n• ", ex.Errors)}";
        }

        return ex.Message;
    }

    public static bool Confirm(IWin32Window owner, string message, string title = "Xác nhận thao tác")
    {
        var form = ResolveForm(owner);
        if (form != null)
        {
            var cfg = new Modal.Config(form, title, message, TType.Warn)
            {
                OkText = "Xác nhận",
                CancelText = "Hủy",
                OkType = TTypeMini.Primary,
                ColorScheme = AppTheme.IsDark ? TAMode.Dark : TAMode.Light,
                Width = 440
            };
            return Modal.open(cfg) == DialogResult.OK;
        }

        return MessageBox.Show(owner, message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
    }

    public static void ShowInfo(IWin32Window owner, string message, string title = "Thành công")
    {
        var form = ResolveForm(owner);
        if (form != null)
        {
            var cfg = new Modal.Config(form, title, message, TType.Success)
            {
                OkText = "Đóng",
                CancelText = string.Empty,
                OkType = TTypeMini.Primary,
                ColorScheme = AppTheme.IsDark ? TAMode.Dark : TAMode.Light,
                Width = 440
            };
            Modal.open(cfg);
            return;
        }

        MessageBox.Show(owner, message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    public static void ShowModalAlert(IWin32Window owner, string title, string message, TType icon)
    {
        var form = ResolveForm(owner);
        if (form != null)
        {
            var cfg = new Modal.Config(form, title, message, icon)
            {
                OkText = "Đóng",
                CancelText = string.Empty,
                OkType = icon == TType.Error ? TTypeMini.Error : TTypeMini.Primary,
                ColorScheme = AppTheme.IsDark ? TAMode.Dark : TAMode.Light,
                Width = 480
            };
            Modal.open(cfg);
            return;
        }

        var mbIcon = icon switch
        {
            TType.Warn => MessageBoxIcon.Warning,
            TType.Error => MessageBoxIcon.Error,
            _ => MessageBoxIcon.Information
        };
        MessageBox.Show(owner, message, title, MessageBoxButtons.OK, mbIcon);
    }

    public static void ShowToastSuccess(Control owner, string text)
    {
        var form = owner.FindForm();
        if (form != null && !form.IsDisposed)
            AntdUI.Message.success(form, text, AppTheme.BodyFont, 3);
    }

    public static void ShowToastInfo(Control owner, string text)
    {
        var form = owner.FindForm();
        if (form != null && !form.IsDisposed)
            AntdUI.Message.info(form, text, AppTheme.BodyFont, 3);
    }

    private static Form? ResolveForm(IWin32Window owner)
    {
        if (owner is Form f && !f.IsDisposed) return f;
        if (owner is Control c && !c.IsDisposed) return c.FindForm();
        return Form.ActiveForm;
    }
}
