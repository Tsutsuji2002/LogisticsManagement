using AntdUI;
using Logistics.Client.Application.Contracts.Reporting;
using Logistics.Client.Application.Interfaces;
using Logistics.Client.Application.Permissions;
using Logistics.Client.WinForms.Controls.Common;
using Logistics.Client.WinForms.Helpers;
using Logistics.Client.WinForms.Themes;
using Microsoft.Extensions.Logging;

namespace Logistics.Client.WinForms.Controls.Reports;

public sealed class ReportsWorkspaceControl : ModuleViewBase, IThemeAware
{
    private readonly IReportingApiClient _reportingApi;
    private readonly IAuthorizationService _auth;
    private readonly IBranchContextService _branchContext;
    private readonly ILogger<ReportsWorkspaceControl> _logger;

    private readonly AntdUI.Tabs _tabs = AppTheme.CreateAntdTabs();
    private readonly AntdUI.TabPage _tabExplorer = new() { Text = "Báo cáo chi tiết & Xuất CSV" };
    private readonly AntdUI.TabPage _tabScheduled = new() { Text = "Lịch báo cáo & Tác vụ nền" };

    private AntdUI.Select _reportTypeCombo = null!;
    private AntdUI.DatePicker _startDatePicker = null!;
    private AntdUI.DatePicker _endDatePicker = null!;
    private AntdUI.Checkbox _useDatesCheck = null!;
    private AntdUI.Checkbox _lowStockCheck = null!;
    private PagedDataGridControl _reportGrid = null!;

    private readonly AntdUI.Alert _healthBannerLabel = new()
    {
        Dock = DockStyle.Top,
        Height = 46,
        Radius = 6,
        Icon = TType.Info,
        Font = AppTheme.BodyBoldFont,
        Margin = new Padding(0, 0, 0, 8)
    };
    private PagedDataGridControl _scheduledGrid = null!;
    private PagedDataGridControl _jobsGrid = null!;
    private PagedDataGridControl _outboxGrid = null!;

    public ReportsWorkspaceControl(
        IReportingApiClient reportingApi,
        IAuthorizationService auth,
        IBranchContextService branchContext,
        ILogger<ReportsWorkspaceControl> logger)
    {
        _reportingApi = reportingApi;
        _auth = auth;
        _branchContext = branchContext;
        _logger = logger;

        BuildExplorerTab();
        BuildScheduledAndBackgroundTab();

        if (HasAnyReportPermission()) _tabs.Pages.Add(_tabExplorer);
        if (_auth.HasAnyPermission(ClientPermissions.Reporting.ScheduleManage, ClientPermissions.Notification.ReportsRead, ClientPermissions.BackgroundProcessing.JobsRead))
            _tabs.Pages.Add(_tabScheduled);

        _tabs.SelectedIndexChanged += async (_, _) => await RefreshDataAsync();
        Controls.Add(_tabs);
        ApplyTheme();
    }

    public void SelectSection(string navKey)
    {
        if (navKey == "Reports.Scheduled" && _tabs.Pages.Contains(_tabScheduled))
            _tabs.SelectedTab = _tabScheduled;
        else if (_tabs.Pages.Contains(_tabExplorer))
            _tabs.SelectedTab = _tabExplorer;
    }

    private bool HasAnyReportPermission()
        => _auth.HasAnyPermission(
            ClientPermissions.Reporting.OrdersRead, ClientPermissions.Reporting.ShipmentsRead, ClientPermissions.Reporting.DeliveryRead,
            ClientPermissions.Reporting.CarrierRead, ClientPermissions.Reporting.WarehouseRead, ClientPermissions.Reporting.InventoryRead,
            ClientPermissions.Reporting.FinanceRead, ClientPermissions.Reporting.CodRead);

    private void BuildExplorerTab()
    {
        _reportGrid = new PagedDataGridControl("Kho báo cáo điều hành", "Dữ liệu lọc & phân trang tại Server. Hỗ trợ xuất CSV trực tiếp từ API /reports/export.");

        _reportTypeCombo = new AntdUI.Select { Width = 250, Height = 32, Radius = 6, List = true, Font = AppTheme.BodyFont, Margin = new Padding(0, 0, 8, 4) };
        PopulateReportSelector();
        _reportTypeCombo.SelectedIndexChanged += (_, _) => _reportGrid.ResetAndReload();

        _useDatesCheck = new AntdUI.Checkbox { Text = "Lọc ngày", AutoSize = true, Checked = true, Margin = new Padding(0, 6, 4, 0), Font = AppTheme.BodyFont };
        _startDatePicker = new AntdUI.DatePicker { Width = 132, Height = 32, Radius = 6, Format = "dd/MM/yyyy", Value = DateTime.Today.AddDays(-30), Font = AppTheme.BodyFont, Margin = new Padding(0, 0, 4, 4) };
        _endDatePicker = new AntdUI.DatePicker { Width = 132, Height = 32, Radius = 6, Format = "dd/MM/yyyy", Value = DateTime.Today, Font = AppTheme.BodyFont, Margin = new Padding(0, 0, 6, 4) };
        _useDatesCheck.CheckedChanged += (_, e) => { _startDatePicker.Enabled = e.Value; _endDatePicker.Enabled = e.Value; };

        _lowStockCheck = new AntdUI.Checkbox { Text = "Chỉ dưới mức tối thiểu", AutoSize = true, Margin = new Padding(4, 6, 8, 0), Font = AppTheme.BodyFont };
        _lowStockCheck.CheckedChanged += (_, _) => _reportGrid.ResetAndReload();

        _reportGrid.AddFilterControl(_reportTypeCombo);
        _reportGrid.AddFilterControl(_useDatesCheck);
        _reportGrid.AddFilterControl(_startDatePicker);
        _reportGrid.AddFilterControl(_endDatePicker);
        _reportGrid.AddFilterControl(_lowStockCheck);

        // NOTE: Backend ONLY supports ?format=csv; never show unsupported Excel/PDF buttons.
        if (_auth.HasPermission(ClientPermissions.Reporting.Export))
        {
            var exportCsvBtn = AppTheme.CreateSuccessButton("Xuất CSV", 110);
            exportCsvBtn.Click += async (_, _) => await ExportSelectedReportCsvAsync(exportCsvBtn);
            _reportGrid.AddActionButton(exportCsvBtn);
        }

        _reportGrid.ReloadRequested += async (_, _) => await LoadExplorerAsync();
        _tabExplorer.Controls.Add(_reportGrid);
    }

    private void PopulateReportSelector()
    {
        var items = new List<ModalComboOption>();
        if (_auth.HasPermission(ClientPermissions.Reporting.OrdersRead)) items.Add(new("orders", "Báo cáo Đơn hàng (Orders)"));
        if (_auth.HasPermission(ClientPermissions.Reporting.ShipmentsRead)) items.Add(new("shipments", "Báo cáo Vận đơn (Shipments)"));
        if (_auth.HasPermission(ClientPermissions.Reporting.DeliveryRead)) items.Add(new("delivery-performance", "Hiệu suất giao hàng & SLA"));
        if (_auth.HasPermission(ClientPermissions.Reporting.CarrierRead)) items.Add(new("carrier-performance", "Hiệu suất ĐVVC (Carriers)"));
        if (_auth.HasPermission(ClientPermissions.Reporting.WarehouseRead))
        {
            items.Add(new("warehouse-receipts", "Báo cáo Nhập kho"));
            items.Add(new("warehouse-dispatches", "Báo cáo Xuất kho"));
            items.Add(new("parcel-handling", "Xử lý bưu kiện Hub"));
        }
        if (_auth.HasPermission(ClientPermissions.Reporting.InventoryRead))
        {
            items.Add(new("inventory-balances", "Báo cáo Tồn kho"));
            items.Add(new("inventory-transactions", "Giao dịch tồn kho"));
        }
        if (_auth.HasPermission(ClientPermissions.Reporting.FinanceRead))
        {
            items.Add(new("shipping-charges", "Cước phí vận đơn"));
            items.Add(new("invoices", "Báo cáo Hóa đơn"));
            items.Add(new("payments", "Báo cáo Thanh toán"));
            items.Add(new("receivables", "Công nợ phải thu (Receivables)"));
        }
        if (_auth.HasPermission(ClientPermissions.Reporting.CodRead))
        {
            items.Add(new("cod-collections", "Thu hộ COD"));
            items.Add(new("cod-remittances", "Hoàn trả COD"));
            items.Add(new("cod-reconciliation", "Đối soát COD theo chi nhánh"));
        }

        _reportTypeCombo.Items.Clear();
        foreach (var opt in items)
            _reportTypeCombo.Items.Add(new SelectItem(opt.Display, opt.Value));
        if (_reportTypeCombo.Items.Count > 0) _reportTypeCombo.SelectedIndex = 0;
    }

    private void BuildScheduledAndBackgroundTab()
    {
        var innerTabs = AppTheme.CreateAntdTabs();

        var schedTab = new AntdUI.TabPage { Text = "Lịch báo cáo tự động (Scheduled Reports)" };
        _scheduledGrid = new PagedDataGridControl("Lịch gửi báo cáo", "Quản lý lịch Cron chạy báo cáo và kích hoạt chạy thủ công.", showSearch: false);
        if (_auth.HasPermission(ClientPermissions.Reporting.ScheduleManage) || _auth.HasPermission(ClientPermissions.Notification.ReportsCreate))
        {
            var createBtn = AppTheme.CreatePrimaryButton("Tạo lịch mới", 120);
            createBtn.Click += async (_, _) => await CreateScheduledReportAsync();
            var runBtn = AppTheme.CreateSuccessButton("Chạy ngay", 105);
            runBtn.Click += async (_, _) => await RunScheduledReportAsync();
            _scheduledGrid.AddActionButton(createBtn);
            _scheduledGrid.AddActionButton(runBtn);
        }
        _scheduledGrid.ReloadRequested += async (_, _) => await LoadScheduledAndBackgroundAsync();
        schedTab.Controls.Add(_scheduledGrid);

        var jobsTab = new AntdUI.TabPage { Text = "Tác vụ nền (BackgroundJobs)" };
        _jobsGrid = new PagedDataGridControl("Nhật ký BackgroundJobLogs", "Giám sát tình trạng tác vụ chạy nền và thử lại khi lỗi.", showSearch: false);
        if (_auth.HasPermission(ClientPermissions.BackgroundProcessing.JobsRetry))
        {
            var retryBtn = AppTheme.CreatePrimaryButton("Thử lại Job", 110);
            retryBtn.Click += async (_, _) =>
            {
                var j = _jobsGrid.GetSelectedRow<BackgroundJobLogDto>();
                if (j == null) return;
                await UiAsyncRunner.RunSafeAsync(this, async () =>
                {
                    await _reportingApi.RetryBackgroundJobAsync(j.Id);
                    await LoadScheduledAndBackgroundAsync();
                }, retryBtn, null, _logger);
            };
            _jobsGrid.AddActionButton(retryBtn);
        }
        _jobsGrid.ReloadRequested += async (_, _) => await LoadScheduledAndBackgroundAsync();
        jobsTab.Controls.Add(_jobsGrid);

        var outboxTab = new AntdUI.TabPage { Text = "Hàng đợi sự kiện (OutboxMessages)" };
        _outboxGrid = new PagedDataGridControl("OutboxMessages", "Theo dõi sự kiện tích hợp đang chờ hoặc thất bại.", showSearch: false);
        if (_auth.HasPermission(ClientPermissions.BackgroundProcessing.OperationsManage))
        {
            var retryOutbox = AppTheme.CreatePrimaryButton("Thử lại Outbox", 125);
            retryOutbox.Click += async (_, _) =>
            {
                var msg = _outboxGrid.GetSelectedRow<OutboxMessageDto>();
                if (msg == null) return;
                await UiAsyncRunner.RunSafeAsync(this, async () =>
                {
                    await _reportingApi.RetryOutboxMessageAsync(msg.Id);
                    await LoadScheduledAndBackgroundAsync();
                }, retryOutbox, null, _logger);
            };
            _outboxGrid.AddActionButton(retryOutbox);
        }
        _outboxGrid.ReloadRequested += async (_, _) => await LoadScheduledAndBackgroundAsync();
        outboxTab.Controls.Add(_outboxGrid);

        innerTabs.Pages.AddRange([schedTab, jobsTab, outboxTab]);
        _tabScheduled.Controls.Add(innerTabs);
        _tabScheduled.Controls.Add(_healthBannerLabel);
    }

    private ReportFilterRequest BuildCurrentFilter() => new()
    {
        StartDate = _useDatesCheck.Checked ? DateOnly.FromDateTime((_startDatePicker.Value ?? DateTime.Today.AddDays(-30)).Date) : null,
        EndDate = _useDatesCheck.Checked ? DateOnly.FromDateTime((_endDatePicker.Value ?? DateTime.Today).Date) : null,
        BranchId = _branchContext.ActiveBranchId,
        Search = _reportGrid.SearchText
    };

    private string? GetSelectedReportType()
    {
        if (_reportTypeCombo.SelectedIndex >= 0 &&
            _reportTypeCombo.SelectedIndex < _reportTypeCombo.Items.Count &&
            _reportTypeCombo.Items[_reportTypeCombo.SelectedIndex] is SelectItem item)
        {
            return item.Tag?.ToString();
        }
        return _reportTypeCombo.SelectedValue?.ToString();
    }

    protected override async Task RefreshDataAsync()
    {
        if (_tabs.SelectedTab == _tabScheduled) await LoadScheduledAndBackgroundAsync();
        else await LoadExplorerAsync();
    }

    private async Task LoadExplorerAsync()
    {
        var type = GetSelectedReportType();
        if (string.IsNullOrWhiteSpace(type)) return;

        _reportGrid.SetLoading(true);
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            var filter = BuildCurrentFilter();
            var page = _reportGrid.CurrentPage;
            var size = _reportGrid.CurrentPageSize;

            switch (type)
            {
                case "orders": _reportGrid.BindPagedData(await _reportingApi.GetOrderReportAsync(filter, page, size)); break;
                case "shipments": _reportGrid.BindPagedData(await _reportingApi.GetShipmentReportAsync(filter, page, size)); break;
                case "delivery-performance": _reportGrid.BindPagedData(await _reportingApi.GetDeliveryPerformanceReportAsync(filter, page, size)); break;
                case "carrier-performance": _reportGrid.BindPagedData(await _reportingApi.GetCarrierPerformanceReportAsync(filter, page, size)); break;
                case "warehouse-receipts": _reportGrid.BindPagedData(await _reportingApi.GetWarehouseReceiptsReportAsync(filter, page, size)); break;
                case "warehouse-dispatches": _reportGrid.BindPagedData(await _reportingApi.GetWarehouseDispatchesReportAsync(filter, page, size)); break;
                case "parcel-handling": _reportGrid.BindPagedData(await _reportingApi.GetParcelHandlingReportAsync(filter, page, size)); break;
                case "inventory-balances": _reportGrid.BindPagedData(await _reportingApi.GetInventoryBalancesReportAsync(filter, _lowStockCheck.Checked, page, size)); break;
                case "inventory-transactions": _reportGrid.BindPagedData(await _reportingApi.GetInventoryTransactionsReportAsync(filter, page, size)); break;
                case "shipping-charges": _reportGrid.BindPagedData(await _reportingApi.GetShippingChargesReportAsync(filter, null, page, size)); break;
                case "invoices": _reportGrid.BindPagedData(await _reportingApi.GetInvoicesReportAsync(filter, page, size)); break;
                case "payments": _reportGrid.BindPagedData(await _reportingApi.GetPaymentsReportAsync(filter, page, size)); break;
                case "receivables": _reportGrid.BindPagedData(await _reportingApi.GetReceivablesReportAsync(filter, page, size)); break;
                case "cod-collections": _reportGrid.BindPagedData(await _reportingApi.GetCodCollectionsReportAsync(filter, page, size)); break;
                case "cod-remittances": _reportGrid.BindPagedData(await _reportingApi.GetCodRemittancesReportAsync(filter, page, size)); break;
                case "cod-reconciliation": _reportGrid.BindPagedData(await _reportingApi.GetCodReconciliationReportAsync(filter, page, size)); break;
            }
        }, null, null, _logger);
        _reportGrid.SetLoading(false);
    }

    private async Task ExportSelectedReportCsvAsync(Control triggerBtn)
    {
        var type = GetSelectedReportType();
        if (string.IsNullOrWhiteSpace(type)) return;

        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            var file = await _reportingApi.ExportCsvAsync(type, BuildCurrentFilter());
            using var sfd = new SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",
                FileName = string.IsNullOrWhiteSpace(file.FileName) ? $"{type}-{DateTime.Now:yyyyMMdd}.csv" : file.FileName
            };
            if (sfd.ShowDialog(this) == DialogResult.OK)
            {
                await File.WriteAllBytesAsync(sfd.FileName, file.Content);
                UiAsyncRunner.ShowInfo(this, $"Đã xuất báo cáo CSV tới:\n{sfd.FileName}", "Xuất CSV thành công");
            }
        }, triggerBtn, null, _logger);
    }

    private async Task LoadScheduledAndBackgroundAsync()
    {
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            if (_auth.HasAnyPermission(ClientPermissions.BackgroundProcessing.JobsRead, ClientPermissions.BackgroundProcessing.OperationsRead))
            {
                var h = await _reportingApi.GetBackgroundHealthAsync();
                _healthBannerLabel.Text =
                    $"Tình trạng Worker: {h.Status}  |  Outbox chờ: {h.PendingOutboxCount} (Lỗi: {h.FailedOutboxCount})  |  Jobs chờ: {h.PendingJobsCount} (Lỗi: {h.FailedJobsCount})  |  Lịch báo cáo đến hạn: {h.DueScheduledReportsCount}";
                _jobsGrid.BindPagedData(await _reportingApi.GetBackgroundJobsAsync(null, _jobsGrid.CurrentPage, _jobsGrid.CurrentPageSize));
                _outboxGrid.BindPagedData(await _reportingApi.GetOutboxMessagesAsync(null, _outboxGrid.CurrentPage, _outboxGrid.CurrentPageSize));
            }
            if (_auth.HasAnyPermission(ClientPermissions.Reporting.ScheduleManage, ClientPermissions.Notification.ReportsRead))
            {
                _scheduledGrid.BindPagedData(await _reportingApi.GetScheduledReportsAsync(_branchContext.ActiveBranchId, _scheduledGrid.CurrentPage, _scheduledGrid.CurrentPageSize));
            }
        }, null, null, _logger);
    }

    private async Task CreateScheduledReportAsync()
    {
        var defs = await _reportingApi.GetReportDefinitionsAsync(1, 100);
        var dlg = new ModalFormDialog("Tạo lịch gửi báo cáo tự động",
        [
            new() { Key = "DefId", Label = "Định nghĩa báo cáo", Kind = ModalFieldKind.ComboBox, Required = true, Options = defs.Items.Select(d => new ModalComboOption(d.Id, $"{d.Code} - {d.Name}")).ToList() },
            new() { Key = "Cron", Label = "Biểu thức Cron", Required = true, InitialValue = "0 8 * * 1" },
            new() { Key = "Format", Label = "Định dạng xuất", Kind = ModalFieldKind.ComboBox, InitialValue = "CSV", Options = [new("CSV", "CSV")] },
            new() { Key = "Emails", Label = "Email nhận (cách nhau dấu phẩy)", Required = true }
        ],
        async vals => await _reportingApi.CreateScheduledReportAsync(new CreateScheduledReportRequest(
            Convert.ToInt64(vals["DefId"]),
            _branchContext.ActiveBranchId,
            vals["Cron"]?.ToString() ?? "0 8 * * 1",
            "SE Asia Standard Time",
            "CSV",
            vals["Emails"]?.ToString() ?? string.Empty)));
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadScheduledAndBackgroundAsync();
    }

    private async Task RunScheduledReportAsync()
    {
        var row = _scheduledGrid.GetSelectedRow<ScheduledReportDto>();
        if (row == null) return;
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            var res = await _reportingApi.RunScheduledReportAsync(row.Id);
            UiAsyncRunner.ShowInfo(this, $"Trạng thái: {res.Status}\nThông điệp: {res.Message}", "Kết quả chạy lịch báo cáo");
            await LoadScheduledAndBackgroundAsync();
        }, null, null, _logger);
    }

    public void ApplyTheme()
    {
        BackColor = AppTheme.SurfaceBg;
        _useDatesCheck.ForeColor = AppTheme.TextPrimary;
        _lowStockCheck.ForeColor = AppTheme.TextPrimary;
    }
}
