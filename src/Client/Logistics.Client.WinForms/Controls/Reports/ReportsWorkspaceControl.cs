using Logistics.Client.Application.Contracts.Reporting;
using Logistics.Client.Application.Interfaces;
using Logistics.Client.Application.Permissions;
using Logistics.Client.WinForms.Controls.Common;
using Logistics.Client.WinForms.Helpers;
using Logistics.Client.WinForms.Themes;
using Microsoft.Extensions.Logging;

namespace Logistics.Client.WinForms.Controls.Reports;

public sealed class ReportsWorkspaceControl : ModuleViewBase
{
    private readonly IReportingApiClient _reportingApi;
    private readonly IAuthorizationService _auth;
    private readonly IBranchContextService _branchContext;
    private readonly ILogger<ReportsWorkspaceControl> _logger;

    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill, Font = AppTheme.BodyFont };
    private readonly TabPage _tabExplorer = new("Báo cáo chi tiết & Xuất CSV") { BackColor = AppTheme.SurfaceBg };
    private readonly TabPage _tabScheduled = new("Lịch báo cáo & Tác vụ nền") { BackColor = AppTheme.SurfaceBg };

    private ComboBox _reportTypeCombo = null!;
    private DateTimePicker _startDatePicker = null!;
    private DateTimePicker _endDatePicker = null!;
    private CheckBox _useDatesCheck = null!;
    private CheckBox _lowStockCheck = null!;
    private PagedDataGridControl _reportGrid = null!;

    private readonly Label _healthBannerLabel = new() { Dock = DockStyle.Top, Height = 52, BackColor = AppTheme.CardBg, Padding = new Padding(14, 10, 14, 6), Font = AppTheme.BodyBoldFont };
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

        if (HasAnyReportPermission()) _tabs.TabPages.Add(_tabExplorer);
        if (_auth.HasAnyPermission(ClientPermissions.Reporting.ScheduleManage, ClientPermissions.Notification.ReportsRead, ClientPermissions.BackgroundProcessing.JobsRead))
            _tabs.TabPages.Add(_tabScheduled);

        _tabs.SelectedIndexChanged += async (_, _) => await RefreshDataAsync();
        Controls.Add(_tabs);
    }

    public void SelectSection(string navKey)
    {
        if (navKey == "Reports.Scheduled" && _tabs.TabPages.Contains(_tabScheduled))
            _tabs.SelectedTab = _tabScheduled;
        else if (_tabs.TabPages.Contains(_tabExplorer))
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

        _reportTypeCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240, Margin = new Padding(0, 4, 8, 0) };
        PopulateReportSelector();
        _reportTypeCombo.SelectedIndexChanged += (_, _) => _reportGrid.ResetAndReload();

        _useDatesCheck = new CheckBox { Text = "Lọc ngày", AutoSize = true, Checked = true, Margin = new Padding(0, 8, 4, 0) };
        _startDatePicker = new DateTimePicker { Width = 115, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30), Margin = new Padding(0, 4, 4, 0) };
        _endDatePicker = new DateTimePicker { Width = 115, Format = DateTimePickerFormat.Short, Value = DateTime.Today, Margin = new Padding(0, 4, 6, 0) };
        _useDatesCheck.CheckedChanged += (_, _) => { _startDatePicker.Enabled = _useDatesCheck.Checked; _endDatePicker.Enabled = _useDatesCheck.Checked; };

        _lowStockCheck = new CheckBox { Text = "Chỉ dưới mức tối thiểu", AutoSize = true, Margin = new Padding(4, 8, 8, 0) };
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
        _reportTypeCombo.Items.AddRange([.. items]);
        if (_reportTypeCombo.Items.Count > 0) _reportTypeCombo.SelectedIndex = 0;
    }

    private void BuildScheduledAndBackgroundTab()
    {
        var innerTabs = new TabControl { Dock = DockStyle.Fill, Font = AppTheme.BodyFont };

        var schedTab = new TabPage("Lịch báo cáo tự động (Scheduled Reports)");
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

        var jobsTab = new TabPage("Tác vụ nền (BackgroundJobs)");
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

        var outboxTab = new TabPage("Hàng đợi sự kiện (OutboxMessages)");
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

        innerTabs.TabPages.AddRange([schedTab, jobsTab, outboxTab]);
        _tabScheduled.Controls.Add(innerTabs);
        _tabScheduled.Controls.Add(_healthBannerLabel);
    }

    private ReportFilterRequest BuildCurrentFilter() => new()
    {
        StartDate = _useDatesCheck.Checked ? DateOnly.FromDateTime(_startDatePicker.Value.Date) : null,
        EndDate = _useDatesCheck.Checked ? DateOnly.FromDateTime(_endDatePicker.Value.Date) : null,
        BranchId = _branchContext.ActiveBranchId,
        Search = _reportGrid.SearchText
    };

    protected override async Task RefreshDataAsync()
    {
        if (_tabs.SelectedTab == _tabScheduled) await LoadScheduledAndBackgroundAsync();
        else await LoadExplorerAsync();
    }

    private async Task LoadExplorerAsync()
    {
        var type = (_reportTypeCombo.SelectedItem as ModalComboOption)?.Value?.ToString();
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
        var type = (_reportTypeCombo.SelectedItem as ModalComboOption)?.Value?.ToString();
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
}
