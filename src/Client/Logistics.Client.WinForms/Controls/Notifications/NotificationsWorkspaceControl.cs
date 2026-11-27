using AntdUI;
using Logistics.Client.Application.Contracts.Notification;
using Logistics.Client.Application.Interfaces;
using Logistics.Client.Application.Permissions;
using Logistics.Client.WinForms.Controls.Common;
using Logistics.Client.WinForms.Helpers;
using Logistics.Client.WinForms.Themes;
using Microsoft.Extensions.Logging;
using Label = System.Windows.Forms.Label;

namespace Logistics.Client.WinForms.Controls.Notifications;

public sealed class NotificationsWorkspaceControl : ModuleViewBase, IThemeAware
{
    private readonly INotificationApiClient _notifApi;
    private readonly IAuthorizationService _auth;
    private readonly IBranchContextService _branchContext;
    private readonly ILogger<NotificationsWorkspaceControl> _logger;

    private readonly AntdUI.Tabs _tabs = AppTheme.CreateAntdTabs();
    private readonly AntdUI.TabPage _tabCenter = new() { Text = "Trung tâm thông báo" };
    private readonly AntdUI.TabPage _tabTemplates = new() { Text = "Mẫu thông báo (Templates)" };

    private PagedDataGridControl _notifGrid = null!;
    private AntdUI.Checkbox _unreadOnlyCheck = null!;
    private AntdUI.Checkbox? _systemAllCheck;
    private AntdUI.Panel _previewCard = null!;
    private Label _previewLabel = null!;

    private PagedDataGridControl _templatesGrid = null!;

    public event EventHandler? UnreadCountChanged;

    public NotificationsWorkspaceControl(
        INotificationApiClient notifApi,
        IAuthorizationService auth,
        IBranchContextService branchContext,
        ILogger<NotificationsWorkspaceControl> logger)
    {
        _notifApi = notifApi;
        _auth = auth;
        _branchContext = branchContext;
        _logger = logger;

        BuildCenterTab();
        BuildTemplatesTab();

        if (_auth.HasPermission(ClientPermissions.Notification.NotificationsReadOwn)) _tabs.Pages.Add(_tabCenter);
        if (_auth.HasPermission(ClientPermissions.Notification.TemplatesRead)) _tabs.Pages.Add(_tabTemplates);

        _tabs.SelectedIndexChanged += async (_, _) => await RefreshDataAsync();
        Controls.Add(_tabs);
        ApplyTheme();
    }

    public void SelectSection(string navKey)
    {
        if (navKey == "Notifications.Templates" && _tabs.Pages.Contains(_tabTemplates))
            _tabs.SelectedTab = _tabTemplates;
        else if (_tabs.Pages.Contains(_tabCenter))
            _tabs.SelectedTab = _tabCenter;
    }

    private void BuildCenterTab()
    {
        var split = AppTheme.CreateSafeSplitContainer(Orientation.Horizontal, 360);
        _notifGrid = new PagedDataGridControl("Thông báo (Notifications)", "Danh sách thông báo trong hệ thống (tự làm mới chu kỳ 60 giây trên thanh tiêu đề).", showSearch: false);

        _unreadOnlyCheck = new AntdUI.Checkbox { Text = "Chỉ chưa đọc", AutoSize = true, Margin = new Padding(4, 6, 8, 0), Font = AppTheme.BodyFont };
        _unreadOnlyCheck.CheckedChanged += (_, _) => _notifGrid.ResetAndReload();
        _notifGrid.AddFilterControl(_unreadOnlyCheck);

        if (_auth.HasPermission(ClientPermissions.Notification.NotificationsManage))
        {
            _systemAllCheck = new AntdUI.Checkbox { Text = "Xem toàn hệ thống", AutoSize = true, Margin = new Padding(4, 6, 8, 0), Font = AppTheme.BodyFont };
            _systemAllCheck.CheckedChanged += (_, _) => _notifGrid.ResetAndReload();
            _notifGrid.AddFilterControl(_systemAllCheck);

            var sendBtn = AppTheme.CreatePrimaryButton("Gửi thông báo", 125);
            sendBtn.Click += async (_, _) => await SendNotificationAsync();
            _notifGrid.AddActionButton(sendBtn);
        }

        var readBtn = AppTheme.CreateSecondaryButton("Đánh dấu đã đọc", 130);
        readBtn.Click += async (_, _) =>
        {
            var n = _notifGrid.GetSelectedRow<NotificationDto>();
            if (n == null) return;
            await UiAsyncRunner.RunSafeAsync(this, async () =>
            {
                await _notifApi.MarkAsReadAsync(n.Id);
                await LoadNotificationsAsync();
                UnreadCountChanged?.Invoke(this, EventArgs.Empty);
            }, readBtn, null, _logger);
        };

        var readAllBtn = AppTheme.CreateSuccessButton("Đọc tất cả", 105);
        readAllBtn.Click += async (_, _) =>
        {
            await UiAsyncRunner.RunSafeAsync(this, async () =>
            {
                await _notifApi.MarkAllReadAsync();
                await LoadNotificationsAsync();
                UnreadCountChanged?.Invoke(this, EventArgs.Empty);
            }, readAllBtn, null, _logger);
        };

        _notifGrid.AddActionButton(readBtn);
        _notifGrid.AddActionButton(readAllBtn);
        _notifGrid.ReloadRequested += async (_, _) => await LoadNotificationsAsync();
        _notifGrid.SelectionChanged += (_, _) =>
        {
            var n = _notifGrid.GetSelectedRow<NotificationDto>();
            _previewLabel.Text = n == null ? "Chọn thông báo để xem chi tiết." : $"{n.Title}\n[{n.Channel} | {n.Priority} | {AppTheme.FormatLocalTime(n.CreatedAt)}]\n\n{n.Content}";
        };

        _previewCard = AppTheme.CreateCardPanel(new Padding(16));
        _previewCard.Dock = DockStyle.Fill;
        _previewLabel = new Label { Dock = DockStyle.Fill, BackColor = Color.Transparent, Font = AppTheme.BodyFont };
        _previewCard.Controls.Add(_previewLabel);
        split.Panel1.Controls.Add(_notifGrid);
        split.Panel2.Controls.Add(_previewCard);
        _tabCenter.Controls.Add(split);
    }

    private void BuildTemplatesTab()
    {
        _templatesGrid = new PagedDataGridControl("Mẫu thông báo (NotificationTemplates)", "Cấu hình nội dung mẫu email/trong ứng dụng theo mã template.");
        if (_auth.HasPermission(ClientPermissions.Notification.TemplatesManage))
        {
            var addBtn = AppTheme.CreatePrimaryButton("Tạo mẫu mới", 120);
            addBtn.Click += async (_, _) => await CreateOrEditTemplateAsync(null);
            var editBtn = AppTheme.CreateSecondaryButton("Sửa mẫu", 95);
            editBtn.Click += async (_, _) =>
            {
                var t = _templatesGrid.GetSelectedRow<NotificationTemplateDto>();
                if (t != null) await CreateOrEditTemplateAsync(t);
            };
            var togBtn = AppTheme.CreateDangerButton("Bật/Tắt", 90);
            togBtn.Click += async (_, _) =>
            {
                var t = _templatesGrid.GetSelectedRow<NotificationTemplateDto>();
                if (t == null) return;
                await UiAsyncRunner.RunSafeAsync(this, async () =>
                {
                    await _notifApi.ToggleTemplateStatusAsync(t.Id, new UpdateTemplateStatusRequest { IsActive = !t.IsActive });
                    await LoadTemplatesAsync();
                }, togBtn, null, _logger);
            };
            _templatesGrid.AddActionButton(addBtn);
            _templatesGrid.AddActionButton(editBtn);
            _templatesGrid.AddActionButton(togBtn);
        }
        _templatesGrid.ReloadRequested += async (_, _) => await LoadTemplatesAsync();
        _tabTemplates.Controls.Add(_templatesGrid);
    }

    protected override async Task RefreshDataAsync()
    {
        if (_tabs.SelectedTab == _tabTemplates) await LoadTemplatesAsync();
        else await LoadNotificationsAsync();
    }

    private async Task LoadNotificationsAsync()
    {
        _notifGrid.SetLoading(true);
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            var isReadFilter = _unreadOnlyCheck.Checked ? (bool?)false : null;
            var res = _systemAllCheck?.Checked == true
                ? await _notifApi.GetAllNotificationsAsync(_branchContext.ActiveBranchId, isReadFilter, _notifGrid.CurrentPage, _notifGrid.CurrentPageSize)
                : await _notifApi.GetMyNotificationsAsync(isReadFilter, _notifGrid.CurrentPage, _notifGrid.CurrentPageSize);
            _notifGrid.BindPagedData(res);
        }, null, null, _logger);
        _notifGrid.SetLoading(false);
    }

    private async Task LoadTemplatesAsync()
    {
        _templatesGrid.SetLoading(true);
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            var res = await _notifApi.GetTemplatesAsync(_templatesGrid.SearchText, null, _templatesGrid.CurrentPage, _templatesGrid.CurrentPageSize);
            _templatesGrid.BindPagedData(res);
        }, null, null, _logger);
        _templatesGrid.SetLoading(false);
    }

    private async Task SendNotificationAsync()
    {
        var dlg = new ModalFormDialog("Gửi thông báo mới",
        [
            new() { Key = "Broadcast", Label = "Phát toàn hệ thống", Kind = ModalFieldKind.CheckBox, InitialValue = true },
            new() { Key = "Channel", Label = "Kênh gửi", Kind = ModalFieldKind.ComboBox, InitialValue = "InApp", Options = [new("InApp", "InApp"), new("Email", "Email")] },
            new() { Key = "Priority", Label = "Mức ưu tiên", Kind = ModalFieldKind.ComboBox, InitialValue = "Normal", Options = [new("Low", "Low"), new("Normal", "Normal"), new("High", "High"), new("Urgent", "Urgent")] },
            new() { Key = "Title", Label = "Tiêu đề", Required = true },
            new() { Key = "Content", Label = "Nội dung", Kind = ModalFieldKind.Multiline, Required = true }
        ],
        async vals =>
        {
            await _notifApi.SendNotificationAsync(new CreateNotificationRequest
            {
                IsSystemBroadcast = vals["Broadcast"] is true,
                BranchId = _branchContext.ActiveBranchId,
                Channel = vals["Channel"]?.ToString() ?? "InApp",
                Priority = vals["Priority"]?.ToString() ?? "Normal",
                Title = vals["Title"]?.ToString(),
                Content = vals["Content"]?.ToString()
            });
        });
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            await LoadNotificationsAsync();
            UnreadCountChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private async Task CreateOrEditTemplateAsync(NotificationTemplateDto? existing)
    {
        var dlg = new ModalFormDialog(existing == null ? "Tạo mẫu thông báo" : $"Sửa mẫu {existing.Code}",
        [
            new() { Key = "Code", Label = "Mã mẫu", Required = existing == null, ReadOnly = existing != null, InitialValue = existing?.Code },
            new() { Key = "Name", Label = "Tên mẫu", Required = true, InitialValue = existing?.Name },
            new() { Key = "Channel", Label = "Kênh", Kind = ModalFieldKind.ComboBox, InitialValue = existing?.Channel ?? "InApp", Options = [new("InApp", "InApp"), new("Email", "Email")] },
            new() { Key = "Subject", Label = "Tiêu đề mẫu", InitialValue = existing?.SubjectTemplate },
            new() { Key = "Body", Label = "Nội dung mẫu", Kind = ModalFieldKind.Multiline, Required = true, InitialValue = existing?.BodyTemplate }
        ],
        async vals =>
        {
            if (existing == null)
            {
                await _notifApi.CreateTemplateAsync(new CreateNotificationTemplateRequest
                {
                    Code = vals["Code"]?.ToString() ?? string.Empty,
                    Name = vals["Name"]?.ToString() ?? string.Empty,
                    Channel = vals["Channel"]?.ToString() ?? "InApp",
                    SubjectTemplate = vals["Subject"]?.ToString(),
                    BodyTemplate = vals["Body"]?.ToString() ?? string.Empty
                });
            }
            else
            {
                await _notifApi.UpdateTemplateAsync(existing.Id, new UpdateNotificationTemplateRequest
                {
                    Name = vals["Name"]?.ToString() ?? string.Empty,
                    Channel = vals["Channel"]?.ToString() ?? "InApp",
                    SubjectTemplate = vals["Subject"]?.ToString(),
                    BodyTemplate = vals["Body"]?.ToString() ?? string.Empty
                });
            }
        });
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadTemplatesAsync();
    }

    public void ApplyTheme()
    {
        BackColor = AppTheme.SurfaceBg;
        _unreadOnlyCheck.ForeColor = AppTheme.TextPrimary;
        if (_systemAllCheck != null) _systemAllCheck.ForeColor = AppTheme.TextPrimary;
        _previewCard.Back = AppTheme.CardBg;
        _previewCard.BorderColor = AppTheme.BorderSubtle;
        _previewLabel.ForeColor = AppTheme.TextPrimary;
    }
}
