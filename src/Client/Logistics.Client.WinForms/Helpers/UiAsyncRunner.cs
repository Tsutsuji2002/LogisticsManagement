using Logistics.Client.Infrastructure.Exceptions;
using Microsoft.Extensions.Logging;

namespace Logistics.Client.WinForms.Helpers;

/// <summary>
/// Centralizes async execution, duplicate-submit locking, API error handling, and user confirmation prompts.
/// </summary>
public static class UiAsyncRunner
{
    public static async Task RunSafeAsync(
        Control owner,
        Func<Task> action,
        Control? disableWhileRunning = null,
        Label? errorLabel = null,
        ILogger? logger = null,
        bool showMessageBoxOnError = true)
    {
        if (owner.IsDisposed) return;

        try
        {
            if (disableWhileRunning != null) disableWhileRunning.Enabled = false;
            if (errorLabel != null) errorLabel.Visible = false;
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
            if (errorLabel != null)
            {
                errorLabel.Text = msg;
                errorLabel.Visible = true;
            }
            if (showMessageBoxOnError && !owner.IsDisposed)
            {
                var icon = ex.IsForbidden ? MessageBoxIcon.Warning : MessageBoxIcon.Error;
                MessageBox.Show(owner, msg, "Thông báo lỗi API", MessageBoxButtons.OK, icon);
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Unhandled UI operation exception.");
            var msg = $"Đã xảy ra lỗi không mong muốn: {ex.Message}";
            if (errorLabel != null)
            {
                errorLabel.Text = msg;
                errorLabel.Visible = true;
            }
            if (showMessageBoxOnError && !owner.IsDisposed)
            {
                MessageBox.Show(owner, msg, "Lỗi ứng dụng", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        finally
        {
            if (!owner.IsDisposed)
            {
                owner.UseWaitCursor = false;
                if (disableWhileRunning != null && !disableWhileRunning.IsDisposed)
                    disableWhileRunning.Enabled = true;
            }
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
        => MessageBox.Show(owner, message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes;

    public static void ShowInfo(IWin32Window owner, string message, string title = "Thành công")
        => MessageBox.Show(owner, message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
}
