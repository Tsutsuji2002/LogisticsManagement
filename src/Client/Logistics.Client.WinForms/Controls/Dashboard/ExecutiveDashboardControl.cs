using AntdUI;
using Logistics.Client.Application.Contracts.Reporting;
using Logistics.Client.Application.Interfaces;
using Logistics.Client.Application.Permissions;
using Logistics.Client.WinForms.Controls.Common;
using Logistics.Client.WinForms.Helpers;
using Logistics.Client.WinForms.Themes;
using Microsoft.Extensions.Logging;
using Label = System.Windows.Forms.Label;

namespace Logistics.Client.WinForms.Controls.Dashboard;

public sealed class ExecutiveDashboardControl : ModuleViewBase, IThemeAware
{
    private readonly IReportingApiClient _reportingClient;
    private readonly IAuthorizationService _auth;
    private readonly IBranchContextService _branchContext;
    private readonly ILogger<ExecutiveDashboardControl> _logger;

    private readonly AntdUI.Panel _topBar;
    private readonly AntdUI.DatePicker _fromPicker;
    private readonly AntdUI.DatePicker _toPicker;
    private readonly AntdUI.Checkbox _useDateFilter;
    private readonly AntdUI.Select _dashboardScopeCombo;
    private readonly AntdUI.Button _refreshBtn;
    private readonly Label _scopeInfoLabel;
    private readonly TableLayoutPanel _kpiGrid;
    private readonly TableLayoutPanel _chartsGrid;
    private int _currentKpiCols = -1;
    private int _currentChartCols = -1;

    private readonly KpiCardControl _cardOrders = new("Đơn hàng (Orders)", "0", "Trong kỳ lọc", AppTheme.PrimaryBlue);
    private readonly KpiCardControl _cardShipments = new("Vận đơn (Shipments)", "0", "Đang giao / Tổng", AppTheme.SuccessEmerald);
    private readonly KpiCardControl _cardInventory = new("Tồn kho khả dụng", "0", "SKU còn hàng", AppTheme.PurpleAccent);
    private readonly KpiCardControl _cardFinance = new("Công nợ / Đã thu", "0", "Khách hàng", AppTheme.WarningAmber);
    private readonly KpiCardControl _cardCod = new("Thu hộ COD", "0", "Tồn quỹ tài xế", AppTheme.DangerRed);
    private readonly KpiCardControl _cardWarehouses = new("Phiếu kho chờ xử lý", "0", "Nhập & Xuất kho", AppTheme.NavyDark);

    private readonly SimpleBarChartControl _leftChart = new("Phân bổ vận đơn / đơn hàng");
    private readonly SimpleBarChartControl _rightChart = new("Hiệu suất & Hoạt động chi nhánh / kho");
    private readonly AntdUI.Alert _notesLabel = new()
    {
        Dock = DockStyle.Top,
        Height = 40,
        Radius = 6,
        Icon = TType.Info,
        Font = AppTheme.SmallFont,
        Margin = new Padding(0, 4, 0, 6),
        Text = "Dữ liệu tổng hợp trực tiếp từ Backend Reporting API."
    };

    public ExecutiveDashboardControl(
        IReportingApiClient reportingClient,
        IAuthorizationService auth,
        IBranchContextService branchContext,
        ILogger<ExecutiveDashboardControl> logger)
    {
        _reportingClient = reportingClient;
        _auth = auth;
        _branchContext = branchContext;
        _logger = logger;

        Padding = new Padding(10);
        AutoScroll = true;

        _topBar = AppTheme.CreateCardPanel(new Padding(14, 10, 14, 10));
        _topBar.Dock = DockStyle.Top;
        _topBar.Height = 56;
        _topBar.Margin = new Padding(0, 0, 0, 10);

        var filterFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };

        _dashboardScopeCombo = new AntdUI.Select
        {
            Width = 265,
            Height = 34,
            Radius = 6,
            List = true,
            Font = AppTheme.BodyFont,
            Margin = new Padding(0, 2, 12, 0)
        };
        PopulateScopes();
        _dashboardScopeCombo.SelectedIndexChanged += async (_, _) => await RefreshDataAsync();

        _useDateFilter = new AntdUI.Checkbox
        {
            Text = "Lọc theo ngày:",
            AutoSize = true,
            Checked = false,
            Font = AppTheme.BodyFont,
            Margin = new Padding(0, 6, 8, 0)
        };
        _fromPicker = new AntdUI.DatePicker
        {
            Format = "dd/MM/yyyy",
            Width = 136,
            Height = 34,
            Radius = 6,
            Value = DateTime.Today.AddDays(-30),
            Enabled = false,
            Font = AppTheme.BodyFont,
            Margin = new Padding(0, 2, 6, 0)
        };
        _toPicker = new AntdUI.DatePicker
        {
            Format = "dd/MM/yyyy",
            Width = 136,
            Height = 34,
            Radius = 6,
            Value = DateTime.Today,
            Enabled = false,
            Font = AppTheme.BodyFont,
            Margin = new Padding(0, 2, 12, 0)
        };
        _useDateFilter.CheckedChanged += (_, e) =>
        {
            _fromPicker.Enabled = e.Value;
            _toPicker.Enabled = e.Value;
        };

        _refreshBtn = AppTheme.CreatePrimaryButton("Làm mới KPI", 120);
        _refreshBtn.Click += async (_, _) => await RefreshDataAsync();

        _scopeInfoLabel = new Label
        {
            AutoSize = true,
            Font = AppTheme.SmallFont,
            ForeColor = AppTheme.TextMuted,
            BackColor = Color.Transparent,
            Padding = new Padding(8, 10, 0, 0)
        };

        filterFlow.Controls.AddRange([_dashboardScopeCombo, _useDateFilter, _fromPicker, _toPicker, _refreshBtn, _scopeInfoLabel]);
        _topBar.Controls.Add(filterFlow);
        AppTheme.BindAutoHeightCard(_topBar, 56);

        _kpiGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 3,
            RowCount = 2,
            Margin = new Padding(0, 10, 0, 6),
            Padding = new Padding(0, 10, 0, 4),
            BackColor = Color.Transparent
        };
        foreach (var card in new[] { _cardOrders, _cardShipments, _cardInventory, _cardFinance, _cardCod, _cardWarehouses })
        {
            card.Dock = DockStyle.Fill;
            card.Margin = new Padding(0, 0, 10, 10);
            card.MinimumSize = new Size(190, 108);
        }

        _chartsGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 360,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(0, 8, 0, 0),
            BackColor = Color.Transparent
        };
        _leftChart.Dock = DockStyle.Fill;
        _rightChart.Dock = DockStyle.Fill;

        Controls.Add(_chartsGrid);
        Controls.Add(_notesLabel);
        Controls.Add(_kpiGrid);
        Controls.Add(_topBar);

        SizeChanged += (_, _) => ApplyResponsiveDashboardLayout();
        ApplyTheme();
        ApplyResponsiveDashboardLayout();
    }

    public int CurrentKpiColumns => _kpiGrid.ColumnCount;
    public int CurrentChartColumns => _chartsGrid.ColumnCount;

    /// <summary>
    /// Adjusts KPI card grid columns (3 / 2 / 1) and chart layout (2 / 1) smoothly across breakpoints.
    /// </summary>
    private void ApplyResponsiveDashboardLayout()
    {
        if (IsDisposed) return;

        var width = ClientSize.Width;
        if (width < 200) return;

        var cards = new[] { _cardOrders, _cardShipments, _cardInventory, _cardFinance, _cardCod, _cardWarehouses };
        var targetKpiCols = width >= 1060 ? 3 : width >= 620 ? 2 : 1;

        if (_currentKpiCols != targetKpiCols)
        {
            _currentKpiCols = targetKpiCols;
            var kpiRows = (int)Math.Ceiling(cards.Length / (double)targetKpiCols);
            _kpiGrid.SuspendLayout();
            try
            {
                _kpiGrid.Controls.Clear();
                _kpiGrid.ColumnStyles.Clear();
                _kpiGrid.RowStyles.Clear();
                _kpiGrid.ColumnCount = targetKpiCols;
                _kpiGrid.RowCount = kpiRows;

                var colPct = 100F / targetKpiCols;
                for (int c = 0; c < targetKpiCols; c++)
                    _kpiGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, colPct));
                for (int r = 0; r < kpiRows; r++)
                    _kpiGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                for (int i = 0; i < cards.Length; i++)
                {
                    var isLastInRow = (i % targetKpiCols) == (targetKpiCols - 1);
                    cards[i].Margin = new Padding(0, 0, isLastInRow ? 0 : 10, 10);
                    _kpiGrid.Controls.Add(cards[i], i % targetKpiCols, i / targetKpiCols);
                }
            }
            finally
            {
                _kpiGrid.ResumeLayout(true);
            }
        }

        var targetChartCols = width >= 860 ? 2 : 1;
        if (_currentChartCols != targetChartCols)
        {
            _currentChartCols = targetChartCols;
            _chartsGrid.SuspendLayout();
            try
            {
                _chartsGrid.Controls.Clear();
                _chartsGrid.ColumnStyles.Clear();
                _chartsGrid.RowStyles.Clear();
                _chartsGrid.ColumnCount = targetChartCols;
                _chartsGrid.RowCount = targetChartCols == 2 ? 1 : 2;

                if (targetChartCols == 2)
                {
                    _chartsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                    _chartsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                    _chartsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                    _leftChart.Margin = new Padding(0, 0, 8, 0);
                    _rightChart.Margin = new Padding(8, 0, 0, 0);
                    _chartsGrid.Controls.Add(_leftChart, 0, 0);
                    _chartsGrid.Controls.Add(_rightChart, 1, 0);
                }
                else
                {
                    _chartsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                    _chartsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
                    _chartsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
                    _leftChart.Margin = new Padding(0, 0, 0, 10);
                    _rightChart.Margin = new Padding(0);
                    _chartsGrid.Controls.Add(_leftChart, 0, 0);
                    _chartsGrid.Controls.Add(_rightChart, 0, 1);
                }
            }
            finally
            {
                _chartsGrid.ResumeLayout(true);
            }
        }

        var usedTop = _topBar.Height + _kpiGrid.Height + _notesLabel.Height + Padding.Vertical + 18;
        var availableChartH = ClientSize.Height - usedTop;
        var desiredChartH = targetChartCols == 2
            ? Math.Max(280, Math.Min(560, availableChartH))
            : Math.Max(520, availableChartH);
        if (Math.Abs(_chartsGrid.Height - desiredChartH) > 4)
            _chartsGrid.Height = desiredChartH;
    }

    private void PopulateScopes()
    {
        _dashboardScopeCombo.Items.Clear();
        if (_auth.HasPermission(ClientPermissions.Reporting.DashboardCompanyRead))
            _dashboardScopeCombo.Items.Add(new SelectItem("Tổng quan toàn công ty", "Company"));
        if (_auth.HasPermission(ClientPermissions.Reporting.DashboardBranchRead) && _branchContext.ActiveBranchId.HasValue)
            _dashboardScopeCombo.Items.Add(new SelectItem($"Chi nhánh: {_branchContext.ActiveBranch?.Name ?? _branchContext.ActiveBranchId.ToString()}", "Branch"));
        if (_auth.HasPermission(ClientPermissions.Reporting.DashboardRead))
            _dashboardScopeCombo.Items.Add(new SelectItem("Điều hành vận hành (Operations)", "Operations"));
        if (_auth.HasPermission(ClientPermissions.Reporting.DeliveryRead))
            _dashboardScopeCombo.Items.Add(new SelectItem("Hiệu suất giao nhận & SLA", "Delivery"));
        if (_auth.HasPermission(ClientPermissions.Reporting.FinanceRead))
            _dashboardScopeCombo.Items.Add(new SelectItem("Tài chính & Hóa đơn", "Finance"));
        if (_auth.HasPermission(ClientPermissions.Reporting.CodRead))
            _dashboardScopeCombo.Items.Add(new SelectItem("Thu hộ COD & Đối soát", "COD"));
        if (_auth.HasPermission(ClientPermissions.Reporting.WarehouseRead))
            _dashboardScopeCombo.Items.Add(new SelectItem("Kho & Phân loại bưu kiện", "Warehouse"));
        if (_auth.HasPermission(ClientPermissions.Reporting.InventoryRead))
            _dashboardScopeCombo.Items.Add(new SelectItem("Tồn kho vật tư / SKU", "Inventory"));

        if (_dashboardScopeCombo.Items.Count > 0)
            _dashboardScopeCombo.SelectedIndex = 0;
    }

    private string? GetSelectedScopeKey()
    {
        if (_dashboardScopeCombo.SelectedIndex >= 0 &&
            _dashboardScopeCombo.SelectedIndex < _dashboardScopeCombo.Items.Count &&
            _dashboardScopeCombo.Items[_dashboardScopeCombo.SelectedIndex] is SelectItem item)
        {
            return item.Tag?.ToString();
        }
        return _dashboardScopeCombo.SelectedValue?.ToString();
    }

    protected override async Task RefreshDataAsync()
    {
        var previous = GetSelectedScopeKey();
        PopulateScopes();
        if (previous != null)
            _dashboardScopeCombo.SelectedValue = previous;

        var selectedMode = GetSelectedScopeKey() ?? "Operations";
        var fromDate = (_fromPicker.Value ?? DateTime.Today.AddDays(-30)).Date;
        var toDate = (_toPicker.Value ?? DateTime.Today).Date;
        var filter = new ReportFilterRequest
        {
            BranchId = _branchContext.ActiveBranchId,
            StartDate = _useDateFilter.Checked ? DateOnly.FromDateTime(fromDate) : null,
            EndDate = _useDateFilter.Checked ? DateOnly.FromDateTime(toDate) : null,
            TimeZoneId = "SE Asia Standard Time"
        };

        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            _scopeInfoLabel.Text = _branchContext.ActiveBranch != null
                ? $"Phạm vi: {_branchContext.ActiveBranch.Code} - {_branchContext.ActiveBranch.Name}"
                : "Phạm vi: Toàn hệ thống được cấp phép";

            if (selectedMode == "Company" && _auth.HasPermission(ClientPermissions.Reporting.DashboardCompanyRead))
            {
                var d = await _reportingClient.GetCompanyOverviewAsync(filter);
                _cardOrders.UpdateMetric($"{d.TotalOrders:N0}", $"Chi nhánh: {d.TotalActiveBranches} | KH: {d.TotalActiveCustomers}");
                _cardShipments.UpdateMetric($"{d.TotalShipments:N0}", $"Đang giao: {d.ShipmentsCurrentlyInTransit} | Đã giao: {d.DeliveredShipments}");
                _cardInventory.UpdateMetric($"{d.InventorySummary.TotalQuantityAvailable:N0}", $"Trên kho: {d.InventorySummary.TotalQuantityOnHand:N0} | SKU: {d.InventorySummary.DistinctProductsInStock}");
                _cardFinance.UpdateMetric($"{d.FinancialSummary.TotalCustomerOutstandingReceivables:N0}", $"Hóa đơn phát hành: {d.FinancialSummary.TotalIssuedInvoiceAmount:N0}");
                _cardCod.UpdateMetric($"{d.CodSummary.TotalCollectedByDrivers:N0}", $"Tài xế giữ: {d.CodSummary.OutstandingDriverCashInHand:N0}");
                _cardWarehouses.UpdateMetric($"{d.PendingWarehouseReceipts + d.PendingWarehouseDispatches:N0}", $"Chờ nhập: {d.PendingWarehouseReceipts} | Chờ xuất: {d.PendingWarehouseDispatches}");

                _leftChart.SetData("Chỉ số vận đơn & giao nhận", [
                    new("Tổng vận đơn", d.TotalShipments, Color: AppTheme.PrimaryBlue),
                    new("Đã giao thành công", d.DeliveredShipments, Color: AppTheme.SuccessEmerald),
                    new("Đang vận chuyển", d.ShipmentsCurrentlyInTransit, Color: AppTheme.PurpleAccent),
                    new("Chờ lấy hàng", d.ShipmentsAwaitingPickup, Color: AppTheme.WarningAmber),
                    new("Chờ hoàn / lỗi", d.ReturnedShipments + d.FailedDeliveryAttempts, Color: AppTheme.DangerRed)
                ]);
                _rightChart.SetData("Tóm tắt dòng tiền COD & Hóa đơn", [
                    new("COD dự kiến", d.CodSummary.TotalExpectedCodOnShipments, $"{d.CodSummary.TotalExpectedCodOnShipments:N0}", AppTheme.PrimaryBlue),
                    new("COD đã thu", d.CodSummary.TotalCollectedByDrivers, $"{d.CodSummary.TotalCollectedByDrivers:N0}", AppTheme.SuccessEmerald),
                    new("COD đã đối soát", d.CodSummary.TotalVerifiedByCashiers, $"{d.CodSummary.TotalVerifiedByCashiers:N0}", AppTheme.PurpleAccent),
                    new("COD đã trả KH", d.CodSummary.TotalRemittedToCustomers, $"{d.CodSummary.TotalRemittedToCustomers:N0}", AppTheme.WarningAmber),
                    new("Công nợ KH", d.FinancialSummary.TotalCustomerOutstandingReceivables, $"{d.FinancialSummary.TotalCustomerOutstandingReceivables:N0}", AppTheme.DangerRed)
                ]);
                _notesLabel.Text = "Dữ liệu được tổng hợp trực tiếp từ Backend Reporting API (chỉ số tồn kho & công nợ là số liệu hiện hành).";
            }
            else if (selectedMode == "Branch" && _branchContext.ActiveBranchId.HasValue)
            {
                var b = await _reportingClient.GetBranchDashboardAsync(_branchContext.ActiveBranchId.Value, filter);
                _cardOrders.UpdateMetric($"{b.OrdersCreated:N0}", $"Chi nhánh: {b.BranchCode}");
                _cardShipments.UpdateMetric($"{b.ShipmentsDelivered:N0} / {b.ShipmentsCreatedAtOrigin:N0}", $"Tỷ lệ giao: {b.DeliverySuccessRatePercent:N1}%");
                _cardInventory.UpdateMetric($"{b.InventorySummary.TotalQuantityAvailable:N0}", $"Kho khả dụng ({b.InventorySummary.DistinctProductsInStock} SKU)");
                _cardFinance.UpdateMetric(b.FinancialSummary != null ? $"{b.FinancialSummary.TotalIssuedInvoiceAmount:N0}" : "Ẩn (Thiếu quyền)", b.FinancialSummary != null ? $"Đã thu: {b.FinancialSummary.TotalPaymentsConfirmed:N0}" : "Cần reports.finance.read");
                _cardCod.UpdateMetric($"{b.CodOperations.TotalCollectedByDrivers:N0}", $"Giữ tiền mặt: {b.CodOperations.OutstandingDriverCashInHand:N0}");
                _cardWarehouses.UpdateMetric($"{b.PendingReceiptsCount + b.PendingDispatchesCount:N0}", $"Nhập chờ: {b.PendingReceiptsCount} | Xuất chờ: {b.PendingDispatchesCount}");

                _leftChart.SetData($"Luồng hàng chi nhánh {b.BranchCode}", [
                    new("Đơn hàng tạo mới", b.OrdersCreated, Color: AppTheme.PrimaryBlue),
                    new("Vận đơn phát đi", b.ShipmentsCreatedAtOrigin, Color: AppTheme.PurpleAccent),
                    new("Vận đơn nhận đến", b.ShipmentsReceivedAtDestination, Color: AppTheme.WarningAmber),
                    new("Giao thành công", b.ShipmentsDelivered, Color: AppTheme.SuccessEmerald)
                ]);
                _rightChart.SetData($"COD chi nhánh {b.BranchCode}", [
                    new("Dự kiến thu", b.CodOperations.TotalExpectedCodOnShipments, $"{b.CodOperations.TotalExpectedCodOnShipments:N0}", AppTheme.PrimaryBlue),
                    new("Đã thu", b.CodOperations.TotalCollectedByDrivers, $"{b.CodOperations.TotalCollectedByDrivers:N0}", AppTheme.SuccessEmerald),
                    new("Đã kiểm quỹ", b.CodOperations.TotalVerifiedByCashiers, $"{b.CodOperations.TotalVerifiedByCashiers:N0}", AppTheme.PurpleAccent),
                    new("Tồn tay tài xế", b.CodOperations.OutstandingDriverCashInHand, $"{b.CodOperations.OutstandingDriverCashInHand:N0}", AppTheme.DangerRed)
                ]);
                _notesLabel.Text = b.FinancialSummary == null ? "Số liệu tài chính chi nhánh bị ẩn theo phân quyền hiện tại." : string.Empty;
            }
            else if (selectedMode == "Delivery")
            {
                var deliv = await _reportingClient.GetDeliveryPerformanceDashboardAsync(filter);
                _cardOrders.UpdateMetric($"{deliv.CompletedTerminalShipmentsCount:N0}", $"Đã loại {deliv.InTransitActiveExcludedCount} đơn đang giao");
                _cardShipments.UpdateMetric(deliv.SuccessfulDeliveryRatePercent.HasValue ? $"{deliv.SuccessfulDeliveryRatePercent:N1}%" : "N/A", $"Giao: {deliv.DeliveredShipmentsCount} | Lỗi/Hoàn: {deliv.FailedOutcomeShipmentsCount}");
                _cardInventory.UpdateMetric(deliv.OnTimeDeliveryRatePercent.HasValue ? $"{deliv.OnTimeDeliveryRatePercent:N1}%" : "N/A", $"Đúng hạn: {deliv.OnTimeDeliveredCount} / Có SLA: {deliv.DeliveredWithSlaCount}");
                _cardFinance.UpdateMetric(deliv.AverageDeliveryDurationHours.HasValue ? $"{deliv.AverageDeliveryDurationHours:N1}h" : "N/A", "Thời gian giao trung bình");
                _cardCod.UpdateMetric(deliv.AverageAttemptsPerAttemptedShipment.HasValue ? $"{deliv.AverageAttemptsPerAttemptedShipment:N2}" : "N/A", "Số lần giao TB/vận đơn");
                _cardWarehouses.UpdateMetric($"{deliv.DeliveredWithoutSlaCount:N0}", "Đơn giao không có mốc SLA");
                _leftChart.SetData("Kết quả trạng thái cuối (Terminal)", [
                    new("Giao thành công", deliv.DeliveredShipmentsCount, Color: AppTheme.SuccessEmerald),
                    new("Thất bại / Hoàn", deliv.FailedOutcomeShipmentsCount, Color: AppTheme.DangerRed),
                    new("Đang giao (loại trừ)", deliv.InTransitActiveExcludedCount, Color: AppTheme.WarningAmber)
                ]);
                _rightChart.SetData("Đánh giá SLA giao hàng", [
                    new("Đúng hạn", deliv.OnTimeDeliveredCount, Color: AppTheme.SuccessEmerald),
                    new("Trễ hạn", deliv.LateDeliveredCount, Color: AppTheme.DangerRed),
                    new("Không có SLA", deliv.DeliveredWithoutSlaCount, Color: AppTheme.TextMuted)
                ]);
                _notesLabel.Text = "Tỷ lệ giao thành công chỉ tính trên vận đơn đã kết thúc (không gộp vận đơn đang đi đường vào thất bại).";
            }
            else if (selectedMode == "Finance")
            {
                var f = await _reportingClient.GetFinanceDashboardAsync(filter);
                _cardOrders.UpdateMetric($"{f.IssuedAndActiveInvoicesCount:N0}", $"Tổng phát hành: {f.IssuedInvoicesTotal:N0}");
                _cardShipments.UpdateMetric($"{f.TotalShipmentChargesAmount:N0}", $"Chưa xuất HĐ: {f.UnbilledShipmentChargesAmount:N0}");
                _cardInventory.UpdateMetric($"{f.TotalPaymentsConfirmed:N0}", $"Đã phân bổ: {f.TotalPaymentsAllocated:N0}");
                _cardFinance.UpdateMetric($"{f.AuthoritativeCustomerReceivables:N0}", $"Chưa phân bổ: {f.TotalUnallocatedPaymentCredit:N0}");
                _cardCod.UpdateMetric($"{f.DraftInvoicesTotal:N0}", $"Hóa đơn nháp ({f.DraftInvoicesCount})");
                _cardWarehouses.UpdateMetric($"{f.AuthoritativeUnbilledBalance:N0}", "Dư nợ chưa xuất HĐ");
                _leftChart.SetData("Tổng quan Cước & Hóa đơn", [
                    new("Cước vận đơn", f.TotalShipmentChargesAmount, $"{f.TotalShipmentChargesAmount:N0}", AppTheme.PrimaryBlue),
                    new("Hóa đơn phát hành", f.IssuedInvoicesTotal, $"{f.IssuedInvoicesTotal:N0}", AppTheme.PurpleAccent),
                    new("Thanh toán xác nhận", f.TotalPaymentsConfirmed, $"{f.TotalPaymentsConfirmed:N0}", AppTheme.SuccessEmerald),
                    new("Công nợ phải thu", f.AuthoritativeCustomerReceivables, $"{f.AuthoritativeCustomerReceivables:N0}", AppTheme.DangerRed)
                ]);
                _rightChart.SetData("Phân bổ thanh toán", [
                    new("Đã phân bổ hóa đơn", f.TotalPaymentsAllocated, $"{f.TotalPaymentsAllocated:N0}", AppTheme.SuccessEmerald),
                    new("Tiền ứng chưa phân bổ", f.TotalUnallocatedPaymentCredit, $"{f.TotalUnallocatedPaymentCredit:N0}", AppTheme.WarningAmber)
                ]);
            }
            else if (selectedMode == "COD")
            {
                var c = await _reportingClient.GetCodDashboardAsync(filter);
                _cardOrders.UpdateMetric($"{c.ExpectedCodOnShipments:N0}", "COD dự kiến trên vận đơn");
                _cardShipments.UpdateMetric($"{c.CollectedByDrivers:N0}", $"{c.CollectionsCount} lượt thu");
                _cardInventory.UpdateMetric($"{c.VerifiedByCashiers:N0}", $"Đã nộp CN: {c.DepositedToBranches:N0}");
                _cardFinance.UpdateMetric($"{c.RemittedToCustomersNet:N0}", $"{c.CompletedRemittancesCount} bảng kê hoàn");
                _cardCod.UpdateMetric($"{c.OutstandingDriverCashInHand:N0}", "Tồn tay tài xế chưa nộp");
                _cardWarehouses.UpdateMetric($"{c.ExpectedVsCollectedDiscrepancy:N0}", "Chênh lệch Dự kiến - Thực thu");
                _leftChart.SetData("Vòng đời dòng tiền COD", [
                    new("Dự kiến trên vận đơn", c.ExpectedCodOnShipments, $"{c.ExpectedCodOnShipments:N0}", AppTheme.PrimaryBlue),
                    new("Tài xế đã thu", c.CollectedByDrivers, $"{c.CollectedByDrivers:N0}", AppTheme.SuccessEmerald),
                    new("Thủ quỹ xác nhận", c.VerifiedByCashiers, $"{c.VerifiedByCashiers:N0}", AppTheme.PurpleAccent),
                    new("Đã hoàn trả KH", c.RemittedToCustomersNet, $"{c.RemittedToCustomersNet:N0}", AppTheme.WarningAmber)
                ]);
                _rightChart.SetData("Giữ quỹ & Chênh lệch", [
                    new("Tài xế đang giữ", c.OutstandingDriverCashInHand, $"{c.OutstandingDriverCashInHand:N0}", AppTheme.DangerRed),
                    new("Chi nhánh giữ chờ trả", c.UnremittedVerifiedHolding, $"{c.UnremittedVerifiedHolding:N0}", AppTheme.WarningAmber),
                    new("Chênh lệch thu hộ", Math.Abs(c.ExpectedVsCollectedDiscrepancy), $"{c.ExpectedVsCollectedDiscrepancy:N0}", AppTheme.NavyDark)
                ]);
            }
            else if (selectedMode == "Warehouse")
            {
                var w = await _reportingClient.GetWarehouseDashboardAsync(filter);
                _cardOrders.UpdateMetric($"{w.TotalReceipts:N0}", $"Chờ: {w.PendingReceipts} | Hoàn tất: {w.CompletedReceipts}");
                _cardShipments.UpdateMetric($"{w.TotalDispatches:N0}", $"Chờ: {w.PendingDispatches} | Đã xuất: {w.CompletedDispatches}");
                _cardInventory.UpdateMetric($"{w.ParcelEventsTotal:N0}", "Tổng lượt quét bưu kiện");
                _cardFinance.UpdateMetric($"{w.ParcelsUnloadedAtHub:N0}", $"Chờ phân loại: {w.ParcelsAwaitingSorting}");
                _cardCod.UpdateMetric($"{w.ParcelsSortedAtHub:N0}", $"Chờ xuất hub: {w.ParcelsAwaitingDispatch}");
                _cardWarehouses.UpdateMetric($"{w.ParcelsDispatchedFromHub:N0}", "Bưu kiện đã rời hub");
                _leftChart.SetData("Sự kiện xử lý bưu kiện (Hub)", w.ParcelEventsByType.Select(x => new BarChartDatum(x.Status, x.Count)).ToList());
                _rightChart.SetData("Thông lượng phiếu kho", [
                    new("Phiếu nhập chờ", w.PendingReceipts, Color: AppTheme.WarningAmber),
                    new("Phiếu nhập hoàn tất", w.CompletedReceipts, Color: AppTheme.SuccessEmerald),
                    new("Phiếu xuất chờ", w.PendingDispatches, Color: AppTheme.PrimaryBlue),
                    new("Phiếu xuất hoàn tất", w.CompletedDispatches, Color: AppTheme.PurpleAccent)
                ]);
            }
            else if (selectedMode == "Inventory")
            {
                var inv = await _reportingClient.GetInventoryDashboardAsync(filter);
                _cardOrders.UpdateMetric($"{inv.Summary.DistinctProductsInStock:N0}", "SKU đang có tồn kho");
                _cardShipments.UpdateMetric($"{inv.Summary.TotalQuantityOnHand:N0}", $"Giữ chỗ: {inv.Summary.TotalQuantityAllocated:N0}");
                _cardInventory.UpdateMetric($"{inv.Summary.TotalQuantityAvailable:N0}", $"Hư hỏng: {inv.Summary.TotalQuantityDamaged:N0}");
                _cardFinance.UpdateMetric($"{inv.LowStockProductWarehouseCount:N0}", "SKU dưới mức đặt lại");
                _cardCod.UpdateMetric($"{inv.ActiveReservationsCount:N0}", $"Đang giữ: {inv.ReservedUnitsTotal:N0}");
                _cardWarehouses.UpdateMetric($"{inv.TransactionsCountInPeriod:N0}", $"Lots: {inv.DistinctLotsCount} | Serial: {inv.SerialNumbersAvailableCount}");
                _leftChart.SetData("Tồn kho tổng hợp (SKU)", [
                    new("Thực tế trên kho", inv.Summary.TotalQuantityOnHand, Color: AppTheme.PrimaryBlue),
                    new("Khả dụng", inv.Summary.TotalQuantityAvailable, Color: AppTheme.SuccessEmerald),
                    new("Đã giữ chỗ", inv.Summary.TotalQuantityAllocated, Color: AppTheme.WarningAmber),
                    new("Hàng lỗi/hỏng", inv.Summary.TotalQuantityDamaged, Color: AppTheme.DangerRed)
                ]);
                _rightChart.SetData("Cảnh báo & Tác vụ kho", [
                    new("Dưới định mức", inv.LowStockProductWarehouseCount, Color: AppTheme.DangerRed),
                    new("Đặt giữ chỗ active", inv.ActiveReservationsCount, Color: AppTheme.WarningAmber),
                    new("Giao dịch trong kỳ", inv.TransactionsCountInPeriod, Color: AppTheme.PrimaryBlue)
                ]);
            }
            else
            {
                var ops = await _reportingClient.GetOperationsDashboardAsync(filter);
                _cardOrders.UpdateMetric($"{ops.OrdersAwaitingProcessing:N0}", "Đơn chờ xử lý");
                _cardShipments.UpdateMetric($"{ops.ShipmentsInTransit:N0}", $"Chờ lấy: {ops.ShipmentsAwaitingPickup} | Hub: {ops.ShipmentsInSortingHub}");
                _cardInventory.UpdateMetric($"{ops.ShipmentsDelivered:N0}", $"Đang phát: {ops.ShipmentsAwaitingDelivery}");
                _cardFinance.UpdateMetric($"{ops.FailedDeliveryAttempts:N0}", $"Hoàn hàng: {ops.ActiveReturnRequests}");
                _cardCod.UpdateMetric($"{ops.ParcelHandlingEventsCount:N0}", "Lượt quét bưu kiện Hub");
                _cardWarehouses.UpdateMetric($"{ops.WarehouseReceiptsPending + ops.WarehouseDispatchesPending:N0}", $"Nhập chờ: {ops.WarehouseReceiptsPending} | Xuất chờ: {ops.WarehouseDispatchesPending}");
                _leftChart.SetData("Trạng thái điều vận", [
                    new("Chờ lấy hàng", ops.ShipmentsAwaitingPickup, Color: AppTheme.WarningAmber),
                    new("Tại Hub phân loại", ops.ShipmentsInSortingHub, Color: AppTheme.PurpleAccent),
                    new("Đang vận chuyển", ops.ShipmentsInTransit, Color: AppTheme.PrimaryBlue),
                    new("Đang phát cuối", ops.ShipmentsAwaitingDelivery, Color: AppTheme.PrimaryHover),
                    new("Đã giao", ops.ShipmentsDelivered, Color: AppTheme.SuccessEmerald)
                ]);
                _rightChart.SetData("Cảnh báo vận hành", [
                    new("Đơn chờ xử lý", ops.OrdersAwaitingProcessing, Color: AppTheme.WarningAmber),
                    new("Giao thất bại", ops.FailedDeliveryAttempts, Color: AppTheme.DangerRed),
                    new("Yêu cầu hoàn hàng", ops.ActiveReturnRequests, Color: AppTheme.PurpleAccent)
                ]);
            }
        }, _refreshBtn, logger: _logger);
    }

    public void ApplyTheme()
    {
        BackColor = AppTheme.SurfaceBg;
        _topBar.Back = AppTheme.CardBg;
        _topBar.BorderColor = AppTheme.BorderSubtle;
        _scopeInfoLabel.ForeColor = AppTheme.TextMuted;
        _useDateFilter.ForeColor = AppTheme.TextPrimary;
        _cardOrders.ApplyTheme();
        _cardShipments.ApplyTheme();
        _cardInventory.ApplyTheme();
        _cardFinance.ApplyTheme();
        _cardCod.ApplyTheme();
        _cardWarehouses.ApplyTheme();
        _leftChart.ApplyTheme();
        _rightChart.ApplyTheme();
    }
}
