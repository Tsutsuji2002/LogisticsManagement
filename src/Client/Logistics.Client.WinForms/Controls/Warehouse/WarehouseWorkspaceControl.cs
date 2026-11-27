using Logistics.Client.Application.Contracts.Warehouse;
using Logistics.Client.Application.Interfaces;
using Logistics.Client.Application.Permissions;
using Logistics.Client.WinForms.Controls.Common;
using Logistics.Client.WinForms.Helpers;
using Logistics.Client.WinForms.Themes;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.Extensions.Logging;

namespace Logistics.Client.WinForms.Controls.Warehouse;

public sealed class WarehouseWorkspaceControl : ModuleViewBase
{
    private readonly IWarehouseApiClient _warehouseApi;
    private readonly IOrganizationApiClient _orgApi;
    private readonly ICarrierApiClient _carrierApi;
    private readonly IAuthenticationService _authService;
    private readonly IAuthorizationService _auth;
    private readonly IBranchContextService _branchContext;
    private readonly ILogger<WarehouseWorkspaceControl> _logger;

    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill, Font = AppTheme.BodyFont };
    private readonly TabPage _tabFacilities = new("Kho bãi, Khu vực & Vị trí") { BackColor = AppTheme.SurfaceBg };
    private readonly TabPage _tabParcelHub = new("Quét & Xử lý bưu kiện (Parcel Hub)") { BackColor = AppTheme.SurfaceBg };
    private readonly TabPage _tabDispatches = new("Phiếu xuất kho / Hub") { BackColor = AppTheme.SurfaceBg };

    private PagedDataGridControl _warehousesGrid = null!;
    private readonly DataGridView _zonesGrid = new() { Dock = DockStyle.Fill };
    private readonly DataGridView _locationsGrid = new() { Dock = DockStyle.Fill };
    private WarehouseDto? _selectedWarehouse;
    private WarehouseZoneDto? _selectedZone;

    private ComboBox _hubWarehouseCombo = null!;
    private TextBox _scanInputBox = null!;
    private Label _scanResultLabel = null!;
    private PagedDataGridControl _parcelEventsGrid = null!;
    private PagedDataGridControl _dispatchesGrid = null!;

    public WarehouseWorkspaceControl(
        IWarehouseApiClient warehouseApi,
        IOrganizationApiClient orgApi,
        ICarrierApiClient carrierApi,
        IAuthenticationService authService,
        IAuthorizationService auth,
        IBranchContextService branchContext,
        ILogger<WarehouseWorkspaceControl> logger)
    {
        _warehouseApi = warehouseApi;
        _orgApi = orgApi;
        _carrierApi = carrierApi;
        _authService = authService;
        _auth = auth;
        _branchContext = branchContext;
        _logger = logger;

        BuildFacilitiesTab();
        BuildParcelHubTab();
        BuildDispatchesTab();

        if (_auth.HasAnyPermission(ClientPermissions.Warehouse.WarehousesRead, ClientPermissions.Warehouse.WarehousesManage))
            _tabs.TabPages.Add(_tabFacilities);
        if (_auth.HasAnyPermission(ClientPermissions.Warehouse.OperationsRead, ClientPermissions.Warehouse.SortingManage))
            _tabs.TabPages.Add(_tabParcelHub);
        if (_auth.HasAnyPermission(ClientPermissions.Warehouse.OperationsRead, ClientPermissions.Warehouse.DispatchesManage))
            _tabs.TabPages.Add(_tabDispatches);

        _tabs.SelectedIndexChanged += async (_, _) => await RefreshDataAsync();
        Controls.Add(_tabs);
    }

    public void SelectSection(string navKey)
    {
        var tab = navKey switch
        {
            "Warehouse.ParcelHub" => _tabParcelHub,
            "Warehouse.Dispatches" => _tabDispatches,
            _ => _tabFacilities
        };
        if (_tabs.TabPages.Contains(tab)) _tabs.SelectedTab = tab;
    }

    private void BuildFacilitiesTab()
    {
        var split = AppTheme.CreateSafeSplitContainer(Orientation.Horizontal, 280, 180, 180);
        _warehousesGrid = new PagedDataGridControl("Kho bãi & Hub trung chuyển", "Quản lý kho, khu vực phân loại (Zone) và vị trí chứa (Location).");

        if (_auth.HasPermission(ClientPermissions.Warehouse.WarehousesManage))
        {
            var addBtn = AppTheme.CreatePrimaryButton("Thêm kho", 105);
            addBtn.Click += async (_, _) => await CreateWarehouseAsync();
            var toggleBtn = AppTheme.CreateDangerButton("Bật/Tắt kho", 110);
            toggleBtn.Click += async (_, _) =>
            {
                var w = _warehousesGrid.GetSelectedRow<WarehouseDto>();
                if (w == null) return;
                await UiAsyncRunner.RunSafeAsync(this, async () =>
                {
                    await _warehouseApi.ToggleWarehouseStatusAsync(w.Id.ToString(), !w.IsActive);
                    await RefreshDataAsync();
                }, toggleBtn, null, _logger);
            };
            _warehousesGrid.AddActionButton(addBtn);
            _warehousesGrid.AddActionButton(toggleBtn);
        }

        _warehousesGrid.ReloadRequested += async (_, _) => await LoadWarehousesAsync();
        _warehousesGrid.SelectionChanged += async (_, _) =>
        {
            _selectedWarehouse = _warehousesGrid.GetSelectedRow<WarehouseDto>();
            await LoadZonesAndLocationsAsync();
        };

        var detailSplit = AppTheme.CreateSafeSplitContainer(Orientation.Vertical, 420);
        detailSplit.Panel1.Controls.Add(BuildSubPanel("Khu vực (Zones)", _zonesGrid,
            _auth.HasPermission(ClientPermissions.Warehouse.ZonesManage) ? ("Thêm Zone", CreateZoneAsync) : null));
        detailSplit.Panel2.Controls.Add(BuildSubPanel("Vị trí (Locations)", _locationsGrid,
            _auth.HasPermission(ClientPermissions.Warehouse.LocationsManage) ? ("Thêm Location", CreateLocationAsync) : null,
            _auth.HasPermission(ClientPermissions.Warehouse.LocationsManage) ? ("Khóa/Mở Location", ToggleLocationBlockAsync) : null));

        AppTheme.StyleDataGrid(_zonesGrid);
        AppTheme.StyleDataGrid(_locationsGrid);
        _zonesGrid.SelectionChanged += async (_, _) =>
        {
            _selectedZone = _zonesGrid.CurrentRow?.DataBoundItem as WarehouseZoneDto;
            await LoadLocationsOnlyAsync();
        };

        split.Panel1.Controls.Add(_warehousesGrid);
        split.Panel2.Controls.Add(detailSplit);
        _tabFacilities.Controls.Add(split);
    }

    private void BuildParcelHubTab()
    {
        var split = AppTheme.CreateSafeSplitContainer(Orientation.Horizontal, 190, 150);
        var hubCard = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.CardBg, Padding = new Padding(16) };

        var header = new Label
        {
            Text = "Xử lý bưu kiện qua Hub (Tách biệt hoàn toàn khỏi Tồn kho vật tư / SKU)",
            Font = AppTheme.SectionTitleFont,
            ForeColor = AppTheme.TextPrimary,
            Dock = DockStyle.Top,
            Height = 28
        };

        var controlsFlow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 52, WrapContents = true, Padding = new Padding(0, 6, 0, 0) };
        _hubWarehouseCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220, Font = AppTheme.BodyFont };
        _scanInputBox = new TextBox { PlaceholderText = "Nhập Tracking / Barcode bưu kiện...", Width = 260, Font = AppTheme.BodyFont };
        var scanBtn = AppTheme.CreatePrimaryButton("Quét bưu kiện", 130);
        scanBtn.Click += async (_, _) => await ScanParcelAsync();
        controlsFlow.Controls.AddRange([_hubWarehouseCombo, _scanInputBox, scanBtn]);

        var opsFlow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, WrapContents = true };
        var recvBtn = AppTheme.CreateSuccessButton("1. Nhập Hub (Receive)", 155);
        recvBtn.Click += async (_, _) => await ReceiveParcelAsync();
        var sortBtn = AppTheme.CreateSecondaryButton("2. Phân loại (Sort)", 155);
        sortBtn.Click += async (_, _) => await SortParcelAsync();
        var stageBtn = AppTheme.CreateSecondaryButton("3. Xếp vị trí (Stage)", 160);
        stageBtn.Click += async (_, _) => await StageParcelAsync();
        var excBtn = AppTheme.CreateDangerButton("Báo sự cố (Exception)", 175);
        excBtn.Click += async (_, _) => await RecordParcelExceptionAsync();

        recvBtn.Enabled = _auth.HasPermission(ClientPermissions.Warehouse.ReceiptsManage) || _auth.HasPermission(ClientPermissions.Warehouse.SortingManage);
        sortBtn.Enabled = _auth.HasPermission(ClientPermissions.Warehouse.SortingManage);
        stageBtn.Enabled = _auth.HasPermission(ClientPermissions.Warehouse.SortingManage);
        excBtn.Enabled = _auth.HasPermission(ClientPermissions.Warehouse.SortingManage);

        opsFlow.Controls.AddRange([recvBtn, sortBtn, stageBtn, excBtn]);

        _scanResultLabel = new Label
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.BodyFont,
            ForeColor = AppTheme.TextSecondary,
            Text = "Quét mã vận đơn hoặc barcode kiện hàng để xem trạng thái và thao tác tiếp theo được phép."
        };

        hubCard.Controls.Add(_scanResultLabel);
        hubCard.Controls.Add(opsFlow);
        hubCard.Controls.Add(controlsFlow);
        hubCard.Controls.Add(header);

        _parcelEventsGrid = new PagedDataGridControl("Lịch sử quét & xử lý bưu kiện (ParcelHandlingEvents)", "Theo dõi mọi sự kiện nhập hub, phân loại, đóng bao và ngoại lệ.");
        _parcelEventsGrid.ReloadRequested += async (_, _) => await LoadParcelEventsAsync();

        split.Panel1.Controls.Add(hubCard);
        split.Panel2.Controls.Add(_parcelEventsGrid);
        _tabParcelHub.Controls.Add(split);
    }

    private void BuildDispatchesTab()
    {
        _dispatchesGrid = new PagedDataGridControl("Phiếu xuất kho / Trung chuyển (WarehouseDispatches)", "Tạo phiếu xuất tuyến Hub-to-Hub / Linehaul / Bàn giao ĐVVC và xác nhận xuất kho.");
        if (_auth.HasPermission(ClientPermissions.Warehouse.DispatchesManage))
        {
            var createBtn = AppTheme.CreatePrimaryButton("Tạo phiếu xuất", 130);
            createBtn.Click += async (_, _) => await CreateDispatchAsync();
            var confirmBtn = AppTheme.CreateSuccessButton("Xác nhận xuất", 130);
            confirmBtn.Click += async (_, _) => await ConfirmDispatchAsync();
            _dispatchesGrid.AddActionButton(createBtn);
            _dispatchesGrid.AddActionButton(confirmBtn);
        }
        _dispatchesGrid.ReloadRequested += async (_, _) => await LoadDispatchesAsync();
        _tabDispatches.Controls.Add(_dispatchesGrid);
    }

    private static Control BuildSubPanel(
        string title,
        DataGridView grid,
        (string Text, Func<Task> Handler)? action1 = null,
        (string Text, Func<Task> Handler)? action2 = null)
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.CardBg, Padding = new Padding(10) };
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 38 };
        top.Controls.Add(new Label { Text = title, Font = AppTheme.SectionTitleFont, AutoSize = true, Margin = new Padding(0, 8, 12, 0) });
        if (action1.HasValue)
        {
            var b1 = AppTheme.CreatePrimaryButton(action1.Value.Text, 120);
            b1.Click += async (_, _) => await action1.Value.Handler();
            top.Controls.Add(b1);
        }
        if (action2.HasValue)
        {
            var b2 = AppTheme.CreateSecondaryButton(action2.Value.Text, 130);
            b2.Click += async (_, _) => await action2.Value.Handler();
            top.Controls.Add(b2);
        }
        panel.Controls.Add(grid);
        panel.Controls.Add(top);
        return panel;
    }

    protected override async Task RefreshDataAsync()
    {
        await EnsureWarehouseComboPopulatedAsync();
        if (_tabs.SelectedTab == _tabFacilities) await LoadWarehousesAsync();
        else if (_tabs.SelectedTab == _tabParcelHub) await LoadParcelEventsAsync();
        else if (_tabs.SelectedTab == _tabDispatches) await LoadDispatchesAsync();
    }

    private async Task EnsureWarehouseComboPopulatedAsync()
    {
        var list = await _warehouseApi.GetWarehousesAsync(null, _branchContext.ActiveBranchId, null, true, 1, 100);
        var prev = (_hubWarehouseCombo.SelectedItem as ModalComboOption)?.Value;
        _hubWarehouseCombo.Items.Clear();
        foreach (var w in list.Items)
            _hubWarehouseCombo.Items.Add(new ModalComboOption(w.Id, $"{w.Code} - {w.Name} ({w.Type})"));
        if (_hubWarehouseCombo.Items.Count > 0)
        {
            if (prev != null)
            {
                foreach (ModalComboOption item in _hubWarehouseCombo.Items)
                {
                    if (Equals(item.Value, prev)) { _hubWarehouseCombo.SelectedItem = item; return; }
                }
            }
            _hubWarehouseCombo.SelectedIndex = 0;
        }
    }

    private async Task LoadWarehousesAsync()
    {
        _warehousesGrid.SetLoading(true);
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            var page = await _warehouseApi.GetWarehousesAsync(_warehousesGrid.SearchText, _branchContext.ActiveBranchId, null, null, _warehousesGrid.CurrentPage, _warehousesGrid.CurrentPageSize);
            _warehousesGrid.BindPagedData(page);
            if (_warehousesGrid.Grid.Rows.Count > 0)
            {
                _selectedWarehouse = _warehousesGrid.GetSelectedRow<WarehouseDto>();
                await LoadZonesAndLocationsAsync();
            }
        }, null, null, _logger);
        _warehousesGrid.SetLoading(false);
    }

    private async Task LoadZonesAndLocationsAsync()
    {
        if (_selectedWarehouse == null)
        {
            _zonesGrid.DataSource = null;
            _locationsGrid.DataSource = null;
            return;
        }
        var zones = await _warehouseApi.GetZonesAsync(_selectedWarehouse.Id.ToString());
        _zonesGrid.DataSource = zones.ToList();
        _selectedZone = zones.FirstOrDefault();
        await LoadLocationsOnlyAsync();
    }

    private async Task LoadLocationsOnlyAsync()
    {
        if (_selectedWarehouse == null) return;
        var locs = await _warehouseApi.GetLocationsAsync(_selectedWarehouse.Id.ToString(), _selectedZone?.Id, 1, 100);
        _locationsGrid.DataSource = locs.Items.ToList();
    }

    private async Task CreateWarehouseAsync()
    {
        var branches = _branchContext.AccessibleBranches;
        if (branches.Count == 0)
        {
            var brPage = await _orgApi.GetBranchesAsync(null, true, null, new PagedRequest { Page = 1, PageSize = 50 });
            branches = brPage.Items.Select(b => new Application.Contracts.Auth.AuthorizedBranchDto(b.Id, b.PublicId, b.Code, b.Name, false)).ToList();
        }
        var dlg = new ModalFormDialog("Tạo kho bãi mới",
        [
            new() { Key = "BranchId", Label = "Chi nhánh", Kind = ModalFieldKind.ComboBox, Required = true, InitialValue = _branchContext.ActiveBranchId ?? branches.FirstOrDefault()?.BranchId, Options = branches.Select(b => new ModalComboOption(b.BranchId, $"{b.Code} - {b.Name}")).ToList() },
            new() { Key = "Code", Label = "Mã kho", Required = true },
            new() { Key = "Name", Label = "Tên kho", Required = true },
            new() { Key = "Type", Label = "Loại kho", Kind = ModalFieldKind.ComboBox, InitialValue = "Hybrid", Options = [new("Fulfillment", "Fulfillment (Lưu kho)"), new("SortingHub", "SortingHub (Trung chuyển)"), new("CrossDock", "CrossDock"), new("Hybrid", "Hybrid (Hỗn hợp)")] },
            new() { Key = "Area", Label = "Diện tích (m2)", Kind = ModalFieldKind.Decimal, InitialValue = 500m }
        ],
        async vals =>
        {
            await _warehouseApi.CreateWarehouseAsync(new CreateWarehouseRequest
            {
                BranchId = Convert.ToInt64(vals["BranchId"]),
                Code = vals["Code"]?.ToString() ?? string.Empty,
                Name = vals["Name"]?.ToString() ?? string.Empty,
                Type = vals["Type"]?.ToString() ?? "Hybrid",
                TotalAreaSqM = Convert.ToDecimal(vals["Area"] ?? 0m)
            });
        });
        if (dlg.ShowDialog(this) == DialogResult.OK) await RefreshDataAsync();
    }

    private async Task CreateZoneAsync()
    {
        if (_selectedWarehouse == null) return;
        var dlg = new ModalFormDialog($"Thêm Zone cho kho {_selectedWarehouse.Code}",
        [
            new() { Key = "Code", Label = "Mã Zone", Required = true },
            new() { Key = "Name", Label = "Tên Zone", Required = true },
            new() { Key = "ZoneType", Label = "Loại khu vực", Kind = ModalFieldKind.ComboBox, InitialValue = "BulkStorage", Options = [new("Receiving", "Receiving"), new("Sorting", "Sorting"), new("BulkStorage", "BulkStorage"), new("PickFace", "PickFace"), new("Staging", "Staging"), new("Dispatch", "Dispatch"), new("Quarantine", "Quarantine")] },
            new() { Key = "Temp", Label = "Nhiệt độ", Kind = ModalFieldKind.ComboBox, InitialValue = "Ambient", Options = [new("Ambient", "Ambient"), new("Chilled", "Chilled"), new("Frozen", "Frozen")] }
        ],
        async vals =>
        {
            await _warehouseApi.CreateZoneAsync(_selectedWarehouse.Id.ToString(), new CreateWarehouseZoneRequest
            {
                Code = vals["Code"]?.ToString() ?? string.Empty,
                Name = vals["Name"]?.ToString() ?? string.Empty,
                ZoneType = vals["ZoneType"]?.ToString() ?? "BulkStorage",
                TemperatureType = vals["Temp"]?.ToString() ?? "Ambient"
            });
        });
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadZonesAndLocationsAsync();
    }

    private async Task CreateLocationAsync()
    {
        if (_selectedWarehouse == null || _selectedZone == null) { UiAsyncRunner.ShowInfo(this, "Vui lòng chọn kho và Zone trước.", "Thông báo"); return; }
        var dlg = new ModalFormDialog($"Thêm Location trong {_selectedZone.Code}",
        [
            new() { Key = "Code", Label = "Mã vị trí", Required = true },
            new() { Key = "Barcode", Label = "Barcode vị trí", Required = true },
            new() { Key = "Aisle", Label = "Dãy (Aisle)" },
            new() { Key = "Rack", Label = "Kệ (Rack)" },
            new() { Key = "MaxWeight", Label = "Tải trọng tối đa (kg)", Kind = ModalFieldKind.Decimal, InitialValue = 1000m }
        ],
        async vals =>
        {
            await _warehouseApi.CreateLocationAsync(_selectedWarehouse.Id.ToString(), _selectedZone.Id, new CreateWarehouseLocationRequest
            {
                Code = vals["Code"]?.ToString() ?? string.Empty,
                Barcode = vals["Barcode"]?.ToString() ?? string.Empty,
                Aisle = vals["Aisle"]?.ToString(),
                Rack = vals["Rack"]?.ToString(),
                MaxWeightKg = Convert.ToDecimal(vals["MaxWeight"] ?? 1000m)
            });
        });
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadLocationsOnlyAsync();
    }

    private async Task ToggleLocationBlockAsync()
    {
        if (_selectedWarehouse == null) return;
        if (_locationsGrid.CurrentRow?.DataBoundItem is not WarehouseLocationDto loc) return;
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            await _warehouseApi.ToggleLocationBlockAsync(_selectedWarehouse.Id.ToString(), loc.Id, !loc.IsBlocked);
            await LoadLocationsOnlyAsync();
        }, null, null, _logger);
    }

    private long ResolveWarehouseIdForHub()
        => Convert.ToInt64((_hubWarehouseCombo.SelectedItem as ModalComboOption)?.Value ?? 0L);

    private long ResolveCurrentEmployeeId() => _authService.CurrentUser?.EmployeeId ?? 0L;

    private async Task ScanParcelAsync()
    {
        var wid = ResolveWarehouseIdForHub();
        var empId = ResolveCurrentEmployeeId();
        var code = _scanInputBox.Text.Trim();
        if (wid <= 0 || string.IsNullOrWhiteSpace(code)) return;

        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            var res = await _warehouseApi.ScanParcelAsync(new ScanPackageRequest
            {
                WarehouseId = wid,
                EmployeeId = empId,
                TrackingNumberOrBarcode = code,
                DeviceId = Environment.MachineName
            });
            _scanResultLabel.ForeColor = AppTheme.TextPrimary;
            _scanResultLabel.Text =
                $"Vận đơn: {res.ShipmentTrackingNumber} ({res.ShipmentStatus})  |  Kiện: {res.PackageCode} ({res.PackageStatus})  |  Khối lượng: {res.WeightKg:N2} kg\n" +
                $"Thao tác tiếp theo hợp lệ: {res.PermittedNextOperation}  |  {res.Message}";
            await LoadParcelEventsAsync();
        }, null, null, _logger);
    }

    private async Task ReceiveParcelAsync()
    {
        var wid = ResolveWarehouseIdForHub();
        var code = _scanInputBox.Text.Trim();
        if (wid <= 0 || string.IsNullOrWhiteSpace(code)) return;
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            await _warehouseApi.ReceiveParcelAsync(new ParcelReceiveRequest
            {
                WarehouseId = wid,
                EmployeeId = ResolveCurrentEmployeeId(),
                TrackingNumberOrBarcode = code,
                Notes = "Nhận tại Hub từ WinForms"
            });
            await LoadParcelEventsAsync();
            UiAsyncRunner.ShowInfo(this, $"Đã ghi nhận nhận kiện {code} tại Hub.", "Thành công");
        }, null, null, _logger);
    }

    private async Task SortParcelAsync()
    {
        var wid = ResolveWarehouseIdForHub();
        var code = _scanInputBox.Text.Trim();
        if (wid <= 0 || string.IsNullOrWhiteSpace(code)) return;
        var zones = await _warehouseApi.GetZonesAsync(wid.ToString());
        var dlg = new ModalFormDialog($"Phân loại bưu kiện {code}",
        [
            new() { Key = "ZoneId", Label = "Khu vực phân loại (Zone)", Kind = ModalFieldKind.ComboBox, Required = true, Options = zones.Select(z => new ModalComboOption(z.Id, $"{z.Code} - {z.Name}")).ToList() },
            new() { Key = "Container", Label = "Mã bao / rổ (Container)" },
            new() { Key = "Notes", Label = "Ghi chú" }
        ],
        async vals =>
        {
            await _warehouseApi.SortParcelAsync(new ParcelSortRequest
            {
                TrackingNumberOrBarcode = code,
                WarehouseId = wid,
                ZoneId = Convert.ToInt64(vals["ZoneId"]),
                EmployeeId = ResolveCurrentEmployeeId(),
                TargetContainerCode = vals["Container"]?.ToString(),
                Notes = vals["Notes"]?.ToString()
            });
        });
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadParcelEventsAsync();
    }

    private async Task StageParcelAsync()
    {
        var wid = ResolveWarehouseIdForHub();
        var code = _scanInputBox.Text.Trim();
        if (wid <= 0 || string.IsNullOrWhiteSpace(code)) return;
        var locs = await _warehouseApi.GetLocationsAsync(wid.ToString(), null, 1, 100);
        var dlg = new ModalFormDialog($"Xếp vị trí chờ xuất cho {code}",
        [
            new() { Key = "LocationId", Label = "Vị trí chờ xuất (Location)", Kind = ModalFieldKind.ComboBox, Required = true, Options = locs.Items.Select(l => new ModalComboOption(l.Id, $"{l.Code} ({l.ZoneCode})")).ToList() },
            new() { Key = "Bag", Label = "Mã bao tải (BagNumber)" },
            new() { Key = "Notes", Label = "Ghi chú" }
        ],
        async vals =>
        {
            await _warehouseApi.StageParcelAsync(new ParcelStageRequest
            {
                TrackingNumberOrBarcode = code,
                WarehouseId = wid,
                LocationId = Convert.ToInt64(vals["LocationId"]),
                EmployeeId = ResolveCurrentEmployeeId(),
                BagNumber = vals["Bag"]?.ToString(),
                Notes = vals["Notes"]?.ToString()
            });
        });
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadParcelEventsAsync();
    }

    private async Task RecordParcelExceptionAsync()
    {
        var wid = ResolveWarehouseIdForHub();
        var code = _scanInputBox.Text.Trim();
        if (wid <= 0 || string.IsNullOrWhiteSpace(code)) return;
        var dlg = new ModalFormDialog($"Ghi nhận ngoại lệ bưu kiện {code}",
        [
            new() { Key = "Type", Label = "Loại sự cố", Kind = ModalFieldKind.ComboBox, Required = true, InitialValue = "Damaged", Options = [new("Damaged", "Hư hỏng"), new("Misrouted", "Lạc tuyến"), new("Overweight", "Quá cân"), new("MissingLabel", "Mất tem mã")] },
            new() { Key = "Reason", Label = "Lý do chi tiết", Kind = ModalFieldKind.Multiline, Required = true }
        ],
        async vals =>
        {
            await _warehouseApi.RecordParcelExceptionAsync(new ParcelExceptionRequest
            {
                TrackingNumberOrBarcode = code,
                WarehouseId = wid,
                EmployeeId = ResolveCurrentEmployeeId(),
                ExceptionType = vals["Type"]?.ToString() ?? "Damaged",
                Reason = vals["Reason"]?.ToString() ?? string.Empty
            });
        });
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadParcelEventsAsync();
    }

    private async Task LoadParcelEventsAsync()
    {
        _parcelEventsGrid.SetLoading(true);
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            var wid = ResolveWarehouseIdForHub();
            var page = await _warehouseApi.GetParcelEventsAsync(wid > 0 ? wid : null, null, null, _parcelEventsGrid.CurrentPage, _parcelEventsGrid.CurrentPageSize);
            _parcelEventsGrid.BindPagedData(page);
        }, null, null, _logger);
        _parcelEventsGrid.SetLoading(false);
    }

    private async Task LoadDispatchesAsync()
    {
        _dispatchesGrid.SetLoading(true);
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            var page = await _warehouseApi.GetDispatchesAsync(null, null, _dispatchesGrid.CurrentPage, _dispatchesGrid.CurrentPageSize);
            _dispatchesGrid.BindPagedData(page);
        }, null, null, _logger);
        _dispatchesGrid.SetLoading(false);
    }

    private async Task CreateDispatchAsync()
    {
        var warehouses = await _warehouseApi.GetWarehousesAsync(null, _branchContext.ActiveBranchId, null, true, 1, 100);
        var carriers = await _carrierApi.GetCarriersAsync(null, true, new PagedRequest { Page = 1, PageSize = 50 });
        var dlg = new ModalFormDialog("Tạo phiếu xuất kho / Hub",
        [
            new() { Key = "WarehouseId", Label = "Kho xuất", Kind = ModalFieldKind.ComboBox, Required = true, Options = warehouses.Items.Select(w => new ModalComboOption(w.Id, $"{w.Code} - {w.Name}")).ToList() },
            new() { Key = "Type", Label = "Loại xuất", Kind = ModalFieldKind.ComboBox, InitialValue = "HubTransfer", Options = [new("HubTransfer", "HubTransfer"), new("Linehaul", "Linehaul"), new("CarrierHandover", "CarrierHandover"), new("LastMileOut", "LastMileOut")] },
            new() { Key = "DestWarehouseId", Label = "Kho đích (nếu trung chuyển)", Kind = ModalFieldKind.ComboBox, Options = warehouses.Items.Select(w => new ModalComboOption(w.Id, $"{w.Code} - {w.Name}")).Prepend(new ModalComboOption(null, "(Không chọn)")).ToList() },
            new() { Key = "CarrierId", Label = "Đơn vị vận chuyển", Kind = ModalFieldKind.ComboBox, Options = carriers.Items.Select(c => new ModalComboOption(c.Id, $"{c.Code} - {c.Name}")).Prepend(new ModalComboOption(null, "(Nội bộ)")).ToList() },
            new() { Key = "Vehicle", Label = "Biển số xe" },
            new() { Key = "Notes", Label = "Ghi chú", Kind = ModalFieldKind.Multiline }
        ],
        async vals =>
        {
            await _warehouseApi.CreateDispatchAsync(new CreateWarehouseDispatchDto
            {
                WarehouseId = Convert.ToInt64(vals["WarehouseId"]),
                DispatchType = vals["Type"]?.ToString() ?? "HubTransfer",
                DestinationWarehouseId = vals["DestWarehouseId"] != null ? Convert.ToInt64(vals["DestWarehouseId"]) : null,
                AssignedCarrierId = vals["CarrierId"] != null ? Convert.ToInt64(vals["CarrierId"]) : null,
                DispatchedByEmployeeId = ResolveCurrentEmployeeId(),
                VehicleNumber = vals["Vehicle"]?.ToString(),
                Notes = vals["Notes"]?.ToString()
            });
        });
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadDispatchesAsync();
    }

    private async Task ConfirmDispatchAsync()
    {
        var row = _dispatchesGrid.GetSelectedRow<WarehouseDispatchDto>();
        if (row == null) return;
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            await _warehouseApi.ConfirmDispatchAsync(row.Id, ResolveCurrentEmployeeId());
            await LoadDispatchesAsync();
            UiAsyncRunner.ShowInfo(this, $"Đã xác nhận xuất phiếu {row.DispatchNumber}.", "Thành công");
        }, null, null, _logger);
    }
}
