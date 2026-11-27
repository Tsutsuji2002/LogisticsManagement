using Logistics.Client.Application.Contracts.Shipping;
using Logistics.Client.Application.Interfaces;
using Logistics.Client.Application.Permissions;
using Logistics.Client.WinForms.Controls.Common;
using Logistics.Client.WinForms.Helpers;
using Logistics.Client.WinForms.Themes;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.Extensions.Logging;

namespace Logistics.Client.WinForms.Controls.Shipping;

public sealed class ShippingWorkspaceControl : ModuleViewBase
{
    private readonly IShippingApiClient _shippingApi;
    private readonly ISalesApiClient _salesApi;
    private readonly IOrganizationApiClient _orgApi;
    private readonly IAuthorizationService _auth;
    private readonly IBranchContextService _branchContext;
    private readonly ILogger<ShippingWorkspaceControl> _logger;

    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill, Font = AppTheme.BodyFont };
    private readonly TabPage _tabShipments = new("Vận đơn (Shipments)") { BackColor = AppTheme.SurfaceBg };
    private readonly TabPage _tabPickups = new("Yêu cầu lấy hàng") { BackColor = AppTheme.SurfaceBg };
    private readonly TabPage _tabReturns = new("Yêu cầu hoàn hàng") { BackColor = AppTheme.SurfaceBg };

    private PagedDataGridControl _shipmentsGrid = null!;
    private PagedDataGridControl _pickupsGrid = null!;
    private PagedDataGridControl _returnsGrid = null!;
    private readonly Label _shipDetailBanner = new() { Dock = DockStyle.Top, Height = 56, BackColor = AppTheme.CardBg, Padding = new Padding(12, 8, 12, 6) };
    private readonly DataGridView _attemptsGrid = new() { Dock = DockStyle.Fill };
    private readonly DataGridView _historyGrid = new() { Dock = DockStyle.Fill };
    private readonly DataGridView _assignmentsGrid = new() { Dock = DockStyle.Fill };
    private readonly Label _podLabel = new() { Dock = DockStyle.Fill, Padding = new Padding(16), BackColor = AppTheme.CardBg };

    private ShipmentDetailDto? _selectedShipment;

    public ShippingWorkspaceControl(
        IShippingApiClient shippingApi,
        ISalesApiClient salesApi,
        IOrganizationApiClient orgApi,
        IAuthorizationService auth,
        IBranchContextService branchContext,
        ILogger<ShippingWorkspaceControl> logger)
    {
        _shippingApi = shippingApi;
        _salesApi = salesApi;
        _orgApi = orgApi;
        _auth = auth;
        _branchContext = branchContext;
        _logger = logger;

        BuildShipmentsTab();
        BuildPickupsTab();
        BuildReturnsTab();

        if (_auth.HasPermission(ClientPermissions.Shipping.ShipmentsRead)) _tabs.TabPages.Add(_tabShipments);
        if (_auth.HasPermission(ClientPermissions.Shipping.PickupsRead)) _tabs.TabPages.Add(_tabPickups);
        if (_auth.HasPermission(ClientPermissions.Shipping.ReturnsRead)) _tabs.TabPages.Add(_tabReturns);

        _tabs.SelectedIndexChanged += async (_, _) => await RefreshDataAsync();
        Controls.Add(_tabs);
    }

    public void SelectSection(string navKey)
    {
        var tab = navKey switch
        {
            "Shipping.Pickups" => _tabPickups,
            "Shipping.Returns" => _tabReturns,
            _ => _tabShipments
        };
        if (_tabs.TabPages.Contains(tab)) _tabs.SelectedTab = tab;
    }

    private void BuildShipmentsTab()
    {
        var split = AppTheme.CreateSafeSplitContainer(Orientation.Horizontal, 320);
        _shipmentsGrid = new PagedDataGridControl("Vận đơn (Shipments)", "Tạo từ đơn hàng đã xác nhận, điều phối tài xế, chuyển trạng thái theo state-machine backend.");

        if (_auth.HasPermission(ClientPermissions.Shipping.ShipmentsCreate))
        {
            var fromOrderBtn = AppTheme.CreatePrimaryButton("Tạo từ đơn hàng", 135);
            fromOrderBtn.Click += async (_, _) => await CreateShipmentFromOrderAsync();
            _shipmentsGrid.AddActionButton(fromOrderBtn);
        }
        if (_auth.HasPermission(ClientPermissions.Shipping.ShipmentsManageStatus))
        {
            var transBtn = AppTheme.CreateSecondaryButton("Chuyển trạng thái", 135);
            transBtn.Click += async (_, _) => await TransitionShipmentStatusAsync();
            _shipmentsGrid.AddActionButton(transBtn);
        }
        if (_auth.HasPermission(ClientPermissions.Shipping.ShipmentsAssign))
        {
            var assignBtn = AppTheme.CreateSecondaryButton("Phân công NV", 115);
            assignBtn.Click += async (_, _) => await AssignEmployeeToShipmentAsync();
            _shipmentsGrid.AddActionButton(assignBtn);
        }
        if (_auth.HasPermission(ClientPermissions.Shipping.ShipmentsDeliver))
        {
            var attemptBtn = AppTheme.CreateSuccessButton("Ghi lần giao", 115);
            attemptBtn.Click += async (_, _) => await RecordDeliveryAttemptAsync();
            var podBtn = AppTheme.CreatePrimaryButton("Ghi nhận POD", 115);
            podBtn.Click += async (_, _) => await RecordPodAsync();
            _shipmentsGrid.AddActionButton(podBtn);
            _shipmentsGrid.AddActionButton(attemptBtn);
        }
        if (_auth.HasPermission(ClientPermissions.Shipping.ShipmentsCancel))
        {
            var cancelBtn = AppTheme.CreateDangerButton("Hủy VĐ", 85);
            cancelBtn.Click += async (_, _) => await CancelShipmentAsync();
            _shipmentsGrid.AddActionButton(cancelBtn);
        }

        _shipmentsGrid.ReloadRequested += async (_, _) => await LoadShipmentsAsync();
        _shipmentsGrid.SelectionChanged += async (_, _) => await LoadShipmentDetailAsync();
        split.Panel1.Controls.Add(_shipmentsGrid);

        var detailTabs = new TabControl { Dock = DockStyle.Fill, Font = AppTheme.BodyFont };
        AppTheme.StyleDataGrid(_attemptsGrid);
        AppTheme.StyleDataGrid(_historyGrid);
        AppTheme.StyleDataGrid(_assignmentsGrid);

        detailTabs.TabPages.Add(new TabPage("Lần giao hàng (Attempts)") { Controls = { _attemptsGrid } });
        detailTabs.TabPages.Add(new TabPage("Phân công giao nhận") { Controls = { _assignmentsGrid } });
        detailTabs.TabPages.Add(new TabPage("Lịch sử trạng thái") { Controls = { _historyGrid } });
        detailTabs.TabPages.Add(new TabPage("Bằng chứng giao (POD)") { Controls = { _podLabel } });

        split.Panel2.Padding = new Padding(12, 4, 12, 12);
        split.Panel2.Controls.Add(detailTabs);
        split.Panel2.Controls.Add(_shipDetailBanner);
        _tabShipments.Controls.Add(split);
    }

    private void BuildPickupsTab()
    {
        _pickupsGrid = new PagedDataGridControl("Yêu cầu lấy hàng (Pickups)", "Tạo, phân công, xác nhận và hoàn tất lấy hàng.");
        if (_auth.HasPermission(ClientPermissions.Shipping.PickupsCreate))
        {
            var addBtn = AppTheme.CreatePrimaryButton("Tạo YC lấy hàng", 130);
            addBtn.Click += async (_, _) => await CreatePickupAsync();
            _pickupsGrid.AddActionButton(addBtn);
        }
        if (_auth.HasPermission(ClientPermissions.Shipping.PickupsManage))
        {
            var compBtn = AppTheme.CreateSuccessButton("Hoàn tất lấy", 115);
            compBtn.Click += async (_, _) =>
            {
                var p = _pickupsGrid.GetSelectedRow<PickupRequestDto>();
                if (p == null) return;
                var dlg = new ModalFormDialog($"Hoàn tất lấy hàng {p.PickupCode}", [
                    new ModalFieldSpec { Key = "ActualParcelsPickedUp", Label = "Số kiện thực lấy", Kind = ModalFieldKind.Integer, InitialValue = p.EstimatedParcels, Required = true },
                    new ModalFieldSpec { Key = "Notes", Label = "Ghi chú" }
                ], async v => await _shippingApi.CompletePickupAsync(p.Id.ToString(), new CompletePickupRequest { ActualParcelsPickedUp = Convert.ToInt32(v["ActualParcelsPickedUp"]), Notes = v["Notes"]?.ToString() }));
                if (dlg.ShowDialog(this) == DialogResult.OK) await LoadPickupsAsync();
            };
            _pickupsGrid.AddActionButton(compBtn);
        }
        _pickupsGrid.ReloadRequested += async (_, _) => await LoadPickupsAsync();
        _tabPickups.Controls.Add(_pickupsGrid);
    }

    private void BuildReturnsTab()
    {
        _returnsGrid = new PagedDataGridControl("Yêu cầu hoàn hàng (Returns)", "Khởi tạo và duyệt hoàn hàng theo vận đơn gốc.");
        if (_auth.HasPermission(ClientPermissions.Shipping.ReturnsManage))
        {
            var createBtn = AppTheme.CreatePrimaryButton("Tạo YC hoàn", 115);
            createBtn.Click += async (_, _) => await CreateReturnAsync();
            var procBtn = AppTheme.CreateSuccessButton("Duyệt / Từ chối", 125);
            procBtn.Click += async (_, _) =>
            {
                var r = _returnsGrid.GetSelectedRow<ReturnRequestDto>();
                if (r == null) return;
                var dlg = new ModalFormDialog($"Xử lý hoàn {r.ReturnCode}", [
                    new ModalFieldSpec { Key = "Decision", Label = "Quyết định", Kind = ModalFieldKind.ComboBox, Required = true,
                        Options = [new("Approved","Approved (Chấp nhận hoàn)"), new("Rejected","Rejected (Từ chối)")] },
                    new ModalFieldSpec { Key = "Notes", Label = "Ghi chú" }
                ], async v => await _shippingApi.ProcessReturnRequestAsync(r.Id.ToString(), new ProcessReturnRequestRequest { Decision = v["Decision"]!.ToString()!, Notes = v["Notes"]?.ToString() }));
                if (dlg.ShowDialog(this) == DialogResult.OK) await LoadReturnsAsync();
            };
            _returnsGrid.AddActionButton(procBtn);
            _returnsGrid.AddActionButton(createBtn);
        }
        _returnsGrid.ReloadRequested += async (_, _) => await LoadReturnsAsync();
        _tabReturns.Controls.Add(_returnsGrid);
    }

    protected override async Task RefreshDataAsync()
    {
        if (_tabs.SelectedTab == _tabShipments) await LoadShipmentsAsync();
        else if (_tabs.SelectedTab == _tabPickups) await LoadPickupsAsync();
        else if (_tabs.SelectedTab == _tabReturns) await LoadReturnsAsync();
    }

    private async Task LoadShipmentsAsync()
    {
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            _shipmentsGrid.SetLoading(true);
            var paged = await _shippingApi.GetShipmentsAsync(_shipmentsGrid.SearchText, null, null, _branchContext.ActiveBranchId, null, null, null,
                new PagedRequest { Page = _shipmentsGrid.CurrentPage, PageSize = _shipmentsGrid.CurrentPageSize });
            _shipmentsGrid.BindPagedData(paged);
        }, logger: _logger, showMessageBoxOnError: false);
        _shipmentsGrid.SetLoading(false);
    }

    private async Task LoadShipmentDetailAsync()
    {
        var row = _shipmentsGrid.GetSelectedRow<ShipmentDto>();
        if (row == null) return;
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            _selectedShipment = await _shippingApi.GetShipmentDetailAsync(row.Id.ToString());
            _shipDetailBanner.Text =
                $"VĐ: {_selectedShipment.TrackingNumber} | Trạng thái: {_selectedShipment.ShipmentStatus} | Dịch vụ: {_selectedShipment.ServiceType}\n" +
                $"Tuyến: {_selectedShipment.OriginBranchName} -> {_selectedShipment.DestinationBranchName} | Cước: {AppTheme.FormatMoney(_selectedShipment.TotalAmount, _selectedShipment.CurrencyCode)} | COD: {AppTheme.FormatMoney(_selectedShipment.CodAmount, _selectedShipment.CurrencyCode)}";
            _historyGrid.DataSource = _selectedShipment.StatusHistory.ToList();
            _assignmentsGrid.DataSource = _selectedShipment.Assignments.ToList();
            _attemptsGrid.DataSource = (await _shippingApi.GetDeliveryAttemptsAsync(_selectedShipment.Id.ToString())).ToList();
            var pod = await _shippingApi.GetProofOfDeliveryAsync(_selectedShipment.Id.ToString());
            _podLabel.Text = pod == null
                ? "Chưa có Proof of Delivery cho vận đơn này."
                : $"Người nhận: {pod.RecipientName} ({pod.RecipientRelation}) | Thời gian giao: {AppTheme.FormatLocalTime(pod.DeliveredAt)}\nCMND/CCCD: {pod.RecipientNationalId ?? "-"} | AttachmentId Chữ ký: {pod.SignatureAttachmentId?.ToString() ?? "-"} | Ảnh: {pod.PhotoAttachmentId?.ToString() ?? "-"}\nGhi chú: {pod.Notes ?? "-"}\n(Lưu ý API gap: Backend hiện lưu metadata POD JSON, chưa có endpoint multipart upload file binary.)";
        }, logger: _logger, showMessageBoxOnError: false);
    }

    private async Task CreateShipmentFromOrderAsync()
    {
        var orders = await _salesApi.GetOrdersAsync(null, "Confirmed", null, null, _branchContext.ActiveBranchId, null, null, new PagedRequest { Page = 1, PageSize = 50 });
        var branches = _branchContext.AccessibleBranches.Select(b => new ModalComboOption(b.BranchId, $"{b.Code} - {b.Name}")).ToList();
        if (orders.Items.Count == 0) { UiAsyncRunner.ShowInfo(this, "Không có đơn hàng Confirmed chờ tạo vận đơn."); return; }

        var dlg = new ModalFormDialog("Tạo vận đơn từ đơn hàng", [
            new ModalFieldSpec { Key = "OrderId", Label = "Đơn hàng", Kind = ModalFieldKind.ComboBox, Required = true,
                Options = orders.Items.Select(o => new ModalComboOption(o.Id, $"{o.OrderNumber} - {o.CustomerName}")).ToList() },
            new ModalFieldSpec { Key = "OriginBranchId", Label = "Chi nhánh gửi", Kind = ModalFieldKind.ComboBox, Required = true, Options = branches, InitialValue = _branchContext.ActiveBranchId },
            new ModalFieldSpec { Key = "DestinationBranchId", Label = "Chi nhánh nhận", Kind = ModalFieldKind.ComboBox, Required = true, Options = branches, InitialValue = _branchContext.ActiveBranchId },
            new ModalFieldSpec { Key = "ServiceType", Label = "Loại dịch vụ", Kind = ModalFieldKind.ComboBox, Required = true,
                Options = [new("StandardGround","StandardGround"), new("NextDayExpress","NextDayExpress"), new("SameDay","SameDay"), new("Economical","Economical")] },
            new ModalFieldSpec { Key = "CodAmount", Label = "COD thu hộ", Kind = ModalFieldKind.Decimal, InitialValue = orders.Items[0].CodAmount }
        ], async v => await _shippingApi.CreateFromOrderAsync(new CreateShipmentFromOrderRequest
        {
            OrderId = Convert.ToInt64(v["OrderId"]), OriginBranchId = Convert.ToInt64(v["OriginBranchId"]),
            DestinationBranchId = Convert.ToInt64(v["DestinationBranchId"]), ServiceType = v["ServiceType"]!.ToString()!,
            CodAmount = Convert.ToDecimal(v["CodAmount"])
        }));
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadShipmentsAsync();
    }

    private async Task TransitionShipmentStatusAsync()
    {
        if (_selectedShipment == null) return;
        var allowed = _selectedShipment.ShipmentStatus switch
        {
            "Draft" => new[] { "ReadyForPickup", "Cancelled" },
            "ReadyForPickup" => new[] { "PickedUp", "Cancelled" },
            "PickedUp" => new[] { "InSortingHub", "InTransit", "Cancelled" },
            "InSortingHub" => new[] { "InTransit" },
            "InTransit" => new[] { "OutForDelivery", "InSortingHub" },
            "OutForDelivery" => new[] { "Delivered", "DeliveryFailed" },
            "DeliveryFailed" => new[] { "OutForDelivery", "Returning", "Cancelled" },
            "Returning" => new[] { "Returned" },
            _ => Array.Empty<string>()
        };
        if (allowed.Length == 0) { UiAsyncRunner.ShowInfo(this, $"Vận đơn đang ở trạng thái kết thúc ({_selectedShipment.ShipmentStatus})."); return; }

        var dlg = new ModalFormDialog($"Chuyển trạng thái {_selectedShipment.TrackingNumber}", [
            new ModalFieldSpec { Key = "TargetStatus", Label = "Trạng thái kế tiếp", Kind = ModalFieldKind.ComboBox, Required = true,
                Options = allowed.Select(s => new ModalComboOption(s, s)).ToList() },
            new ModalFieldSpec { Key = "Notes", Label = "Ghi chú" }
        ], async v => await _shippingApi.TransitionStatusAsync(_selectedShipment.Id.ToString(), new ShipmentStatusTransitionRequest
        {
            TargetStatus = v["TargetStatus"]!.ToString()!, Notes = v["Notes"]?.ToString()
        }));
        if (dlg.ShowDialog(this) == DialogResult.OK) { await LoadShipmentsAsync(); await LoadShipmentDetailAsync(); }
    }

    private async Task AssignEmployeeToShipmentAsync()
    {
        if (_selectedShipment == null) return;
        var emps = await _orgApi.GetEmployeesAsync(null, _branchContext.ActiveBranchId, null, "Active", new PagedRequest { Page = 1, PageSize = 100 });
        var dlg = new ModalFormDialog($"Phân công VĐ {_selectedShipment.TrackingNumber}", [
            new ModalFieldSpec { Key = "AssignmentType", Label = "Loại tác vụ", Kind = ModalFieldKind.ComboBox, Required = true,
                Options = [new("Pickup","Pickup"), new("Linehaul","Linehaul"), new("LastMileDelivery","LastMileDelivery")] },
            new ModalFieldSpec { Key = "EmployeeId", Label = "Nhân viên", Kind = ModalFieldKind.ComboBox, Required = true,
                Options = emps.Items.Select(e => new ModalComboOption(e.Id, $"{e.Code} - {e.FullName}")).ToList() },
            new ModalFieldSpec { Key = "Notes", Label = "Ghi chú" }
        ], async v => await _shippingApi.AssignAsync(_selectedShipment.Id.ToString(), new CreateShipmentAssignmentRequest
        {
            AssignmentType = v["AssignmentType"]!.ToString()!, AssignedToEmployeeId = Convert.ToInt64(v["EmployeeId"]),
            AssignedBranchId = _branchContext.ActiveBranchId ?? _selectedShipment.DestinationBranchId, Notes = v["Notes"]?.ToString()
        }));
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadShipmentDetailAsync();
    }

    private async Task RecordDeliveryAttemptAsync()
    {
        if (_selectedShipment == null) return;
        var emps = await _orgApi.GetEmployeesAsync(null, _branchContext.ActiveBranchId, null, "Active", new PagedRequest { Page = 1, PageSize = 100 });
        var dlg = new ModalFormDialog($"Ghi lần giao {_selectedShipment.TrackingNumber}", [
            new ModalFieldSpec { Key = "EmployeeId", Label = "Tài xế giao", Kind = ModalFieldKind.ComboBox, Required = true,
                Options = emps.Items.Select(e => new ModalComboOption(e.Id, $"{e.Code} - {e.FullName}")).ToList() },
            new ModalFieldSpec { Key = "Status", Label = "Kết quả", Kind = ModalFieldKind.ComboBox, Required = true,
                Options = [new("Delivered","Delivered (Thành công)"), new("Failed","Failed (Thất bại)"), new("Rescheduled","Rescheduled (Hẹn lại)")] },
            new ModalFieldSpec { Key = "FailureNotes", Label = "Ghi chú / Lý do" }
        ], async v => await _shippingApi.RecordDeliveryAttemptAsync(_selectedShipment.Id.ToString(), new RecordDeliveryAttemptRequest
        {
            AttemptedByEmployeeId = Convert.ToInt64(v["EmployeeId"]), AttemptedAt = DateTime.UtcNow,
            Status = v["Status"]!.ToString()!, FailureNotes = v["FailureNotes"]?.ToString(), RecipientContacted = true
        }));
        if (dlg.ShowDialog(this) == DialogResult.OK) { await LoadShipmentsAsync(); await LoadShipmentDetailAsync(); }
    }

    private async Task RecordPodAsync()
    {
        if (_selectedShipment == null) return;
        var attempts = await _shippingApi.GetDeliveryAttemptsAsync(_selectedShipment.Id.ToString());
        var deliveredAttempt = attempts.FirstOrDefault(a => a.Status == "Delivered");
        if (deliveredAttempt == null)
        {
            UiAsyncRunner.ShowInfo(this, "Vận đơn cần có ít nhất 1 DeliveryAttempt trạng thái Delivered trước khi ghi nhận POD.");
            return;
        }

        // Allow selecting local image file for reference preview while submitting honest JSON metadata
        using var ofd = new OpenFileDialog { Title = "Chọn ảnh chữ ký / giao hàng tham chiếu (tùy chọn)", Filter = "Images|*.png;*.jpg;*.jpeg" };
        string selectedLocalFile = ofd.ShowDialog(this) == DialogResult.OK ? Path.GetFileName(ofd.FileName) : "";

        var dlg = new ModalFormDialog($"Ghi nhận POD ({_selectedShipment.TrackingNumber})", [
            new ModalFieldSpec { Key = "RecipientName", Label = "Người ký nhận", Required = true },
            new ModalFieldSpec { Key = "RecipientRelation", Label = "Quan hệ", Kind = ModalFieldKind.ComboBox, Required = true,
                Options = [new("Self","Self (Chính chủ)"), new("FamilyMember","FamilyMember"), new("Receptionist","Receptionist"), new("SecurityGuard","SecurityGuard")] },
            new ModalFieldSpec { Key = "Notes", Label = "Ghi chú / Tham chiếu file", InitialValue = string.IsNullOrEmpty(selectedLocalFile) ? "" : $"Local file selected: {selectedLocalFile}" }
        ], async v => await _shippingApi.CreateProofOfDeliveryAsync(_selectedShipment.Id.ToString(), deliveredAttempt.Id, new CreateProofOfDeliveryRequest
        {
            RecipientName = v["RecipientName"]!.ToString()!, RecipientRelation = v["RecipientRelation"]!.ToString()!,
            DeliveredAt = DateTime.UtcNow, Notes = v["Notes"]?.ToString()
        }));
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadShipmentDetailAsync();
    }

    private async Task CancelShipmentAsync()
    {
        if (_selectedShipment == null) return;
        var dlg = new ModalFormDialog($"Hủy vận đơn {_selectedShipment.TrackingNumber}", [
            new ModalFieldSpec { Key = "Reason", Label = "Lý do hủy", Required = true, Kind = ModalFieldKind.Multiline }
        ], async v => await _shippingApi.CancelShipmentAsync(_selectedShipment.Id.ToString(), new CancelShipmentRequest { Reason = v["Reason"]!.ToString()! }));
        if (dlg.ShowDialog(this) == DialogResult.OK) { await LoadShipmentsAsync(); await LoadShipmentDetailAsync(); }
    }

    private async Task LoadPickupsAsync()
    {
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            _pickupsGrid.SetLoading(true);
            _pickupsGrid.BindPagedData(await _shippingApi.GetPickupRequestsAsync(_pickupsGrid.SearchText, null, new PagedRequest { Page = _pickupsGrid.CurrentPage, PageSize = _pickupsGrid.CurrentPageSize }));
        }, logger: _logger, showMessageBoxOnError: false);
        _pickupsGrid.SetLoading(false);
    }

    private async Task CreatePickupAsync()
    {
        var dlg = new ModalFormDialog("Tạo yêu cầu lấy hàng", [
            new ModalFieldSpec { Key = "CustomerId", Label = "Customer ID", Kind = ModalFieldKind.Integer, Required = true },
            new ModalFieldSpec { Key = "RequestedAddressId", Label = "Address ID", Kind = ModalFieldKind.Integer, Required = true },
            new ModalFieldSpec { Key = "ScheduledDate", Label = "Ngày hẹn", Kind = ModalFieldKind.Date, Required = true, InitialValue = DateTime.Today.AddDays(1) },
            new ModalFieldSpec { Key = "TimeSlot", Label = "Khung giờ", Required = true, InitialValue = "09:00-12:00" },
            new ModalFieldSpec { Key = "ContactName", Label = "Người liên hệ", Required = true },
            new ModalFieldSpec { Key = "ContactPhone", Label = "SĐT", Required = true },
            new ModalFieldSpec { Key = "EstimatedParcels", Label = "Số kiện dự kiến", Kind = ModalFieldKind.Integer, InitialValue = 1 }
        ], async v => await _shippingApi.CreatePickupRequestAsync(new CreatePickupRequestRequest
        {
            CustomerId = Convert.ToInt64(v["CustomerId"]), RequestedAddressId = Convert.ToInt64(v["RequestedAddressId"]),
            ScheduledDate = DateOnly.FromDateTime((DateTime)v["ScheduledDate"]!), TimeSlot = v["TimeSlot"]!.ToString()!,
            ContactName = v["ContactName"]!.ToString()!, ContactPhone = v["ContactPhone"]!.ToString()!, EstimatedParcels = Convert.ToInt32(v["EstimatedParcels"])
        }));
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadPickupsAsync();
    }

    private async Task LoadReturnsAsync()
    {
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            _returnsGrid.SetLoading(true);
            _returnsGrid.BindPagedData(await _shippingApi.GetReturnRequestsAsync(_returnsGrid.SearchText, null, new PagedRequest { Page = _returnsGrid.CurrentPage, PageSize = _returnsGrid.CurrentPageSize }));
        }, logger: _logger, showMessageBoxOnError: false);
        _returnsGrid.SetLoading(false);
    }

    private async Task CreateReturnAsync()
    {
        var dlg = new ModalFormDialog("Tạo yêu cầu hoàn hàng", [
            new ModalFieldSpec { Key = "OriginalShipmentId", Label = "Shipment ID gốc", Kind = ModalFieldKind.Integer, Required = true },
            new ModalFieldSpec { Key = "ReasonCode", Label = "Mã lý do", Required = true, InitialValue = "CUSTOMER_REFUSED" },
            new ModalFieldSpec { Key = "ReasonDescription", Label = "Mô tả lý do", Required = true, Kind = ModalFieldKind.Multiline }
        ], async v => await _shippingApi.CreateReturnRequestAsync(new CreateReturnRequestRequest
        {
            OriginalShipmentId = Convert.ToInt64(v["OriginalShipmentId"]), ReasonCode = v["ReasonCode"]!.ToString()!, ReasonDescription = v["ReasonDescription"]!.ToString()!
        }));
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadReturnsAsync();
    }
}
