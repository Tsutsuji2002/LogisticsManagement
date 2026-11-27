using Logistics.Client.Application.Contracts.Sales;
using Logistics.Client.Application.Interfaces;
using Logistics.Client.Application.Permissions;
using Logistics.Client.WinForms.Controls.Common;
using Logistics.Client.WinForms.Helpers;
using Logistics.Client.WinForms.Themes;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.Extensions.Logging;

namespace Logistics.Client.WinForms.Controls.Sales;

public sealed class SalesOrdersWorkspaceControl : ModuleViewBase
{
    private readonly ISalesApiClient _salesApi;
    private readonly ICrmApiClient _crmApi;
    private readonly ICatalogApiClient _catalogApi;
    private readonly IAuthorizationService _auth;
    private readonly IBranchContextService _branchContext;
    private readonly ILogger<SalesOrdersWorkspaceControl> _logger;

    private readonly PagedDataGridControl _ordersGrid;
    private readonly ComboBox _statusFilter = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140, Margin = new Padding(0, 4, 8, 0) };
    private readonly Label _detailHeaderLabel = new() { Dock = DockStyle.Top, Height = 58, BackColor = AppTheme.CardBg, Padding = new Padding(12, 8, 12, 6), Font = AppTheme.BodyFont };
    private readonly DataGridView _itemsGrid = new() { Dock = DockStyle.Fill };
    private readonly DataGridView _packagesGrid = new() { Dock = DockStyle.Fill };
    private readonly DataGridView _addressesGrid = new() { Dock = DockStyle.Fill };

    private OrderDetailDto? _selectedDetail;

    public SalesOrdersWorkspaceControl(
        ISalesApiClient salesApi,
        ICrmApiClient crmApi,
        ICatalogApiClient catalogApi,
        IAuthorizationService auth,
        IBranchContextService branchContext,
        ILogger<SalesOrdersWorkspaceControl> logger)
    {
        _salesApi = salesApi;
        _crmApi = crmApi;
        _catalogApi = catalogApi;
        _auth = auth;
        _branchContext = branchContext;
        _logger = logger;

        var split = AppTheme.CreateSafeSplitContainer(Orientation.Horizontal, 330, 200, 200);

        _ordersGrid = new PagedDataGridControl("Đơn hàng (Sales Orders)", "Tạo đơn, xác nhận và hủy đơn hàng theo vòng đời backend.");
        _statusFilter.Items.AddRange(["", "Draft", "Confirmed", "Processing", "PartiallyShipped", "Shipped", "Delivered", "Cancelled"]);
        _statusFilter.SelectedIndex = 0;
        _statusFilter.SelectedIndexChanged += (_, _) => _ordersGrid.ResetAndReload();
        _ordersGrid.AddFilterControl(_statusFilter);

        if (_auth.HasPermission(ClientPermissions.Sales.OrdersCreate))
        {
            var createBtn = AppTheme.CreatePrimaryButton("Tạo đơn hàng", 130);
            createBtn.Click += async (_, _) => await CreateOrderWizardAsync();
            _ordersGrid.AddActionButton(createBtn);
        }
        if (_auth.HasPermission(ClientPermissions.Sales.OrdersUpdate))
        {
            var confirmBtn = AppTheme.CreateSuccessButton("Xác nhận đơn", 120);
            confirmBtn.Click += async (_, _) => await ConfirmSelectedOrderAsync();
            _ordersGrid.AddActionButton(confirmBtn);
        }
        if (_auth.HasPermission(ClientPermissions.Sales.OrdersCancel))
        {
            var cancelBtn = AppTheme.CreateDangerButton("Hủy đơn", 95);
            cancelBtn.Click += async (_, _) => await CancelSelectedOrderAsync();
            _ordersGrid.AddActionButton(cancelBtn);
        }

        _ordersGrid.ReloadRequested += async (_, _) => await LoadOrdersAsync();
        _ordersGrid.SelectionChanged += async (_, _) => await LoadSelectedOrderDetailAsync();
        split.Panel1.Controls.Add(_ordersGrid);

        var detailTabs = new TabControl { Dock = DockStyle.Fill, Font = AppTheme.BodyFont };
        AppTheme.StyleDataGrid(_itemsGrid);
        AppTheme.StyleDataGrid(_packagesGrid);
        AppTheme.StyleDataGrid(_addressesGrid);

        var tItems = new TabPage("Sản phẩm (OrderItems)");
        var itemBar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 38, Padding = new Padding(4) };
        if (_auth.HasPermission(ClientPermissions.Sales.OrdersUpdate))
        {
            var addItemBtn = AppTheme.CreatePrimaryButton("+ Thêm dòng hàng", 135);
            addItemBtn.Click += async (_, _) => await AddItemToSelectedOrderAsync();
            itemBar.Controls.Add(addItemBtn);
        }
        tItems.Controls.Add(_itemsGrid);
        tItems.Controls.Add(itemBar);

        var tPkgs = new TabPage("Kiện hàng (OrderPackages)");
        var pkgBar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 38, Padding = new Padding(4) };
        if (_auth.HasPermission(ClientPermissions.Sales.OrdersUpdate))
        {
            var addPkgBtn = AppTheme.CreatePrimaryButton("+ Thêm kiện hàng", 135);
            addPkgBtn.Click += async (_, _) => await AddPackageToSelectedOrderAsync();
            pkgBar.Controls.Add(addPkgBtn);
        }
        tPkgs.Controls.Add(_packagesGrid);
        tPkgs.Controls.Add(pkgBar);

        var tAddr = new TabPage("Địa chỉ lấy / giao");
        tAddr.Controls.Add(_addressesGrid);

        detailTabs.TabPages.AddRange([tItems, tPkgs, tAddr]);
        split.Panel2.Padding = new Padding(12, 4, 12, 12);
        split.Panel2.Controls.Add(detailTabs);
        split.Panel2.Controls.Add(_detailHeaderLabel);

        Controls.Add(split);
    }

    protected override Task RefreshDataAsync() => LoadOrdersAsync();

    private async Task LoadOrdersAsync()
    {
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            _ordersGrid.SetLoading(true);
            var st = string.IsNullOrWhiteSpace(_statusFilter.SelectedItem?.ToString()) ? null : _statusFilter.SelectedItem?.ToString();
            var paged = await _salesApi.GetOrdersAsync(_ordersGrid.SearchText, st, null, null, _branchContext.ActiveBranchId, null, null,
                new PagedRequest { Page = _ordersGrid.CurrentPage, PageSize = _ordersGrid.CurrentPageSize });
            _ordersGrid.BindPagedData(paged);
        }, logger: _logger, showMessageBoxOnError: false);
        _ordersGrid.SetLoading(false);
    }

    private async Task LoadSelectedOrderDetailAsync()
    {
        var row = _ordersGrid.GetSelectedRow<OrderDto>();
        if (row == null) { _selectedDetail = null; return; }
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            _selectedDetail = await _salesApi.GetOrderDetailAsync(row.Id.ToString());
            _detailHeaderLabel.Text =
                $"Đơn {_selectedDetail.OrderNumber} | Trạng thái: {_selectedDetail.OrderStatus} | Thanh toán: {_selectedDetail.PaymentStatus} | KH: {_selectedDetail.CustomerName}\n" +
                $"Trọng lượng: {_selectedDetail.TotalWeightKg:N2} kg | Khai báo: {AppTheme.FormatMoney(_selectedDetail.DeclaredValue, _selectedDetail.CurrencyCode)} | COD: {AppTheme.FormatMoney(_selectedDetail.CodAmount, _selectedDetail.CurrencyCode)}";
            _itemsGrid.DataSource = _selectedDetail.Items.ToList();
            _packagesGrid.DataSource = _selectedDetail.Packages.ToList();
            _addressesGrid.DataSource = _selectedDetail.Addresses.ToList();
        }, logger: _logger, showMessageBoxOnError: false);
    }

    private async Task CreateOrderWizardAsync()
    {
        var customers = await _crmApi.GetCustomersAsync(null, null, true, _branchContext.ActiveBranchId, new PagedRequest { Page = 1, PageSize = 100 });
        var products = await _catalogApi.GetProductsAsync(null, null, true, new PagedRequest { Page = 1, PageSize = 100 });
        var branches = _branchContext.AccessibleBranches.Select(b => new ModalComboOption(b.BranchId, $"{b.Code} - {b.Name}")).ToList();

        if (customers.Items.Count == 0)
        {
            UiAsyncRunner.ShowInfo(this, "Không có khách hàng active trong chi nhánh để tạo đơn.", "Thông báo");
            return;
        }

        var dlg = new ModalFormDialog("Tạo đơn hàng mới (Draft)", [
            new ModalFieldSpec { Key = "CustomerId", Label = "Khách hàng", Kind = ModalFieldKind.ComboBox, Required = true,
                Options = customers.Items.Select(c => new ModalComboOption(c.Id, $"{c.CustomerCode} - {c.Name}")).ToList() },
            new ModalFieldSpec { Key = "BranchId", Label = "Chi nhánh", Kind = ModalFieldKind.ComboBox, Required = true, Options = branches, InitialValue = _branchContext.ActiveBranchId },
            new ModalFieldSpec { Key = "ProductId", Label = "Sản phẩm chính", Kind = ModalFieldKind.ComboBox,
                Options = products.Items.Select(p => new ModalComboOption(p.Id, $"{p.Sku} - {p.Name}")).Prepend(new ModalComboOption(null, "(Nhập tay tên hàng)")).ToList() },
            new ModalFieldSpec { Key = "CustomProductName", Label = "Tên hàng (nếu nhập tay)", InitialValue = "Hàng hóa tiêu chuẩn" },
            new ModalFieldSpec { Key = "Quantity", Label = "Số lượng", Kind = ModalFieldKind.Integer, Required = true, InitialValue = 1 },
            new ModalFieldSpec { Key = "WeightKg", Label = "Cân nặng kiện (kg)", Kind = ModalFieldKind.Decimal, Required = true, InitialValue = 1.5m },
            new ModalFieldSpec { Key = "DeclaredValue", Label = "Giá trị khai báo", Kind = ModalFieldKind.Decimal, InitialValue = 500000m },
            new ModalFieldSpec { Key = "CodAmount", Label = "Tiền thu hộ (COD)", Kind = ModalFieldKind.Decimal, InitialValue = 0m },
            new ModalFieldSpec { Key = "SenderName", Label = "Người gửi", Required = true, InitialValue = "Kho gửi" },
            new ModalFieldSpec { Key = "SenderPhone", Label = "SĐT người gửi", Required = true, InitialValue = "0900000001" },
            new ModalFieldSpec { Key = "SenderAddress", Label = "Địa chỉ lấy hàng", Required = true, InitialValue = "Quận 1, TP.HCM" },
            new ModalFieldSpec { Key = "RecipientName", Label = "Người nhận", Required = true, InitialValue = "Khách nhận" },
            new ModalFieldSpec { Key = "RecipientPhone", Label = "SĐT người nhận", Required = true, InitialValue = "0900000002" },
            new ModalFieldSpec { Key = "RecipientAddress", Label = "Địa chỉ giao hàng", Required = true, InitialValue = "Quận Hoàn Kiếm, Hà Nội" }
        ], async v =>
        {
            var prodId = v["ProductId"] as long?;
            var qty = Math.Max(1, Convert.ToInt32(v["Quantity"]));
            var wt = Convert.ToDecimal(v["WeightKg"]);
            var val = Convert.ToDecimal(v["DeclaredValue"]);

            var req = new CreateOrderRequest
            {
                CustomerId = Convert.ToInt64(v["CustomerId"]),
                BranchId = Convert.ToInt64(v["BranchId"]),
                CodAmount = Convert.ToDecimal(v["CodAmount"]),
                PickupAddress = new OrderAddressRequest { ContactName = v["SenderName"]!.ToString()!, PhoneNumber = v["SenderPhone"]!.ToString()!, AddressLine1 = v["SenderAddress"]!.ToString()! },
                DeliveryAddress = new OrderAddressRequest { ContactName = v["RecipientName"]!.ToString()!, PhoneNumber = v["RecipientPhone"]!.ToString()!, AddressLine1 = v["RecipientAddress"]!.ToString()! },
                Items = [new CreateOrderItemRequest { ProductId = prodId, ProductName = v["CustomProductName"]?.ToString(), Quantity = qty, UnitPrice = val, DeclaredValue = val, WeightKg = wt }],
                Packages = [new CreateOrderPackageRequest { WeightKg = wt, LengthCm = 20, WidthCm = 15, HeightCm = 10, DeclaredValue = val, Description = "Kiện 1" }]
            };
            var created = await _salesApi.CreateOrderAsync(req);
            UiAsyncRunner.ShowInfo(this, $"Đã tạo đơn hàng {created.OrderNumber} (Trạng thái: {created.OrderStatus}).");
        });
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadOrdersAsync();
    }

    private async Task ConfirmSelectedOrderAsync()
    {
        var row = _ordersGrid.GetSelectedRow<OrderDto>();
        if (row == null) return;
        if (!UiAsyncRunner.Confirm(this, $"Xác nhận đơn hàng {row.OrderNumber}?")) return;
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            await _salesApi.ConfirmOrderAsync(row.Id.ToString());
            await LoadOrdersAsync();
            await LoadSelectedOrderDetailAsync();
        }, logger: _logger);
    }

    private async Task CancelSelectedOrderAsync()
    {
        var row = _ordersGrid.GetSelectedRow<OrderDto>();
        if (row == null) return;
        var dlg = new ModalFormDialog($"Hủy đơn {row.OrderNumber}", [
            new ModalFieldSpec { Key = "Reason", Label = "Lý do hủy đơn", Required = true, Kind = ModalFieldKind.Multiline }
        ], async v => await _salesApi.CancelOrderAsync(row.Id.ToString(), new CancelOrderRequest { Reason = v["Reason"]!.ToString()! }));
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadOrdersAsync();
    }

    private async Task AddItemToSelectedOrderAsync()
    {
        if (_selectedDetail == null) return;
        var dlg = new ModalFormDialog("Thêm sản phẩm vào đơn", [
            new ModalFieldSpec { Key = "ProductName", Label = "Tên hàng", Required = true },
            new ModalFieldSpec { Key = "Quantity", Label = "Số lượng", Kind = ModalFieldKind.Integer, InitialValue = 1, Required = true },
            new ModalFieldSpec { Key = "UnitPrice", Label = "Đơn giá", Kind = ModalFieldKind.Decimal, InitialValue = 100000m },
            new ModalFieldSpec { Key = "WeightKg", Label = "Cân nặng (kg)", Kind = ModalFieldKind.Decimal, InitialValue = 1m }
        ], async v => await _salesApi.AddOrderItemAsync(_selectedDetail.Id.ToString(), new CreateOrderItemRequest
        {
            ProductName = v["ProductName"]!.ToString()!, Quantity = Math.Max(1, Convert.ToInt32(v["Quantity"])),
            UnitPrice = Convert.ToDecimal(v["UnitPrice"]), DeclaredValue = Convert.ToDecimal(v["UnitPrice"]), WeightKg = Convert.ToDecimal(v["WeightKg"])
        }));
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadSelectedOrderDetailAsync();
    }

    private async Task AddPackageToSelectedOrderAsync()
    {
        if (_selectedDetail == null) return;
        var dlg = new ModalFormDialog("Thêm kiện hàng vào đơn", [
            new ModalFieldSpec { Key = "WeightKg", Label = "Cân nặng (kg)", Kind = ModalFieldKind.Decimal, InitialValue = 1m, Required = true },
            new ModalFieldSpec { Key = "LengthCm", Label = "Dài (cm)", Kind = ModalFieldKind.Decimal, InitialValue = 20m },
            new ModalFieldSpec { Key = "WidthCm", Label = "Rộng (cm)", Kind = ModalFieldKind.Decimal, InitialValue = 15m },
            new ModalFieldSpec { Key = "HeightCm", Label = "Cao (cm)", Kind = ModalFieldKind.Decimal, InitialValue = 10m },
            new ModalFieldSpec { Key = "DeclaredValue", Label = "Giá trị khai báo", Kind = ModalFieldKind.Decimal, InitialValue = 0m }
        ], async v => await _salesApi.AddOrderPackageAsync(_selectedDetail.Id.ToString(), new CreateOrderPackageRequest
        {
            WeightKg = Convert.ToDecimal(v["WeightKg"]), LengthCm = Convert.ToDecimal(v["LengthCm"]), WidthCm = Convert.ToDecimal(v["WidthCm"]),
            HeightCm = Convert.ToDecimal(v["HeightCm"]), DeclaredValue = Convert.ToDecimal(v["DeclaredValue"])
        }));
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadSelectedOrderDetailAsync();
    }
}
