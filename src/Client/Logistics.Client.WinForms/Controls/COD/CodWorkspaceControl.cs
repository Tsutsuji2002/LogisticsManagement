using AntdUI;
using Logistics.Client.Application.Contracts.Cod;
using Logistics.Client.Application.Interfaces;
using Logistics.Client.Application.Permissions;
using Logistics.Client.WinForms.Controls.Common;
using Logistics.Client.WinForms.Helpers;
using Logistics.Client.WinForms.Themes;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.Extensions.Logging;
using Label = System.Windows.Forms.Label;

namespace Logistics.Client.WinForms.Controls.COD;

public sealed class CodWorkspaceControl : ModuleViewBase, IThemeAware
{
    private readonly ICodApiClient _codApi;
    private readonly IShippingApiClient _shippingApi;
    private readonly IOrganizationApiClient _orgApi;
    private readonly ICrmApiClient _crmApi;
    private readonly IAuthenticationService _authService;
    private readonly IAuthorizationService _auth;
    private readonly IBranchContextService _branchContext;
    private readonly ILogger<CodWorkspaceControl> _logger;

    private readonly AntdUI.Tabs _tabs = AppTheme.CreateAntdTabs();
    private readonly AntdUI.TabPage _tabCollections = new() { Text = "Thu hộ COD (Collections)" };
    private readonly AntdUI.TabPage _tabRemittances = new() { Text = "Bảng kê hoàn trả COD (Remittances)" };
    private readonly AntdUI.TabPage _tabRecon = new() { Text = "Đối soát COD (Reconciliation)", Padding = new Padding(12) };

    private PagedDataGridControl _collectionsGrid = null!;
    private readonly AntdUI.Select _colStatusFilter = new() { Width = 175, Height = 32, Radius = 6, List = true, Font = AppTheme.BodyFont, Margin = new Padding(0, 0, 8, 4) };
    private readonly AntdUI.Alert _colSummaryLabel = new() { Dock = DockStyle.Top, Height = 44, Radius = 6, Icon = TType.Info, Font = AppTheme.BodyBoldFont, Margin = new Padding(0, 0, 0, 8), Text = "Tổng hợp thu hộ COD..." };

    private PagedDataGridControl _remittancesGrid = null!;
    private AntdUI.Panel _remItemsCard = null!;
    private readonly AntdUI.Table _remItemsGrid = AppTheme.CreateAntdTable();
    private CodRemittanceDto? _selectedRemittance;

    private AntdUI.Panel _reconSummaryCard = null!;
    private readonly Label _reconSummaryLabel = new() { Dock = DockStyle.Fill, Font = AppTheme.BodyFont, BackColor = Color.Transparent };
    private AntdUI.Select _reconEmployeeCombo = null!;
    private AntdUI.Panel _reconDetailCard = null!;
    private Label _reconDetailLabel = null!;

    public CodWorkspaceControl(
        ICodApiClient codApi,
        IShippingApiClient shippingApi,
        IOrganizationApiClient orgApi,
        ICrmApiClient crmApi,
        IAuthenticationService authService,
        IAuthorizationService auth,
        IBranchContextService branchContext,
        ILogger<CodWorkspaceControl> logger)
    {
        _codApi = codApi;
        _shippingApi = shippingApi;
        _orgApi = orgApi;
        _crmApi = crmApi;
        _authService = authService;
        _auth = auth;
        _branchContext = branchContext;
        _logger = logger;

        BuildCollectionsTab();
        BuildRemittancesTab();
        BuildReconTab();

        if (_auth.HasPermission(ClientPermissions.Cod.CollectionsRead)) _tabs.Pages.Add(_tabCollections);
        if (_auth.HasPermission(ClientPermissions.Cod.RemittancesRead)) _tabs.Pages.Add(_tabRemittances);
        if (_auth.HasPermission(ClientPermissions.Cod.ReconciliationRead)) _tabs.Pages.Add(_tabRecon);

        _tabs.SelectedIndexChanged += async (_, _) => await RefreshDataAsync();
        Controls.Add(_tabs);
        ApplyTheme();
    }

    public void SelectSection(string navKey)
    {
        var tab = navKey switch
        {
            "Cod.Remittances" => _tabRemittances,
            "Cod.Reconciliation" => _tabRecon,
            _ => _tabCollections
        };
        if (_tabs.Pages.Contains(tab)) _tabs.SelectedTab = tab;
    }

    private void BuildCollectionsTab()
    {
        _collectionsGrid = new PagedDataGridControl("Phiếu thu hộ COD (CODCollections)", "Tách bạch rõ: Dự kiến -> Tài xế thu -> Nộp chi nhánh -> Thủ quỹ xác nhận -> Đã hoàn trả.");
        _colStatusFilter.Items.AddRange([
            new SelectItem("Tất cả trạng thái", ""),
            new SelectItem("Pending", "Pending"),
            new SelectItem("CollectedByDriver", "CollectedByDriver"),
            new SelectItem("DepositedToBranch", "DepositedToBranch"),
            new SelectItem("VerifiedByCashier", "VerifiedByCashier"),
            new SelectItem("RemittedToCustomer", "RemittedToCustomer"),
            new SelectItem("CollectionFailed", "CollectionFailed")
        ]);
        _colStatusFilter.SelectedIndex = 0;
        _colStatusFilter.SelectedIndexChanged += (_, _) => _collectionsGrid.ResetAndReload();
        _collectionsGrid.AddFilterControl(_colStatusFilter);

        if (_auth.HasPermission(ClientPermissions.Cod.CollectionsCreate))
        {
            var createBtn = AppTheme.CreatePrimaryButton("Tạo phiếu COD", 120);
            createBtn.Click += async (_, _) => await CreateCollectionAsync();
            _collectionsGrid.AddActionButton(createBtn);
        }
        if (_auth.HasPermission(ClientPermissions.Cod.CollectionsCollect))
        {
            var colBtn = AppTheme.CreateSuccessButton("Tài xế đã thu", 120);
            colBtn.Click += async (_, _) => await CollectAsync();
            var depBtn = AppTheme.CreateSecondaryButton("Nộp về chi nhánh", 135);
            depBtn.Click += async (_, _) => await DepositAsync();
            var failBtn = AppTheme.CreateDangerButton("Thu thất bại", 115);
            failBtn.Click += async (_, _) => await FailCollectionAsync();
            _collectionsGrid.AddActionButton(colBtn);
            _collectionsGrid.AddActionButton(depBtn);
            _collectionsGrid.AddActionButton(failBtn);
        }
        if (_auth.HasPermission(ClientPermissions.Cod.CollectionsVerify))
        {
            var verBtn = AppTheme.CreatePrimaryButton("Thủ quỹ xác nhận", 145);
            verBtn.Click += async (_, _) => await VerifyAsync();
            _collectionsGrid.AddActionButton(verBtn);
        }

        _collectionsGrid.ReloadRequested += async (_, _) => await LoadCollectionsAsync();
        _tabCollections.Controls.Add(_collectionsGrid);
        _tabCollections.Controls.Add(_colSummaryLabel);
    }

    private void BuildRemittancesTab()
    {
        var split = AppTheme.CreateSafeSplitContainer(Orientation.Horizontal, 320);
        _remittancesGrid = new PagedDataGridControl("Bảng kê hoàn trả COD cho khách hàng", "Tổng COD gộp, phí dịch vụ khấu trừ và thực chi hoàn trả ròng do backend tính.");

        if (_auth.HasPermission(ClientPermissions.Cod.RemittancesCreate))
        {
            var addBtn = AppTheme.CreatePrimaryButton("Tạo bảng kê COD", 140);
            addBtn.Click += async (_, _) => await CreateRemittanceAsync();
            var addItemBtn = AppTheme.CreateSecondaryButton("Thêm vận đơn COD", 145);
            addItemBtn.Click += async (_, _) => await AddRemittanceItemAsync();
            _remittancesGrid.AddActionButton(addBtn);
            _remittancesGrid.AddActionButton(addItemBtn);
        }
        if (_auth.HasPermission(ClientPermissions.Cod.RemittancesConfirm))
        {
            var confBtn = AppTheme.CreateSuccessButton("Xác nhận hoàn trả", 145);
            confBtn.Click += async (_, _) => await ConfirmRemittanceAsync();
            _remittancesGrid.AddActionButton(confBtn);
        }
        if (_auth.HasPermission(ClientPermissions.Cod.RemittancesManage))
        {
            var canBtn = AppTheme.CreateDangerButton("Hủy bảng kê", 115);
            canBtn.Click += async (_, _) => await CancelRemittanceAsync();
            _remittancesGrid.AddActionButton(canBtn);
        }

        _remittancesGrid.ReloadRequested += async (_, _) => await LoadRemittancesAsync();
        _remittancesGrid.SelectionChanged += async (_, _) => await LoadRemittanceItemsAsync();

        _remItemsCard = AppTheme.CreateCardPanel(new Padding(10));
        _remItemsCard.Dock = DockStyle.Fill;
        _remItemsCard.Controls.Add(_remItemsGrid);
        _remItemsCard.Controls.Add(new Label { Text = "Chi tiết khoản thu COD trong bảng kê", Dock = DockStyle.Top, Height = 28, Font = AppTheme.SectionTitleFont, BackColor = Color.Transparent });

        split.Panel1.Controls.Add(_remittancesGrid);
        split.Panel2.Controls.Add(_remItemsCard);
        _tabRemittances.Controls.Add(split);
    }

    private void BuildReconTab()
    {
        _reconSummaryCard = AppTheme.CreateCardPanel(new Padding(16));
        _reconSummaryCard.Dock = DockStyle.Top;
        _reconSummaryCard.Height = 122;
        _reconSummaryCard.Margin = new Padding(0, 0, 0, 8);
        _reconSummaryCard.Controls.Add(_reconSummaryLabel);

        var actionPanel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 52, Padding = new Padding(0, 8, 0, 6), BackColor = Color.Transparent };
        _reconEmployeeCombo = new AntdUI.Select { Width = 280, Height = 34, Radius = 6, List = true, Font = AppTheme.BodyFont, Margin = new Padding(0, 0, 8, 0) };
        var empBtn = AppTheme.CreatePrimaryButton("Đối soát tài xế", 130);
        empBtn.Click += async (_, _) => await ReconcileEmployeeAsync();
        var branchBtn = AppTheme.CreateSecondaryButton("Đối soát chi nhánh hiện tại", 190);
        branchBtn.Click += async (_, _) => await ReconcileBranchAsync();
        actionPanel.Controls.AddRange([_reconEmployeeCombo, empBtn, branchBtn]);

        _reconDetailCard = AppTheme.CreateCardPanel(new Padding(16));
        _reconDetailCard.Dock = DockStyle.Fill;
        _reconDetailLabel = new Label
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Font = AppTheme.BodyFont,
            ForeColor = AppTheme.TextPrimary,
            Text = "Chọn nhân viên hoặc chi nhánh để đối soát công quỹ COD theo thời gian thực."
        };
        _reconDetailCard.Controls.Add(_reconDetailLabel);

        _tabRecon.Controls.Add(_reconDetailCard);
        _tabRecon.Controls.Add(actionPanel);
        _tabRecon.Controls.Add(_reconSummaryCard);
    }

    protected override async Task RefreshDataAsync()
    {
        if (_tabs.SelectedTab == _tabCollections) await LoadCollectionsAsync();
        else if (_tabs.SelectedTab == _tabRemittances) await LoadRemittancesAsync();
        else if (_tabs.SelectedTab == _tabRecon) await LoadReconSummaryAsync();
    }

    private async Task LoadCollectionsAsync()
    {
        _collectionsGrid.SetLoading(true);
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            var status = _colStatusFilter.SelectedValue?.ToString();
            var list = await _codApi.GetCollectionsAsync(_branchContext.ActiveBranchId, null, null, string.IsNullOrWhiteSpace(status) ? null : status, _collectionsGrid.CurrentPage, _collectionsGrid.CurrentPageSize);
            _collectionsGrid.BindPagedData(list);

            var sum = await _codApi.GetCollectionSummaryAsync(_branchContext.ActiveBranchId);
            _colSummaryLabel.Text =
                $"Dự kiến: {AppTheme.FormatMoney(sum.TotalExpectedAmount)}  |  Tài xế đã thu: {AppTheme.FormatMoney(sum.TotalCollectedAmount)}  |  Thủ quỹ đã xác nhận: {AppTheme.FormatMoney(sum.TotalVerifiedAmount)}  |  Đã hoàn trả KH: {AppTheme.FormatMoney(sum.TotalRemittedAmount)}";
        }, null, null, _logger);
        _collectionsGrid.SetLoading(false);
    }

    private async Task LoadRemittancesAsync()
    {
        _remittancesGrid.SetLoading(true);
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            var list = await _codApi.GetRemittancesAsync(null, null, _remittancesGrid.CurrentPage, _remittancesGrid.CurrentPageSize);
            _remittancesGrid.BindPagedData(list);
            await LoadRemittanceItemsAsync();
        }, null, null, _logger);
        _remittancesGrid.SetLoading(false);
    }

    private async Task LoadRemittanceItemsAsync()
    {
        _selectedRemittance = _remittancesGrid.GetSelectedRow<CodRemittanceDto>();
        if (_selectedRemittance == null) { AppTheme.BindAntdTable(_remItemsGrid, Array.Empty<CodRemittanceItemDto>()); return; }
        var full = await _codApi.GetRemittanceByIdAsync(_selectedRemittance.Id);
        AppTheme.BindAntdTable(_remItemsGrid, full.Items);
    }

    private async Task LoadReconSummaryAsync()
    {
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            var s = await _codApi.GetReconciliationSummaryAsync(_branchContext.ActiveBranchId);
            _reconSummaryLabel.Text =
                $"TỔNG HỢP ĐỐI SOÁT COD (Server Authoritative)\n" +
                $"• COD dự kiến: {AppTheme.FormatMoney(s.TotalShipmentCodExpected)}   |   • Tài xế giữ: {AppTheme.FormatMoney(s.TotalOutstandingWithDrivers)}   |   • Quỹ chi nhánh giữ: {AppTheme.FormatMoney(s.TotalHoldingInBranches)}\n" +
                $"• Thủ quỹ xác nhận: {AppTheme.FormatMoney(s.TotalVerifiedByCashiers)}   |   • Đã hoàn trả KH: {AppTheme.FormatMoney(s.TotalRemittedToCustomers)}   |   • Chênh lệch (Discrepancy): {AppTheme.FormatMoney(s.DiscrepancyAmount)}";

            var emps = await _orgApi.GetEmployeesAsync(null, _branchContext.ActiveBranchId, null, "Active", new PagedRequest { Page = 1, PageSize = 100 });
            _reconEmployeeCombo.Items.Clear();
            foreach (var e in emps.Items)
                _reconEmployeeCombo.Items.Add(new SelectItem($"{e.Code} - {e.FullName}", e.Id));
            if (_reconEmployeeCombo.Items.Count > 0) _reconEmployeeCombo.SelectedIndex = 0;
        }, null, null, _logger);
    }

    private async Task CreateCollectionAsync()
    {
        var ships = await _shippingApi.GetShipmentsAsync(null, null, null, _branchContext.ActiveBranchId, null, null, null, new PagedRequest { Page = 1, PageSize = 50 });
        var emps = await _orgApi.GetEmployeesAsync(null, _branchContext.ActiveBranchId, null, "Active", new PagedRequest { Page = 1, PageSize = 50 });
        var dlg = new ModalFormDialog("Tạo phiếu thu hộ COD",
        [
            new() { Key = "ShipmentId", Label = "Vận đơn", Kind = ModalFieldKind.ComboBox, Required = true, Options = ships.Items.Select(s => new ModalComboOption(s.Id, $"{s.TrackingNumber} ({AppTheme.FormatMoney(s.CodAmount, s.CurrencyCode)})")).ToList() },
            new() { Key = "EmployeeId", Label = "Nhân viên giao hàng", Kind = ModalFieldKind.ComboBox, Required = true, InitialValue = _authService.CurrentUser?.EmployeeId, Options = emps.Items.Select(e => new ModalComboOption(e.Id, $"{e.Code} - {e.FullName}")).ToList() },
            new() { Key = "Amount", Label = "Số tiền thu thực tế", Kind = ModalFieldKind.Decimal, Required = true, InitialValue = 0m },
            new() { Key = "Receipt", Label = "Số biên nhận tài xế" },
            new() { Key = "Notes", Label = "Ghi chú" }
        ],
        async vals =>
        {
            await _codApi.CreateCollectionAsync(new CreateCodCollectionRequest
            {
                ShipmentId = Convert.ToInt64(vals["ShipmentId"]),
                CollectedByEmployeeId = Convert.ToInt64(vals["EmployeeId"]),
                CollectedAmount = Convert.ToDecimal(vals["Amount"]),
                DriverReceiptNumber = vals["Receipt"]?.ToString(),
                Notes = vals["Notes"]?.ToString()
            });
        });
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadCollectionsAsync();
    }

    private async Task CollectAsync()
    {
        var row = _collectionsGrid.GetSelectedRow<CodCollectionDto>();
        if (row == null) return;
        var dlg = new ModalFormDialog($"Ghi nhận tài xế thu COD - {row.ShipmentTrackingNumber}",
        [
            new() { Key = "Amount", Label = "Số tiền đã thu", Kind = ModalFieldKind.Decimal, Required = true, InitialValue = row.ExpectedCodAmount },
            new() { Key = "Receipt", Label = "Số biên nhận" },
            new() { Key = "Notes", Label = "Ghi chú" }
        ],
        async vals => await _codApi.CollectAsync(row.Id, new RecordCodCollectionRequest
        {
            CollectedAmount = Convert.ToDecimal(vals["Amount"]),
            CollectedByEmployeeId = _authService.CurrentUser?.EmployeeId,
            DriverReceiptNumber = vals["Receipt"]?.ToString(),
            Notes = vals["Notes"]?.ToString()
        }));
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadCollectionsAsync();
    }

    private async Task DepositAsync()
    {
        var row = _collectionsGrid.GetSelectedRow<CodCollectionDto>();
        if (row == null) return;
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            await _codApi.DepositAsync(row.Id, "Nộp quỹ từ WinForms Desktop");
            await LoadCollectionsAsync();
        }, null, null, _logger);
    }

    private async Task VerifyAsync()
    {
        var row = _collectionsGrid.GetSelectedRow<CodCollectionDto>();
        if (row == null) return;
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            await _codApi.VerifyAsync(row.Id, new VerifyCodCollectionRequest { Notes = "Thủ quỹ đã kiểm đếm" });
            await LoadCollectionsAsync();
        }, null, null, _logger);
    }

    private async Task FailCollectionAsync()
    {
        var row = _collectionsGrid.GetSelectedRow<CodCollectionDto>();
        if (row == null) return;
        var dlg = new ModalFormDialog("Ghi nhận thu COD thất bại",
        [
            new() { Key = "ReasonCode", Label = "Mã lý do", Required = true, InitialValue = "CustomerRefused" },
            new() { Key = "Notes", Label = "Chi tiết", Kind = ModalFieldKind.Multiline }
        ],
        async vals => await _codApi.RecordFailureAsync(row.Id, new RecordCodFailureRequest
        {
            ReasonCode = vals["ReasonCode"]?.ToString() ?? string.Empty,
            Notes = vals["Notes"]?.ToString()
        }));
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadCollectionsAsync();
    }

    private async Task CreateRemittanceAsync()
    {
        var custs = await _crmApi.GetCustomersAsync(null, null, true, _branchContext.ActiveBranchId, new PagedRequest { Page = 1, PageSize = 50 });
        var dlg = new ModalFormDialog("Tạo bảng kê hoàn trả COD",
        [
            new() { Key = "CustomerId", Label = "Khách hàng", Kind = ModalFieldKind.ComboBox, Required = true, Options = custs.Items.Select(c => new ModalComboOption(c.Id, $"{c.CustomerCode} - {c.Name}")).ToList() },
            new() { Key = "CurrencyId", Label = "Currency ID (1=VND)", Kind = ModalFieldKind.Integer, InitialValue = 1 },
            new() { Key = "Method", Label = "Phương thức", Kind = ModalFieldKind.ComboBox, InitialValue = "BankTransfer", Options = [new("BankTransfer", "Chuyển khoản"), new("Cash", "Tiền mặt")] },
            new() { Key = "BankName", Label = "Tên ngân hàng", Required = true },
            new() { Key = "BankAccount", Label = "Số tài khoản", Required = true },
            new() { Key = "Holder", Label = "Tên chủ tài khoản", Required = true },
            new() { Key = "Fees", Label = "Phí dịch vụ khấu trừ", Kind = ModalFieldKind.Decimal, InitialValue = 0m }
        ],
        async vals => await _codApi.CreateRemittanceAsync(new CreateCodRemittanceRequest
        {
            CustomerId = Convert.ToInt64(vals["CustomerId"]),
            CurrencyId = Convert.ToInt64(vals["CurrencyId"] ?? 1),
            PaymentMethod = vals["Method"]?.ToString() ?? "BankTransfer",
            BankName = vals["BankName"]?.ToString() ?? string.Empty,
            BankAccount = vals["BankAccount"]?.ToString() ?? string.Empty,
            AccountHolderName = vals["Holder"]?.ToString() ?? string.Empty,
            ServiceFeesDeducted = Convert.ToDecimal(vals["Fees"] ?? 0m)
        }));
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadRemittancesAsync();
    }

    private async Task AddRemittanceItemAsync()
    {
        if (_selectedRemittance == null) return;
        var verifiedCols = await _codApi.GetCollectionsAsync(null, null, _selectedRemittance.CustomerId, "VerifiedByCashier", 1, 100);
        var dlg = new ModalFormDialog($"Thêm khoản COD vào {_selectedRemittance.RemittanceNumber}",
        [
            new() { Key = "CollectionId", Label = "Phiếu COD đã xác nhận", Kind = ModalFieldKind.ComboBox, Required = true, Options = verifiedCols.Items.Select(c => new ModalComboOption(c.Id, $"{c.ShipmentTrackingNumber} ({AppTheme.FormatMoney(c.CollectedAmount, c.CurrencyCode)})")).ToList() },
            new() { Key = "Fee", Label = "Cước khấu trừ (nếu có)", Kind = ModalFieldKind.Decimal, InitialValue = 0m }
        ],
        async vals => await _codApi.AddRemittanceItemAsync(_selectedRemittance.Id, new AddCodRemittanceItemRequest
        {
            CODCollectionId = Convert.ToInt64(vals["CollectionId"]),
            DeductedShippingFee = Convert.ToDecimal(vals["Fee"] ?? 0m)
        }));
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadRemittancesAsync();
    }

    private async Task ConfirmRemittanceAsync()
    {
        if (_selectedRemittance == null) return;
        var dlg = new ModalFormDialog($"Xác nhận hoàn trả {_selectedRemittance.RemittanceNumber}",
        [
            new() { Key = "Ref", Label = "Mã ủy nhiệm chi / chứng từ", Required = true },
            new() { Key = "Amount", Label = "Số tiền thực chi", Kind = ModalFieldKind.Decimal, InitialValue = _selectedRemittance.NetRemittanceAmount },
            new() { Key = "Notes", Label = "Ghi chú" }
        ],
        async vals => await _codApi.ConfirmRemittanceAsync(_selectedRemittance.Id, new ConfirmCodRemittanceRequest
        {
            PaymentReference = vals["Ref"]?.ToString(),
            ActualRemittedAmount = Convert.ToDecimal(vals["Amount"]),
            Notes = vals["Notes"]?.ToString()
        }));
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadRemittancesAsync();
    }

    private async Task CancelRemittanceAsync()
    {
        if (_selectedRemittance == null) return;
        var dlg = new ModalFormDialog($"Hủy bảng kê {_selectedRemittance.RemittanceNumber}",
        [
            new() { Key = "Reason", Label = "Lý do hủy", Kind = ModalFieldKind.Multiline, Required = true }
        ],
        async vals => await _codApi.CancelRemittanceAsync(_selectedRemittance.Id, new CancelCodRemittanceRequest { Reason = vals["Reason"]?.ToString() ?? string.Empty }));
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadRemittancesAsync();
    }

    private async Task ReconcileEmployeeAsync()
    {
        if (_reconEmployeeCombo.SelectedIndex < 0 || _reconEmployeeCombo.SelectedIndex >= _reconEmployeeCombo.Items.Count) return;
        if (_reconEmployeeCombo.Items[_reconEmployeeCombo.SelectedIndex] is not SelectItem item || item.Tag is not long empId) return;
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            var r = await _codApi.ReconcileEmployeeAsync(empId);
            _reconDetailLabel.Text =
                $"ĐỐI SOÁT TÀI XẾ: {r.EmployeeName} (ID {r.EmployeeId})\n\n" +
                $"• Số phiếu hoàn tất: {r.TotalCompletedCollections}\n" +
                $"• COD dự kiến: {AppTheme.FormatMoney(r.TotalExpected)}\n" +
                $"• Đã thu thực tế: {AppTheme.FormatMoney(r.TotalCollected)}\n" +
                $"• Đã nộp chi nhánh: {AppTheme.FormatMoney(r.TotalDeposited)}\n" +
                $"• Tiền tài xế còn giữ: {AppTheme.FormatMoney(r.DriverCashHolding)}";
        }, null, null, _logger);
    }

    private async Task ReconcileBranchAsync()
    {
        var bid = _branchContext.ActiveBranchId ?? _branchContext.AccessibleBranches.FirstOrDefault()?.BranchId ?? 0;
        if (bid <= 0) { UiAsyncRunner.ShowInfo(this, "Vui lòng chọn 1 chi nhánh trên thanh tiêu đề để đối soát chi nhánh.", "Thông báo"); return; }
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            var r = await _codApi.ReconcileBranchAsync(bid);
            _reconDetailLabel.Text =
                $"ĐỐI SOÁT CHI NHÁNH: {r.BranchCode} - {r.BranchName}\n\n" +
                $"• Số phiếu COD: {r.CollectionsCount}  |  Số bảng kê: {r.RemittancesCount}\n" +
                $"• Tổng thu tại chi nhánh: {AppTheme.FormatMoney(r.TotalCollectedAtBranch)}\n" +
                $"• Thủ quỹ đã xác nhận: {AppTheme.FormatMoney(r.TotalVerifiedAtBranch)}\n" +
                $"• Đã hoàn trả từ chi nhánh: {AppTheme.FormatMoney(r.TotalRemittedFromBranch)}\n" +
                $"• Tồn quỹ COD chi nhánh hiện giữ: {AppTheme.FormatMoney(r.CurrentBranchCashHolding)}";
        }, null, null, _logger);
    }

    public void ApplyTheme()
    {
        BackColor = AppTheme.SurfaceBg;
        _remItemsCard.Back = AppTheme.CardBg;
        _remItemsCard.BorderColor = AppTheme.BorderSubtle;
        _reconSummaryCard.Back = AppTheme.CardBg;
        _reconSummaryCard.BorderColor = AppTheme.BorderSubtle;
        _reconSummaryLabel.ForeColor = AppTheme.TextPrimary;
        _reconDetailCard.Back = AppTheme.CardBg;
        _reconDetailCard.BorderColor = AppTheme.BorderSubtle;
        _reconDetailLabel.ForeColor = AppTheme.TextPrimary;
    }
}
