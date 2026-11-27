using Logistics.Client.Application.Contracts.Warehouse;
using Logistics.Client.Application.Interfaces;
using Logistics.Client.Application.Permissions;
using Logistics.Client.WinForms.Controls.Common;
using Logistics.Client.WinForms.Helpers;
using Logistics.Client.WinForms.Themes;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.Extensions.Logging;

namespace Logistics.Client.WinForms.Controls.Inventory;

public sealed class InventoryWorkspaceControl : ModuleViewBase
{
    private readonly IInventoryApiClient _inventoryApi;
    private readonly IWarehouseApiClient _warehouseApi;
    private readonly ICatalogApiClient _catalogApi;
    private readonly ISalesApiClient _salesApi;
    private readonly IAuthenticationService _authService;
    private readonly IAuthorizationService _auth;
    private readonly IBranchContextService _branchContext;
    private readonly ILogger<InventoryWorkspaceControl> _logger;

    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill, Font = AppTheme.BodyFont };
    private readonly TabPage _tabBalances = new("Tồn kho & Định mức (Chỉ xem số liệu)") { BackColor = AppTheme.SurfaceBg };
    private readonly TabPage _tabReceipts = new("Phiếu nhập kho & Giao dịch") { BackColor = AppTheme.SurfaceBg };
    private readonly TabPage _tabMovements = new("Điều chỉnh, Chuyển kho & Giữ hàng") { BackColor = AppTheme.SurfaceBg };
    private readonly TabPage _tabTracking = new("Lô hàng (Lots) & Số Serial") { BackColor = AppTheme.SurfaceBg };

    private ComboBox _whFilterCombo = null!;
    private PagedDataGridControl _balancesGrid = null!;
    private PagedDataGridControl _itemsSetupGrid = null!;
    private PagedDataGridControl _receiptsGrid = null!;
    private PagedDataGridControl _transactionsGrid = null!;
    private PagedDataGridControl _adjustmentsGrid = null!;
    private PagedDataGridControl _transfersGrid = null!;
    private PagedDataGridControl _reservationsGrid = null!;
    private PagedDataGridControl _lotsGrid = null!;
    private PagedDataGridControl _serialsGrid = null!;

    public InventoryWorkspaceControl(
        IInventoryApiClient inventoryApi,
        IWarehouseApiClient warehouseApi,
        ICatalogApiClient catalogApi,
        ISalesApiClient salesApi,
        IAuthenticationService authService,
        IAuthorizationService auth,
        IBranchContextService branchContext,
        ILogger<InventoryWorkspaceControl> logger)
    {
        _inventoryApi = inventoryApi;
        _warehouseApi = warehouseApi;
        _catalogApi = catalogApi;
        _salesApi = salesApi;
        _authService = authService;
        _auth = auth;
        _branchContext = branchContext;
        _logger = logger;

        BuildTopFilterBar();
        BuildBalancesTab();
        BuildReceiptsTab();
        BuildMovementsTab();
        BuildTrackingTab();

        if (_auth.HasAnyPermission(ClientPermissions.Inventory.InventoryRead, ClientPermissions.Inventory.InventoryManage))
            _tabs.TabPages.Add(_tabBalances);
        if (_auth.HasAnyPermission(ClientPermissions.Inventory.InventoryRead, ClientPermissions.Warehouse.ReceiptsManage))
            _tabs.TabPages.Add(_tabReceipts);
        if (_auth.HasAnyPermission(ClientPermissions.Inventory.InventoryRead, ClientPermissions.Inventory.AdjustmentsManage, ClientPermissions.Inventory.TransfersManage, ClientPermissions.Inventory.ReservationsManage))
            _tabs.TabPages.Add(_tabMovements);
        if (_auth.HasAnyPermission(ClientPermissions.Inventory.InventoryRead, ClientPermissions.Inventory.LotsManage, ClientPermissions.Inventory.SerialNumbersManage))
            _tabs.TabPages.Add(_tabTracking);

        _tabs.SelectedIndexChanged += async (_, _) => await RefreshDataAsync();
        Controls.Add(_tabs);
    }

    public void SelectSection(string navKey)
    {
        var tab = navKey switch
        {
            "Inventory.Receipts" => _tabReceipts,
            "Inventory.Movements" => _tabMovements,
            "Inventory.Tracking" => _tabTracking,
            _ => _tabBalances
        };
        if (_tabs.TabPages.Contains(tab)) _tabs.SelectedTab = tab;
    }

    private void BuildTopFilterBar()
    {
        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, BackColor = AppTheme.CardBg, Padding = new Padding(12, 6, 12, 4) };
        bar.Controls.Add(new Label { Text = "Kho vật tư:", AutoSize = true, Font = AppTheme.BodyBoldFont, Margin = new Padding(0, 7, 6, 0) });
        _whFilterCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240, Font = AppTheme.BodyFont };
        _whFilterCombo.SelectedIndexChanged += async (_, _) => await RefreshActiveTabAsync();
        bar.Controls.Add(_whFilterCombo);
        Controls.Add(bar);
    }

    private void BuildBalancesTab()
    {
        var split = AppTheme.CreateSafeSplitContainer(Orientation.Horizontal, 300, 180);
        _balancesGrid = new PagedDataGridControl("Số dư tồn kho thực tế (InventoryBalances)", "Chỉ đọc từ server: OnHand, Allocated, Available, Damaged.");
        _balancesGrid.ReloadRequested += async (_, _) => await LoadBalancesTabAsync();

        _itemsSetupGrid = new PagedDataGridControl("Định mức tồn kho theo SKU (InventoryItems)", "Cấu hình điểm đặt lại hàng (ReorderPoint), tồn an toàn và tồn tối đa.");
        if (_auth.HasPermission(ClientPermissions.Inventory.InventoryManage))
        {
            var addItemBtn = AppTheme.CreatePrimaryButton("Thêm định mức SKU", 155);
            addItemBtn.Click += async (_, _) => await CreateInventoryItemAsync();
            _itemsSetupGrid.AddActionButton(addItemBtn);
        }
        _itemsSetupGrid.ReloadRequested += async (_, _) => await LoadBalancesTabAsync();

        split.Panel1.Controls.Add(_balancesGrid);
        split.Panel2.Controls.Add(_itemsSetupGrid);
        _tabBalances.Controls.Add(split);
    }

    private void BuildReceiptsTab()
    {
        var split = AppTheme.CreateSafeSplitContainer(Orientation.Horizontal, 300);
        _receiptsGrid = new PagedDataGridControl("Phiếu nhập kho (WarehouseReceipts)", "Tạo phiếu nhập vật tư và xác nhận nhập kho để tăng InventoryBalances.");
        if (_auth.HasPermission(ClientPermissions.Warehouse.ReceiptsManage))
        {
            var addBtn = AppTheme.CreatePrimaryButton("Tạo phiếu nhập", 130);
            addBtn.Click += async (_, _) => await CreateReceiptAsync();
            var confirmBtn = AppTheme.CreateSuccessButton("Xác nhận nhập", 130);
            confirmBtn.Click += async (_, _) => await ConfirmReceiptAsync();
            _receiptsGrid.AddActionButton(addBtn);
            _receiptsGrid.AddActionButton(confirmBtn);
        }
        _receiptsGrid.ReloadRequested += async (_, _) => await LoadReceiptsTabAsync();

        _transactionsGrid = new PagedDataGridControl("Sổ nhật ký giao dịch kho (InventoryTransactions - Chỉ đọc)", "Mọi biến động nhập, xuất, điều chỉnh, chuyển kho đều được backend ghi nhận bất biến.");
        _transactionsGrid.ReloadRequested += async (_, _) => await LoadReceiptsTabAsync();

        split.Panel1.Controls.Add(_receiptsGrid);
        split.Panel2.Controls.Add(_transactionsGrid);
        _tabReceipts.Controls.Add(split);
    }

    private void BuildMovementsTab()
    {
        var innerTabs = new TabControl { Dock = DockStyle.Fill, Font = AppTheme.BodyFont };

        // Adjustments
        var adjPage = new TabPage("Điều chỉnh tồn kho (Adjustments)");
        _adjustmentsGrid = new PagedDataGridControl("Phiếu điều chỉnh tồn kho", "Tạo phiếu kiểm kê / hư hỏng và phê duyệt điều chỉnh.");
        if (_auth.HasPermission(ClientPermissions.Inventory.AdjustmentsManage))
        {
            var addAdj = AppTheme.CreatePrimaryButton("Tạo điều chỉnh", 130);
            addAdj.Click += async (_, _) => await CreateAdjustmentAsync();
            var appAdj = AppTheme.CreateSuccessButton("Phê duyệt", 105);
            appAdj.Click += async (_, _) => await ApproveAdjustmentAsync();
            _adjustmentsGrid.AddActionButton(addAdj);
            _adjustmentsGrid.AddActionButton(appAdj);
        }
        _adjustmentsGrid.ReloadRequested += async (_, _) => await LoadMovementsTabAsync();
        adjPage.Controls.Add(_adjustmentsGrid);

        // Transfers
        var trfPage = new TabPage("Chuyển kho (Stock Transfers)");
        _transfersGrid = new PagedDataGridControl("Lệnh chuyển kho giữa các kho", "Tạo lệnh chuyển, xác nhận xuất chuyển kho và xác nhận nhận hàng tại kho đích.");
        if (_auth.HasPermission(ClientPermissions.Inventory.TransfersManage))
        {
            var addTrf = AppTheme.CreatePrimaryButton("Tạo chuyển kho", 130);
            addTrf.Click += async (_, _) => await CreateTransferAsync();
            var confTrf = AppTheme.CreateSecondaryButton("Xác nhận xuất", 125);
            confTrf.Click += async (_, _) => await ConfirmTransferAsync();
            var recvTrf = AppTheme.CreateSuccessButton("Nhận chuyển kho", 135);
            recvTrf.Click += async (_, _) => await ReceiveTransferAsync();
            _transfersGrid.AddActionButton(addTrf);
            _transfersGrid.AddActionButton(confTrf);
            _transfersGrid.AddActionButton(recvTrf);
        }
        _transfersGrid.ReloadRequested += async (_, _) => await LoadMovementsTabAsync();
        trfPage.Controls.Add(_transfersGrid);

        // Reservations
        var resPage = new TabPage("Giữ hàng theo đơn (Reservations)");
        _reservationsGrid = new PagedDataGridControl("Giữ hàng tồn kho cho đơn hàng", "Tạo, giải phóng hoặc tiêu thụ giữ hàng.");
        if (_auth.HasPermission(ClientPermissions.Inventory.ReservationsManage))
        {
            var addRes = AppTheme.CreatePrimaryButton("Tạo giữ hàng", 120);
            addRes.Click += async (_, _) => await CreateReservationAsync();
            var relRes = AppTheme.CreateDangerButton("Giải phóng", 105);
            relRes.Click += async (_, _) => await HandleReservationActionAsync(true);
            var conRes = AppTheme.CreateSuccessButton("Xuất tiêu thụ", 120);
            conRes.Click += async (_, _) => await HandleReservationActionAsync(false);
            _reservationsGrid.AddActionButton(addRes);
            _reservationsGrid.AddActionButton(relRes);
            _reservationsGrid.AddActionButton(conRes);
        }
        _reservationsGrid.ReloadRequested += async (_, _) => await LoadMovementsTabAsync();
        resPage.Controls.Add(_reservationsGrid);

        innerTabs.TabPages.AddRange([adjPage, trfPage, resPage]);
        innerTabs.SelectedIndexChanged += async (_, _) => await LoadMovementsTabAsync();
        _tabMovements.Controls.Add(innerTabs);
    }

    private void BuildTrackingTab()
    {
        var split = AppTheme.CreateSafeSplitContainer(Orientation.Vertical, 520);
        _lotsGrid = new PagedDataGridControl("Lô hàng (InventoryLots)", "Quản lý mã lô sản xuất và hạn sử dụng.");
        if (_auth.HasPermission(ClientPermissions.Inventory.LotsManage))
        {
            var addLot = AppTheme.CreatePrimaryButton("Thêm lô hàng", 120);
            addLot.Click += async (_, _) => await CreateLotAsync();
            _lotsGrid.AddActionButton(addLot);
        }
        _lotsGrid.ReloadRequested += async (_, _) => await LoadTrackingTabAsync();

        _serialsGrid = new PagedDataGridControl("Số Serial (InventorySerialNumbers)", "Quản lý số Serial thiết bị theo kho và vị trí.");
        if (_auth.HasPermission(ClientPermissions.Inventory.SerialNumbersManage))
        {
            var addSer = AppTheme.CreatePrimaryButton("Thêm Serial", 115);
            addSer.Click += async (_, _) => await CreateSerialAsync();
            _serialsGrid.AddActionButton(addSer);
        }
        _serialsGrid.ReloadRequested += async (_, _) => await LoadTrackingTabAsync();

        split.Panel1.Controls.Add(_lotsGrid);
        split.Panel2.Controls.Add(_serialsGrid);
        _tabTracking.Controls.Add(split);
    }

    private long? SelectedWarehouseId => (_whFilterCombo.SelectedItem as ModalComboOption)?.Value is long id && id > 0 ? id : null;
    private long CurrentEmployeeId => _authService.CurrentUser?.EmployeeId ?? 0L;

    protected override async Task RefreshDataAsync()
    {
        var list = await _warehouseApi.GetWarehousesAsync(null, _branchContext.ActiveBranchId, null, true, 1, 100);
        var prev = SelectedWarehouseId;
        _whFilterCombo.Items.Clear();
        _whFilterCombo.Items.Add(new ModalComboOption(0L, "-- Tất cả kho --"));
        foreach (var w in list.Items)
            _whFilterCombo.Items.Add(new ModalComboOption(w.Id, $"{w.Code} - {w.Name}"));

        _whFilterCombo.SelectedIndex = 0;
        if (prev.HasValue)
        {
            foreach (ModalComboOption item in _whFilterCombo.Items)
            {
                if (Equals(item.Value, prev.Value)) { _whFilterCombo.SelectedItem = item; break; }
            }
        }
        await RefreshActiveTabAsync();
    }

    private async Task RefreshActiveTabAsync()
    {
        if (_tabs.SelectedTab == _tabBalances) await LoadBalancesTabAsync();
        else if (_tabs.SelectedTab == _tabReceipts) await LoadReceiptsTabAsync();
        else if (_tabs.SelectedTab == _tabMovements) await LoadMovementsTabAsync();
        else if (_tabs.SelectedTab == _tabTracking) await LoadTrackingTabAsync();
    }

    private async Task LoadBalancesTabAsync()
    {
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            var bal = await _inventoryApi.GetBalancesAsync(SelectedWarehouseId, null, null, _balancesGrid.CurrentPage, _balancesGrid.CurrentPageSize);
            _balancesGrid.BindPagedData(bal);
            var items = await _inventoryApi.GetItemsAsync(_itemsSetupGrid.SearchText, SelectedWarehouseId, _itemsSetupGrid.CurrentPage, _itemsSetupGrid.CurrentPageSize);
            _itemsSetupGrid.BindPagedData(items);
        }, null, null, _logger);
    }

    private async Task LoadReceiptsTabAsync()
    {
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            var rec = await _inventoryApi.GetReceiptsAsync(SelectedWarehouseId, null, _receiptsGrid.CurrentPage, _receiptsGrid.CurrentPageSize);
            _receiptsGrid.BindPagedData(rec);
            var tx = await _inventoryApi.GetTransactionsAsync(SelectedWarehouseId, null, _transactionsGrid.CurrentPage, _transactionsGrid.CurrentPageSize);
            _transactionsGrid.BindPagedData(tx);
        }, null, null, _logger);
    }

    private async Task LoadMovementsTabAsync()
    {
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            var adj = await _inventoryApi.GetAdjustmentsAsync(SelectedWarehouseId, null, _adjustmentsGrid.CurrentPage, _adjustmentsGrid.CurrentPageSize);
            _adjustmentsGrid.BindPagedData(adj);
            var trf = await _inventoryApi.GetTransfersAsync(SelectedWarehouseId, null, null, _transfersGrid.CurrentPage, _transfersGrid.CurrentPageSize);
            _transfersGrid.BindPagedData(trf);
            var res = await _inventoryApi.GetReservationsAsync(SelectedWarehouseId, null, _reservationsGrid.CurrentPage, _reservationsGrid.CurrentPageSize);
            _reservationsGrid.BindPagedData(res);
        }, null, null, _logger);
    }

    private async Task LoadTrackingTabAsync()
    {
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            var lots = await _inventoryApi.GetLotsAsync(null, _lotsGrid.CurrentPage, _lotsGrid.CurrentPageSize);
            _lotsGrid.BindPagedData(lots);
            var serials = await _inventoryApi.GetSerialNumbersAsync(SelectedWarehouseId, null, _serialsGrid.CurrentPage, _serialsGrid.CurrentPageSize);
            _serialsGrid.BindPagedData(serials);
        }, null, null, _logger);
    }

    private async Task CreateInventoryItemAsync()
    {
        var warehouses = await _warehouseApi.GetWarehousesAsync(null, _branchContext.ActiveBranchId, null, true, 1, 100);
        var products = await _catalogApi.GetProductsAsync(null, null, true, new PagedRequest { Page = 1, PageSize = 100 });
        var dlg = new ModalFormDialog("Thêm định mức tồn kho SKU",
        [
            new() { Key = "WarehouseId", Label = "Kho", Kind = ModalFieldKind.ComboBox, Required = true, InitialValue = SelectedWarehouseId, Options = warehouses.Items.Select(w => new ModalComboOption(w.Id, $"{w.Code} - {w.Name}")).ToList() },
            new() { Key = "ProductId", Label = "Sản phẩm", Kind = ModalFieldKind.ComboBox, Required = true, Options = products.Items.Select(p => new ModalComboOption(p.Id, $"{p.Sku} - {p.Name}")).ToList() },
            new() { Key = "Reorder", Label = "Điểm đặt lại (ReorderPoint)", Kind = ModalFieldKind.Integer, InitialValue = 10 },
            new() { Key = "Safety", Label = "Tồn an toàn (SafetyStock)", Kind = ModalFieldKind.Integer, InitialValue = 5 },
            new() { Key = "Target", Label = "Tồn mục tiêu", Kind = ModalFieldKind.Integer, InitialValue = 100 },
            new() { Key = "Max", Label = "Tồn tối đa", Kind = ModalFieldKind.Integer, InitialValue = 500 }
        ],
        async vals =>
        {
            await _inventoryApi.CreateItemAsync(new CreateInventoryItemRequest
            {
                WarehouseId = Convert.ToInt64(vals["WarehouseId"]),
                ProductId = Convert.ToInt64(vals["ProductId"]),
                ReorderPoint = Convert.ToInt32(vals["Reorder"]),
                SafetyStock = Convert.ToInt32(vals["Safety"]),
                TargetStock = Convert.ToInt32(vals["Target"]),
                MaximumStock = Convert.ToInt32(vals["Max"])
            });
        });
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadBalancesTabAsync();
    }

    private async Task CreateReceiptAsync()
    {
        var warehouses = await _warehouseApi.GetWarehousesAsync(null, _branchContext.ActiveBranchId, null, true, 1, 100);
        var products = await _catalogApi.GetProductsAsync(null, null, true, new PagedRequest { Page = 1, PageSize = 100 });
        var units = await _catalogApi.GetUnitsAsync(null, null, true, new PagedRequest { Page = 1, PageSize = 50 });
        var wid = SelectedWarehouseId ?? warehouses.Items.FirstOrDefault()?.Id ?? 0;
        var locs = wid > 0 ? await _warehouseApi.GetLocationsAsync(wid.ToString(), null, 1, 100) : new PagedResponse<WarehouseLocationDto>();

        var dlg = new ModalFormDialog("Tạo phiếu nhập kho",
        [
            new() { Key = "WarehouseId", Label = "Kho nhập", Kind = ModalFieldKind.ComboBox, Required = true, InitialValue = wid > 0 ? wid : null, Options = warehouses.Items.Select(w => new ModalComboOption(w.Id, $"{w.Code} - {w.Name}")).ToList() },
            new() { Key = "Vendor", Label = "Nhà cung cấp / Nguồn" },
            new() { Key = "ProductId", Label = "Sản phẩm", Kind = ModalFieldKind.ComboBox, Required = true, Options = products.Items.Select(p => new ModalComboOption(p.Id, $"{p.Sku} - {p.Name}")).ToList() },
            new() { Key = "UnitId", Label = "Đơn vị tính", Kind = ModalFieldKind.ComboBox, Required = true, Options = units.Items.Select(u => new ModalComboOption(u.Id, $"{u.Code} - {u.Name}")).ToList() },
            new() { Key = "LocationId", Label = "Vị trí nhận", Kind = ModalFieldKind.ComboBox, Options = locs.Items.Select(l => new ModalComboOption(l.Id, $"{l.Code} ({l.ZoneCode})")).Prepend(new ModalComboOption(null, "(Mặc định)")).ToList() },
            new() { Key = "Qty", Label = "Số lượng dự kiến", Kind = ModalFieldKind.Integer, Required = true, InitialValue = 10 },
            new() { Key = "Notes", Label = "Ghi chú" }
        ],
        async vals =>
        {
            await _inventoryApi.CreateReceiptAsync(new CreateWarehouseReceiptRequest
            {
                WarehouseId = Convert.ToInt64(vals["WarehouseId"]),
                ReceiptType = "InventoryInbound",
                SourceVendor = vals["Vendor"]?.ToString(),
                ReceivedByEmployeeId = CurrentEmployeeId,
                Notes = vals["Notes"]?.ToString(),
                Items =
                [
                    new CreateWarehouseReceiptItemRequest
                    {
                        ProductId = Convert.ToInt64(vals["ProductId"]),
                        UnitOfMeasureId = Convert.ToInt64(vals["UnitId"]),
                        TargetLocationId = vals["LocationId"] != null ? Convert.ToInt64(vals["LocationId"]) : null,
                        ExpectedQuantity = Convert.ToInt32(vals["Qty"])
                    }
                ]
            });
        });
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadReceiptsTabAsync();
    }

    private async Task ConfirmReceiptAsync()
    {
        var rec = _receiptsGrid.GetSelectedRow<WarehouseReceiptDto>();
        if (rec == null) return;
        var locs = await _warehouseApi.GetLocationsAsync(rec.WarehouseId.ToString(), null, 1, 100);
        var defaultLocId = locs.Items.FirstOrDefault()?.Id ?? 0;

        var firstItem = rec.Items.FirstOrDefault();
        if (firstItem == null)
        {
            await UiAsyncRunner.RunSafeAsync(this, async () =>
            {
                await _inventoryApi.ConfirmReceiptAsync(rec.Id, null);
                await LoadReceiptsTabAsync();
            }, null, null, _logger);
            return;
        }

        var dlg = new ModalFormDialog($"Xác nhận nhập phiếu {rec.ReceiptNumber}",
        [
            new() { Key = "Received", Label = $"SL thực nhận ({firstItem.ProductName})", Kind = ModalFieldKind.Integer, Required = true, InitialValue = firstItem.ExpectedQuantity },
            new() { Key = "Damaged", Label = "SL hư hỏng", Kind = ModalFieldKind.Integer, InitialValue = 0 },
            new() { Key = "LocationId", Label = "Vị trí lưu kho", Kind = ModalFieldKind.ComboBox, Required = true, InitialValue = firstItem.TargetLocationId ?? defaultLocId, Options = locs.Items.Select(l => new ModalComboOption(l.Id, $"{l.Code} ({l.ZoneCode})")).ToList() },
            new() { Key = "UnitCost", Label = "Đơn giá vốn", Kind = ModalFieldKind.Decimal, InitialValue = 10000m }
        ],
        async vals =>
        {
            await _inventoryApi.ConfirmReceiptAsync(rec.Id, new ConfirmReceiptRequest
            {
                Items =
                [
                    new ConfirmReceiptItemRequest
                    {
                        ItemId = firstItem.Id,
                        ReceivedQuantity = Convert.ToInt32(vals["Received"]),
                        DamagedQuantity = Convert.ToInt32(vals["Damaged"] ?? 0),
                        TargetLocationId = Convert.ToInt64(vals["LocationId"]),
                        UnitCost = Convert.ToDecimal(vals["UnitCost"] ?? 0m)
                    }
                ]
            });
        });
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadReceiptsTabAsync();
    }

    private async Task CreateAdjustmentAsync()
    {
        var warehouses = await _warehouseApi.GetWarehousesAsync(null, _branchContext.ActiveBranchId, null, true, 1, 100);
        var products = await _catalogApi.GetProductsAsync(null, null, true, new PagedRequest { Page = 1, PageSize = 100 });
        var wid = SelectedWarehouseId ?? warehouses.Items.FirstOrDefault()?.Id ?? 0;
        var locs = wid > 0 ? await _warehouseApi.GetLocationsAsync(wid.ToString(), null, 1, 100) : new PagedResponse<WarehouseLocationDto>();

        var dlg = new ModalFormDialog("Tạo điều chỉnh tồn kho",
        [
            new() { Key = "WarehouseId", Label = "Kho", Kind = ModalFieldKind.ComboBox, Required = true, InitialValue = wid > 0 ? wid : null, Options = warehouses.Items.Select(w => new ModalComboOption(w.Id, $"{w.Code} - {w.Name}")).ToList() },
            new() { Key = "ProductId", Label = "Sản phẩm", Kind = ModalFieldKind.ComboBox, Required = true, Options = products.Items.Select(p => new ModalComboOption(p.Id, $"{p.Sku} - {p.Name}")).ToList() },
            new() { Key = "LocationId", Label = "Vị trí", Kind = ModalFieldKind.ComboBox, Required = true, Options = locs.Items.Select(l => new ModalComboOption(l.Id, l.Code)).ToList() },
            new() { Key = "ReasonCode", Label = "Mã lý do", Kind = ModalFieldKind.ComboBox, InitialValue = "CountingVariance", Options = [new("CountingVariance", "Chênh lệch kiểm kê"), new("Damage", "Hư hỏng"), new("Found", "Tìm thấy"), new("Other", "Khác")] },
            new() { Key = "ReasonDesc", Label = "Diễn giải lý do", Required = true },
            new() { Key = "QtyChange", Label = "Số lượng +/-", Kind = ModalFieldKind.Integer, Required = true, InitialValue = 1 },
            new() { Key = "UnitCost", Label = "Đơn giá", Kind = ModalFieldKind.Decimal, InitialValue = 0m }
        ],
        async vals =>
        {
            await _inventoryApi.CreateAdjustmentAsync(new CreateStockAdjustmentRequest
            {
                WarehouseId = Convert.ToInt64(vals["WarehouseId"]),
                ProductId = Convert.ToInt64(vals["ProductId"]),
                LocationId = Convert.ToInt64(vals["LocationId"]),
                ReasonCode = vals["ReasonCode"]?.ToString() ?? "CountingVariance",
                ReasonDescription = vals["ReasonDesc"]?.ToString() ?? string.Empty,
                QuantityChange = Convert.ToInt32(vals["QtyChange"]),
                UnitCost = Convert.ToDecimal(vals["UnitCost"] ?? 0m)
            });
        });
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadMovementsTabAsync();
    }

    private async Task ApproveAdjustmentAsync()
    {
        var adj = _adjustmentsGrid.GetSelectedRow<StockAdjustmentDto>();
        if (adj == null) return;
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            await _inventoryApi.ApproveAdjustmentAsync(adj.Id, CurrentEmployeeId);
            await LoadMovementsTabAsync();
        }, null, null, _logger);
    }

    private async Task CreateTransferAsync()
    {
        var warehouses = await _warehouseApi.GetWarehousesAsync(null, null, null, true, 1, 100);
        var products = await _catalogApi.GetProductsAsync(null, null, true, new PagedRequest { Page = 1, PageSize = 100 });
        var srcWid = SelectedWarehouseId ?? warehouses.Items.FirstOrDefault()?.Id ?? 0;
        var locs = srcWid > 0 ? await _warehouseApi.GetLocationsAsync(srcWid.ToString(), null, 1, 100) : new PagedResponse<WarehouseLocationDto>();

        var dlg = new ModalFormDialog("Tạo lệnh chuyển kho",
        [
            new() { Key = "SrcWh", Label = "Kho nguồn", Kind = ModalFieldKind.ComboBox, Required = true, InitialValue = srcWid > 0 ? srcWid : null, Options = warehouses.Items.Select(w => new ModalComboOption(w.Id, $"{w.Code} - {w.Name}")).ToList() },
            new() { Key = "DstWh", Label = "Kho đích", Kind = ModalFieldKind.ComboBox, Required = true, Options = warehouses.Items.Select(w => new ModalComboOption(w.Id, $"{w.Code} - {w.Name}")).ToList() },
            new() { Key = "ProductId", Label = "Sản phẩm", Kind = ModalFieldKind.ComboBox, Required = true, Options = products.Items.Select(p => new ModalComboOption(p.Id, $"{p.Sku} - {p.Name}")).ToList() },
            new() { Key = "SrcLoc", Label = "Vị trí nguồn", Kind = ModalFieldKind.ComboBox, Required = true, Options = locs.Items.Select(l => new ModalComboOption(l.Id, l.Code)).ToList() },
            new() { Key = "Qty", Label = "Số lượng chuyển", Kind = ModalFieldKind.Integer, Required = true, InitialValue = 1 },
            new() { Key = "Notes", Label = "Ghi chú" }
        ],
        async vals =>
        {
            await _inventoryApi.CreateTransferAsync(new CreateStockTransferRequest
            {
                SourceWarehouseId = Convert.ToInt64(vals["SrcWh"]),
                DestinationWarehouseId = Convert.ToInt64(vals["DstWh"]),
                RequestedByEmployeeId = CurrentEmployeeId,
                ProductId = Convert.ToInt64(vals["ProductId"]),
                SourceLocationId = Convert.ToInt64(vals["SrcLoc"]),
                Quantity = Convert.ToInt32(vals["Qty"]),
                Notes = vals["Notes"]?.ToString()
            });
        });
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadMovementsTabAsync();
    }

    private async Task ConfirmTransferAsync()
    {
        var trf = _transfersGrid.GetSelectedRow<StockTransferDto>();
        if (trf == null) return;
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            await _inventoryApi.ConfirmTransferAsync(trf.Id, CurrentEmployeeId);
            await LoadMovementsTabAsync();
        }, null, null, _logger);
    }

    private async Task ReceiveTransferAsync()
    {
        var trf = _transfersGrid.GetSelectedRow<StockTransferDto>();
        if (trf == null) return;
        var locs = await _warehouseApi.GetLocationsAsync(trf.DestinationWarehouseId.ToString(), null, 1, 100);
        var dlg = new ModalFormDialog($"Nhận chuyển kho {trf.TransferNumber}",
        [
            new() { Key = "DestLoc", Label = "Vị trí nhận tại kho đích", Kind = ModalFieldKind.ComboBox, Required = true, Options = locs.Items.Select(l => new ModalComboOption(l.Id, $"{l.Code} ({l.ZoneCode})")).ToList() },
            new() { Key = "Notes", Label = "Ghi chú" }
        ],
        async vals =>
        {
            await _inventoryApi.ReceiveTransferAsync(trf.Id, new ReceiveStockTransferRequest
            {
                DestinationLocationId = Convert.ToInt64(vals["DestLoc"]),
                Notes = vals["Notes"]?.ToString()
            }, CurrentEmployeeId);
        });
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadMovementsTabAsync();
    }

    private async Task CreateReservationAsync()
    {
        var warehouses = await _warehouseApi.GetWarehousesAsync(null, _branchContext.ActiveBranchId, null, true, 1, 100);
        var products = await _catalogApi.GetProductsAsync(null, null, true, new PagedRequest { Page = 1, PageSize = 100 });
        var orders = await _salesApi.GetOrdersAsync(null, "Confirmed", null, null, _branchContext.ActiveBranchId, null, null, new PagedRequest { Page = 1, PageSize = 50 });
        var wid = SelectedWarehouseId ?? warehouses.Items.FirstOrDefault()?.Id ?? 0;
        var locs = wid > 0 ? await _warehouseApi.GetLocationsAsync(wid.ToString(), null, 1, 100) : new PagedResponse<WarehouseLocationDto>();

        var dlg = new ModalFormDialog("Tạo giữ hàng theo đơn",
        [
            new() { Key = "OrderId", Label = "Đơn hàng", Kind = ModalFieldKind.ComboBox, Required = true, Options = orders.Items.Select(o => new ModalComboOption(o.Id, $"{o.OrderNumber} - {o.CustomerName}")).ToList() },
            new() { Key = "WarehouseId", Label = "Kho", Kind = ModalFieldKind.ComboBox, Required = true, InitialValue = wid > 0 ? wid : null, Options = warehouses.Items.Select(w => new ModalComboOption(w.Id, $"{w.Code} - {w.Name}")).ToList() },
            new() { Key = "LocationId", Label = "Vị trí", Kind = ModalFieldKind.ComboBox, Required = true, Options = locs.Items.Select(l => new ModalComboOption(l.Id, l.Code)).ToList() },
            new() { Key = "ProductId", Label = "Sản phẩm", Kind = ModalFieldKind.ComboBox, Required = true, Options = products.Items.Select(p => new ModalComboOption(p.Id, $"{p.Sku} - {p.Name}")).ToList() },
            new() { Key = "Qty", Label = "Số lượng giữ", Kind = ModalFieldKind.Integer, Required = true, InitialValue = 1 }
        ],
        async vals =>
        {
            await _inventoryApi.CreateReservationAsync(new CreateInventoryReservationRequest
            {
                OrderId = Convert.ToInt64(vals["OrderId"]),
                WarehouseId = Convert.ToInt64(vals["WarehouseId"]),
                LocationId = Convert.ToInt64(vals["LocationId"]),
                ProductId = Convert.ToInt64(vals["ProductId"]),
                ReservedQuantity = Convert.ToInt32(vals["Qty"])
            });
        });
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadMovementsTabAsync();
    }

    private async Task HandleReservationActionAsync(bool release)
    {
        var row = _reservationsGrid.GetSelectedRow<InventoryReservationDto>();
        if (row == null) return;
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            if (release) await _inventoryApi.ReleaseReservationAsync(row.Id);
            else await _inventoryApi.ConsumeReservationAsync(row.Id);
            await LoadMovementsTabAsync();
        }, null, null, _logger);
    }

    private async Task CreateLotAsync()
    {
        var products = await _catalogApi.GetProductsAsync(null, null, true, new PagedRequest { Page = 1, PageSize = 100 });
        var dlg = new ModalFormDialog("Thêm lô hàng (Lot)",
        [
            new() { Key = "ProductId", Label = "Sản phẩm", Kind = ModalFieldKind.ComboBox, Required = true, Options = products.Items.Select(p => new ModalComboOption(p.Id, $"{p.Sku} - {p.Name}")).ToList() },
            new() { Key = "LotNumber", Label = "Số lô", Required = true },
            new() { Key = "VendorBatch", Label = "Mã batch NCC" },
            new() { Key = "Expiry", Label = "Hạn sử dụng", Kind = ModalFieldKind.Date, InitialValue = DateTime.Today.AddMonths(12) }
        ],
        async vals =>
        {
            await _inventoryApi.CreateLotAsync(new CreateInventoryLotRequest
            {
                ProductId = Convert.ToInt64(vals["ProductId"]),
                LotNumber = vals["LotNumber"]?.ToString() ?? string.Empty,
                VendorBatchCode = vals["VendorBatch"]?.ToString(),
                ExpiryDate = vals["Expiry"] is DateTime dt ? DateOnly.FromDateTime(dt) : null
            });
        });
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadTrackingTabAsync();
    }

    private async Task CreateSerialAsync()
    {
        var products = await _catalogApi.GetProductsAsync(null, null, true, new PagedRequest { Page = 1, PageSize = 100 });
        var warehouses = await _warehouseApi.GetWarehousesAsync(null, _branchContext.ActiveBranchId, null, true, 1, 100);
        var dlg = new ModalFormDialog("Thêm số Serial",
        [
            new() { Key = "ProductId", Label = "Sản phẩm", Kind = ModalFieldKind.ComboBox, Required = true, Options = products.Items.Select(p => new ModalComboOption(p.Id, $"{p.Sku} - {p.Name}")).ToList() },
            new() { Key = "WarehouseId", Label = "Kho", Kind = ModalFieldKind.ComboBox, Required = true, InitialValue = SelectedWarehouseId, Options = warehouses.Items.Select(w => new ModalComboOption(w.Id, $"{w.Code} - {w.Name}")).ToList() },
            new() { Key = "Serial", Label = "Số Serial", Required = true }
        ],
        async vals =>
        {
            await _inventoryApi.CreateSerialNumberAsync(new CreateInventorySerialNumberRequest
            {
                ProductId = Convert.ToInt64(vals["ProductId"]),
                WarehouseId = Convert.ToInt64(vals["WarehouseId"]),
                SerialNumber = vals["Serial"]?.ToString() ?? string.Empty
            });
        });
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadTrackingTabAsync();
    }
}
