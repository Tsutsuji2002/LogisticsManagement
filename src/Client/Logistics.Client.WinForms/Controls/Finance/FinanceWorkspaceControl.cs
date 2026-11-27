using Logistics.Client.Application.Contracts.Finance;
using Logistics.Client.Application.Interfaces;
using Logistics.Client.Application.Permissions;
using Logistics.Client.WinForms.Controls.Common;
using Logistics.Client.WinForms.Helpers;
using Logistics.Client.WinForms.Themes;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.Extensions.Logging;

namespace Logistics.Client.WinForms.Controls.Finance;

public sealed class FinanceWorkspaceControl : ModuleViewBase
{
    private readonly IFinanceApiClient _financeApi;
    private readonly ICrmApiClient _crmApi;
    private readonly IShippingApiClient _shippingApi;
    private readonly IAuthorizationService _auth;
    private readonly IBranchContextService _branchContext;
    private readonly ILogger<FinanceWorkspaceControl> _logger;

    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill, Font = AppTheme.BodyFont };
    private readonly TabPage _tabRates = new("Bảng giá, Tính cước & Phí vận đơn") { BackColor = AppTheme.SurfaceBg };
    private readonly TabPage _tabInvoices = new("Hóa đơn & Thanh toán") { BackColor = AppTheme.SurfaceBg };
    private readonly TabPage _tabBalances = new("Công nợ khách hàng & Sổ cái") { BackColor = AppTheme.SurfaceBg };

    private PagedDataGridControl _rateCardsGrid = null!;
    private readonly DataGridView _rulesGrid = new() { Dock = DockStyle.Fill };
    private PagedDataGridControl _chargesGrid = null!;
    private ShippingRateCardDto? _selectedCard;

    private PagedDataGridControl _invoicesGrid = null!;
    private readonly ComboBox _invStatusFilter = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130, Margin = new Padding(0, 4, 8, 0) };
    private readonly DataGridView _invoiceItemsGrid = new() { Dock = DockStyle.Fill };
    private PagedDataGridControl _paymentsGrid = null!;
    private InvoiceDto? _selectedInvoice;

    private PagedDataGridControl _balancesGrid = null!;
    private PagedDataGridControl _ledgerGrid = null!;

    public FinanceWorkspaceControl(
        IFinanceApiClient financeApi,
        ICrmApiClient crmApi,
        IShippingApiClient shippingApi,
        IAuthorizationService auth,
        IBranchContextService branchContext,
        ILogger<FinanceWorkspaceControl> logger)
    {
        _financeApi = financeApi;
        _crmApi = crmApi;
        _shippingApi = shippingApi;
        _auth = auth;
        _branchContext = branchContext;
        _logger = logger;

        BuildRatesAndChargesTab();
        BuildInvoicesAndPaymentsTab();
        BuildBalancesAndLedgerTab();

        if (_auth.HasAnyPermission(ClientPermissions.Finance.RatesRead, ClientPermissions.Finance.ChargesRead))
            _tabs.TabPages.Add(_tabRates);
        if (_auth.HasAnyPermission(ClientPermissions.Finance.InvoicesRead, ClientPermissions.Finance.PaymentsRead))
            _tabs.TabPages.Add(_tabInvoices);
        if (_auth.HasAnyPermission(ClientPermissions.Finance.BalancesRead, ClientPermissions.Finance.LedgerRead))
            _tabs.TabPages.Add(_tabBalances);

        _tabs.SelectedIndexChanged += async (_, _) => await RefreshDataAsync();
        Controls.Add(_tabs);
    }

    public void SelectSection(string navKey)
    {
        var tab = navKey switch
        {
            "Finance.Invoices" => _tabInvoices,
            "Finance.Balances" => _tabBalances,
            _ => _tabRates
        };
        if (_tabs.TabPages.Contains(tab)) _tabs.SelectedTab = tab;
    }

    private void BuildRatesAndChargesTab()
    {
        var split = AppTheme.CreateSafeSplitContainer(Orientation.Horizontal, 280, 180);
        var topSplit = AppTheme.CreateSafeSplitContainer(Orientation.Vertical, 540);

        _rateCardsGrid = new PagedDataGridControl("Bảng giá cước (ShippingRateCards)", "Quản lý bảng giá mặc định và bậc cước theo trọng lượng.");
        if (_auth.HasPermission(ClientPermissions.Finance.RatesManage))
        {
            var addCard = AppTheme.CreatePrimaryButton("Tạo bảng giá", 115);
            addCard.Click += async (_, _) => await CreateRateCardAsync();
            var togCard = AppTheme.CreateDangerButton("Bật/Tắt", 86);
            togCard.Click += async (_, _) =>
            {
                if (_selectedCard == null) return;
                await UiAsyncRunner.RunSafeAsync(this, async () =>
                {
                    await _financeApi.ToggleRateCardStatusAsync(_selectedCard.Id, !_selectedCard.IsActive);
                    await LoadRatesAndChargesAsync();
                }, togCard, null, _logger);
            };
            _rateCardsGrid.AddActionButton(addCard);
            _rateCardsGrid.AddActionButton(togCard);
        }
        if (_auth.HasPermission(ClientPermissions.Finance.ChargesCalculate))
        {
            var calcBtn = AppTheme.CreateSuccessButton("Tính cước Server", 130);
            calcBtn.Click += async (_, _) => await CalculateShippingFeeAsync();
            _rateCardsGrid.AddActionButton(calcBtn);
        }
        _rateCardsGrid.ReloadRequested += async (_, _) => await LoadRatesAndChargesAsync();
        _rateCardsGrid.SelectionChanged += async (_, _) =>
        {
            _selectedCard = _rateCardsGrid.GetSelectedRow<ShippingRateCardDto>();
            await LoadSelectedCardRulesAsync();
        };

        var rulesPanel = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.CardBg, Padding = new Padding(10) };
        var rulesTop = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 38 };
        rulesTop.Controls.Add(new Label { Text = "Quy tắc tính cước (ShippingRateRules)", Font = AppTheme.SectionTitleFont, AutoSize = true, Margin = new Padding(0, 8, 12, 0) });
        if (_auth.HasPermission(ClientPermissions.Finance.RatesManage))
        {
            var addRuleBtn = AppTheme.CreatePrimaryButton("Thêm Rule", 105);
            addRuleBtn.Click += async (_, _) => await CreateRateRuleAsync();
            rulesTop.Controls.Add(addRuleBtn);
        }
        AppTheme.StyleDataGrid(_rulesGrid);
        rulesPanel.Controls.Add(_rulesGrid);
        rulesPanel.Controls.Add(rulesTop);

        topSplit.Panel1.Controls.Add(_rateCardsGrid);
        topSplit.Panel2.Controls.Add(rulesPanel);

        _chargesGrid = new PagedDataGridControl("Phí vận đơn (ShipmentCharges)", "Server tự động kết xuất cước phí; hỗ trợ điều chỉnh phụ phí.");
        if (_auth.HasPermission(ClientPermissions.Finance.ChargesAdjust))
        {
            var adjBtn = AppTheme.CreateSecondaryButton("Điều chỉnh phí vận đơn", 165);
            adjBtn.Click += async (_, _) => await CreateChargeAdjustmentAsync();
            _chargesGrid.AddActionButton(adjBtn);
        }
        _chargesGrid.ReloadRequested += async (_, _) => await LoadRatesAndChargesAsync();

        split.Panel1.Controls.Add(topSplit);
        split.Panel2.Controls.Add(_chargesGrid);
        _tabRates.Controls.Add(split);
    }

    private void BuildInvoicesAndPaymentsTab()
    {
        var split = AppTheme.CreateSafeSplitContainer(Orientation.Horizontal, 320, 200);
        var invSplit = AppTheme.CreateSafeSplitContainer(Orientation.Vertical, 680);

        _invoicesGrid = new PagedDataGridControl("Hóa đơn khách hàng (Invoices - Server tính tổng & VAT)", "Mọi dòng tiền Subtotal / VAT / Paid / Outstanding đều do server tính.");
        _invStatusFilter.Items.AddRange(["", "Draft", "Issued", "PartiallyPaid", "Paid", "Overdue", "Cancelled"]);
        _invStatusFilter.SelectedIndex = 0;
        _invStatusFilter.SelectedIndexChanged += (_, _) => _invoicesGrid.ResetAndReload();
        _invoicesGrid.AddFilterControl(_invStatusFilter);

        if (_auth.HasPermission(ClientPermissions.Finance.InvoicesCreate))
        {
            var addInv = AppTheme.CreatePrimaryButton("Tạo hóa đơn", 115);
            addInv.Click += async (_, _) => await CreateInvoiceAsync();
            _invoicesGrid.AddActionButton(addInv);
        }
        if (_auth.HasPermission(ClientPermissions.Finance.InvoicesIssue))
        {
            var issInv = AppTheme.CreateSuccessButton("Phát hành (Issue)", 135);
            issInv.Click += async (_, _) =>
            {
                if (_selectedInvoice == null) return;
                await UiAsyncRunner.RunSafeAsync(this, async () =>
                {
                    await _financeApi.IssueInvoiceAsync(_selectedInvoice.Id);
                    await LoadInvoicesAndPaymentsAsync();
                }, issInv, null, _logger);
            };
            _invoicesGrid.AddActionButton(issInv);
        }
        if (_auth.HasPermission(ClientPermissions.Finance.InvoicesCancel))
        {
            var canInv = AppTheme.CreateDangerButton("Hủy hóa đơn", 115);
            canInv.Click += async (_, _) => await CancelSelectedInvoiceAsync();
            _invoicesGrid.AddActionButton(canInv);
        }
        _invoicesGrid.ReloadRequested += async (_, _) => await LoadInvoicesAndPaymentsAsync();
        _invoicesGrid.SelectionChanged += async (_, _) => await LoadInvoiceItemsAsync();

        var itemsPanel = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.CardBg, Padding = new Padding(10) };
        itemsPanel.Controls.Add(_invoiceItemsGrid);
        itemsPanel.Controls.Add(new Label { Text = "Chi tiết hạng mục hóa đơn (InvoiceItems)", Dock = DockStyle.Top, Height = 30, Font = AppTheme.SectionTitleFont });
        AppTheme.StyleDataGrid(_invoiceItemsGrid);

        invSplit.Panel1.Controls.Add(_invoicesGrid);
        invSplit.Panel2.Controls.Add(itemsPanel);

        _paymentsGrid = new PagedDataGridControl("Thanh toán & Phân bổ công nợ (Payments)", "Ghi nhận thanh toán từ khách hàng và phân bổ vào từng hóa đơn.");
        if (_auth.HasPermission(ClientPermissions.Finance.PaymentsManage))
        {
            var payBtn = AppTheme.CreatePrimaryButton("Ghi nhận thanh toán", 155);
            payBtn.Click += async (_, _) => await RegisterPaymentAsync();
            _paymentsGrid.AddActionButton(payBtn);
        }
        if (_auth.HasPermission(ClientPermissions.Finance.PaymentsAllocate))
        {
            var allocBtn = AppTheme.CreateSuccessButton("Phân bổ vào HĐ", 130);
            allocBtn.Click += async (_, _) => await AllocatePaymentAsync();
            _paymentsGrid.AddActionButton(allocBtn);
        }
        _paymentsGrid.ReloadRequested += async (_, _) => await LoadInvoicesAndPaymentsAsync();

        split.Panel1.Controls.Add(invSplit);
        split.Panel2.Controls.Add(_paymentsGrid);
        _tabInvoices.Controls.Add(split);
    }

    private void BuildBalancesAndLedgerTab()
    {
        var split = AppTheme.CreateSafeSplitContainer(Orientation.Horizontal, 280);
        _balancesGrid = new PagedDataGridControl("Công nợ khách hàng (CustomerBalances - Authoritative)", "Số dư công nợ, phí chưa xuất hóa đơn và quỹ giữ hộ COD.");
        if (_auth.HasPermission(ClientPermissions.Finance.BalancesReconcile))
        {
            var recBtn = AppTheme.CreatePrimaryButton("Đối soát công nợ KH", 160);
            recBtn.Click += async (_, _) => await ReconcileCustomerAsync();
            _balancesGrid.AddActionButton(recBtn);
        }
        _balancesGrid.ReloadRequested += async (_, _) => await LoadBalancesAndLedgerAsync();

        _ledgerGrid = new PagedDataGridControl("Sổ cái tài chính (FinancialLedgerEntries - Chỉ đọc)", "Nhật ký kế toán ghi nhận mọi phát sinh Nợ / Có và số dư lũy kế.");
        _ledgerGrid.ReloadRequested += async (_, _) => await LoadBalancesAndLedgerAsync();

        split.Panel1.Controls.Add(_balancesGrid);
        split.Panel2.Controls.Add(_ledgerGrid);
        _tabBalances.Controls.Add(split);
    }

    protected override async Task RefreshDataAsync()
    {
        if (_tabs.SelectedTab == _tabRates) await LoadRatesAndChargesAsync();
        else if (_tabs.SelectedTab == _tabInvoices) await LoadInvoicesAndPaymentsAsync();
        else if (_tabs.SelectedTab == _tabBalances) await LoadBalancesAndLedgerAsync();
    }

    private async Task LoadRatesAndChargesAsync()
    {
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            if (_auth.HasPermission(ClientPermissions.Finance.RatesRead))
            {
                var cards = await _financeApi.GetRateCardsAsync(_rateCardsGrid.SearchText, null, _rateCardsGrid.CurrentPage, _rateCardsGrid.CurrentPageSize);
                _rateCardsGrid.BindPagedData(cards);
                _selectedCard = _rateCardsGrid.GetSelectedRow<ShippingRateCardDto>();
                await LoadSelectedCardRulesAsync();
            }
            if (_auth.HasPermission(ClientPermissions.Finance.ChargesRead))
            {
                var charges = await _financeApi.GetChargesAsync(null, null, null, _chargesGrid.CurrentPage, _chargesGrid.CurrentPageSize);
                _chargesGrid.BindPagedData(charges);
            }
        }, null, null, _logger);
    }

    private async Task LoadSelectedCardRulesAsync()
    {
        if (_selectedCard == null) { _rulesGrid.DataSource = null; return; }
        var rules = await _financeApi.GetRateRulesAsync(_selectedCard.Id);
        _rulesGrid.DataSource = rules.ToList();
    }

    private async Task LoadInvoicesAndPaymentsAsync()
    {
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            if (_auth.HasPermission(ClientPermissions.Finance.InvoicesRead))
            {
                var status = _invStatusFilter.SelectedItem?.ToString();
                var invs = await _financeApi.GetInvoicesAsync(null, string.IsNullOrWhiteSpace(status) ? null : status, _invoicesGrid.CurrentPage, _invoicesGrid.CurrentPageSize);
                _invoicesGrid.BindPagedData(invs);
                await LoadInvoiceItemsAsync();
            }
            if (_auth.HasPermission(ClientPermissions.Finance.PaymentsRead))
            {
                var pays = await _financeApi.GetPaymentsAsync(null, null, _paymentsGrid.CurrentPage, _paymentsGrid.CurrentPageSize);
                _paymentsGrid.BindPagedData(pays);
            }
        }, null, null, _logger);
    }

    private async Task LoadInvoiceItemsAsync()
    {
        _selectedInvoice = _invoicesGrid.GetSelectedRow<InvoiceDto>();
        if (_selectedInvoice == null) { _invoiceItemsGrid.DataSource = null; return; }
        var full = await _financeApi.GetInvoiceByIdAsync(_selectedInvoice.Id);
        _invoiceItemsGrid.DataSource = full.Items;
    }

    private async Task LoadBalancesAndLedgerAsync()
    {
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            if (_auth.HasPermission(ClientPermissions.Finance.BalancesRead))
            {
                var bals = await _financeApi.GetCustomerBalancesAsync(_branchContext.ActiveBranchId, _balancesGrid.CurrentPage, _balancesGrid.CurrentPageSize);
                _balancesGrid.BindPagedData(bals);
            }
            if (_auth.HasPermission(ClientPermissions.Finance.LedgerRead))
            {
                var ledger = await _financeApi.GetLedgerEntriesAsync(null, null, _ledgerGrid.CurrentPage, _ledgerGrid.CurrentPageSize);
                _ledgerGrid.BindPagedData(ledger);
            }
        }, null, null, _logger);
    }

    private async Task CreateRateCardAsync()
    {
        var dlg = new ModalFormDialog("Tạo bảng giá mới",
        [
            new() { Key = "Code", Label = "Mã bảng giá", Required = true },
            new() { Key = "Name", Label = "Tên bảng giá", Required = true },
            new() { Key = "Desc", Label = "Mô tả" },
            new() { Key = "CurrencyId", Label = "Currency ID (1=VND)", Kind = ModalFieldKind.Integer, Required = true, InitialValue = 1 },
            new() { Key = "From", Label = "Hiệu lực từ", Kind = ModalFieldKind.Date, InitialValue = DateTime.Today },
            new() { Key = "IsDefault", Label = "Bảng giá mặc định", Kind = ModalFieldKind.CheckBox, InitialValue = false }
        ],
        async vals =>
        {
            await _financeApi.CreateRateCardAsync(new CreateShippingRateCardRequest
            {
                Code = vals["Code"]?.ToString() ?? string.Empty,
                Name = vals["Name"]?.ToString() ?? string.Empty,
                Description = vals["Desc"]?.ToString(),
                BaseCurrencyId = Convert.ToInt64(vals["CurrencyId"] ?? 1),
                EffectiveFrom = vals["From"] is DateTime dt ? dt : DateTime.Today,
                IsDefault = vals["IsDefault"] is true
            });
        });
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadRatesAndChargesAsync();
    }

    private async Task CreateRateRuleAsync()
    {
        if (_selectedCard == null) return;
        var dlg = new ModalFormDialog($"Thêm quy tắc cho {_selectedCard.Code}",
        [
            new() { Key = "ServiceType", Label = "Loại dịch vụ", Kind = ModalFieldKind.ComboBox, InitialValue = "StandardGround", Options = [new("StandardGround", "StandardGround"), new("ExpressAir", "ExpressAir"), new("SameDay", "SameDay")] },
            new() { Key = "FromKg", Label = "Từ (kg)", Kind = ModalFieldKind.Decimal, InitialValue = 0m },
            new() { Key = "ToKg", Label = "Đến (kg)", Kind = ModalFieldKind.Decimal, InitialValue = 50m },
            new() { Key = "BaseRate", Label = "Giá cơ bản", Kind = ModalFieldKind.Decimal, InitialValue = 25000m },
            new() { Key = "PerKg", Label = "Đơn giá / kg thêm", Kind = ModalFieldKind.Decimal, InitialValue = 5000m },
            new() { Key = "MinFee", Label = "Cước tối thiểu", Kind = ModalFieldKind.Decimal, InitialValue = 20000m }
        ],
        async vals =>
        {
            await _financeApi.CreateRateRuleAsync(_selectedCard.Id, new CreateShippingRateRuleRequest
            {
                ServiceType = vals["ServiceType"]?.ToString() ?? "StandardGround",
                FromWeightKg = Convert.ToDecimal(vals["FromKg"] ?? 0m),
                ToWeightKg = Convert.ToDecimal(vals["ToKg"] ?? 50m),
                BaseRate = Convert.ToDecimal(vals["BaseRate"] ?? 0m),
                AdditionalPerKgRate = Convert.ToDecimal(vals["PerKg"] ?? 0m),
                MinimumFee = Convert.ToDecimal(vals["MinFee"] ?? 0m)
            });
        });
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadSelectedCardRulesAsync();
    }

    private async Task CalculateShippingFeeAsync()
    {
        var custs = await _crmApi.GetCustomersAsync(null, null, true, _branchContext.ActiveBranchId, new PagedRequest { Page = 1, PageSize = 50 });
        var dlg = new ModalFormDialog("Tính cước phí qua Server Finance Engine",
        [
            new() { Key = "CustomerId", Label = "Khách hàng", Kind = ModalFieldKind.ComboBox, Required = true, Options = custs.Items.Select(c => new ModalComboOption(c.Id, $"{c.CustomerCode} - {c.Name}")).ToList() },
            new() { Key = "ServiceType", Label = "Dịch vụ", Kind = ModalFieldKind.ComboBox, InitialValue = "StandardGround", Options = [new("StandardGround", "StandardGround"), new("ExpressAir", "ExpressAir"), new("SameDay", "SameDay")] },
            new() { Key = "WeightKg", Label = "Trọng lượng (kg)", Kind = ModalFieldKind.Decimal, Required = true, InitialValue = 5m },
            new() { Key = "DeclaredValue", Label = "Giá trị khai báo", Kind = ModalFieldKind.Decimal, InitialValue = 1000000m },
            new() { Key = "CodAmount", Label = "Tiền thu hộ COD", Kind = ModalFieldKind.Decimal, InitialValue = 500000m }
        ],
        async vals =>
        {
            var r = await _financeApi.CalculateShippingFeeAsync(new CalculateShippingFeeRequest
            {
                CustomerId = Convert.ToInt64(vals["CustomerId"]),
                ServiceType = vals["ServiceType"]?.ToString() ?? "StandardGround",
                WeightKg = Convert.ToDecimal(vals["WeightKg"]),
                DeclaredValue = Convert.ToDecimal(vals["DeclaredValue"] ?? 0m),
                CodAmount = Convert.ToDecimal(vals["CodAmount"] ?? 0m)
            });
            UiAsyncRunner.ShowInfo(this,
                $"Bảng giá áp dụng: {r.AppliedRateCardCode ?? "-"}\nGiá cơ bản: {AppTheme.FormatMoney(r.BaseRate, r.CurrencyCode)}\nCước cân nặng: {AppTheme.FormatMoney(r.WeightCharge, r.CurrencyCode)}\nPhí bảo hiểm: {AppTheme.FormatMoney(r.InsuranceFee, r.CurrencyCode)}\nPhí COD: {AppTheme.FormatMoney(r.CodFee, r.CurrencyCode)}\nGiảm giá KH: -{AppTheme.FormatMoney(r.CustomerDiscountAmount, r.CurrencyCode)}\n=> TỔNG CƯỚC SERVER: {AppTheme.FormatMoney(r.TotalAmount, r.CurrencyCode)}",
                "Kết quả tính cước");
        }, "Tính cước");
        dlg.ShowDialog(this);
    }

    private async Task CreateChargeAdjustmentAsync()
    {
        var ships = await _shippingApi.GetShipmentsAsync(null, null, null, _branchContext.ActiveBranchId, null, null, null, new PagedRequest { Page = 1, PageSize = 50 });
        var dlg = new ModalFormDialog("Điều chỉnh phụ phí vận đơn",
        [
            new() { Key = "ShipmentId", Label = "Vận đơn", Kind = ModalFieldKind.ComboBox, Required = true, Options = ships.Items.Select(s => new ModalComboOption(s.Id, $"{s.TrackingNumber} - {s.CustomerName}")).ToList() },
            new() { Key = "ChargeType", Label = "Loại phí", Kind = ModalFieldKind.ComboBox, InitialValue = "Other", Options = [new("FuelSurcharge", "Phụ phí xăng dầu"), new("RemoteArea", "Vùng sâu vùng xa"), new("Adjustment", "Điều chỉnh"), new("Other", "Khác")] },
            new() { Key = "Amount", Label = "Số tiền (+/-)", Kind = ModalFieldKind.Decimal, Required = true, InitialValue = 20000m },
            new() { Key = "CurrencyId", Label = "Currency ID (1=VND)", Kind = ModalFieldKind.Integer, InitialValue = 1 },
            new() { Key = "Desc", Label = "Diễn giải", Required = true }
        ],
        async vals =>
        {
            await _financeApi.CreateChargeAdjustmentAsync(Convert.ToInt64(vals["ShipmentId"]), new CreateChargeAdjustmentRequest
            {
                ChargeType = vals["ChargeType"]?.ToString() ?? "Other",
                Amount = Convert.ToDecimal(vals["Amount"]),
                CurrencyId = Convert.ToInt64(vals["CurrencyId"] ?? 1),
                Description = vals["Desc"]?.ToString() ?? string.Empty
            });
        });
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadRatesAndChargesAsync();
    }

    private async Task CreateInvoiceAsync()
    {
        var custs = await _crmApi.GetCustomersAsync(null, null, true, _branchContext.ActiveBranchId, new PagedRequest { Page = 1, PageSize = 50 });
        var dlg = new ModalFormDialog("Tạo hóa đơn nháp (Draft Invoice)",
        [
            new() { Key = "CustomerId", Label = "Khách hàng", Kind = ModalFieldKind.ComboBox, Required = true, Options = custs.Items.Select(c => new ModalComboOption(c.Id, $"{c.CustomerCode} - {c.Name}")).ToList() },
            new() { Key = "InvoiceDate", Label = "Ngày hóa đơn", Kind = ModalFieldKind.Date, InitialValue = DateTime.Today },
            new() { Key = "TaxRate", Label = "Thuế suất VAT (%)", Kind = ModalFieldKind.Decimal, InitialValue = 8m },
            new() { Key = "Terms", Label = "Hạn thanh toán (ngày)", Kind = ModalFieldKind.Integer, InitialValue = 30 },
            new() { Key = "ItemDesc", Label = "Nội dung hạng mục 1", Required = true, InitialValue = "Cước dịch vụ vận chuyển" },
            new() { Key = "Qty", Label = "Số lượng", Kind = ModalFieldKind.Integer, InitialValue = 1 },
            new() { Key = "UnitPrice", Label = "Đơn giá", Kind = ModalFieldKind.Decimal, Required = true, InitialValue = 100000m },
            new() { Key = "Notes", Label = "Ghi chú" }
        ],
        async vals =>
        {
            await _financeApi.CreateInvoiceAsync(new CreateInvoiceRequest
            {
                CustomerId = Convert.ToInt64(vals["CustomerId"]),
                InvoiceDate = DateOnly.FromDateTime(vals["InvoiceDate"] is DateTime dt ? dt : DateTime.Today),
                TaxRate = Convert.ToDecimal(vals["TaxRate"] ?? 8m),
                PaymentTermsDays = Convert.ToInt32(vals["Terms"] ?? 30),
                Notes = vals["Notes"]?.ToString(),
                Items =
                [
                    new CreateInvoiceItemRequest
                    {
                        Description = vals["ItemDesc"]?.ToString() ?? string.Empty,
                        Quantity = Convert.ToInt32(vals["Qty"] ?? 1),
                        UnitPrice = Convert.ToDecimal(vals["UnitPrice"])
                    }
                ]
            });
        });
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadInvoicesAndPaymentsAsync();
    }

    private async Task CancelSelectedInvoiceAsync()
    {
        if (_selectedInvoice == null) return;
        var dlg = new ModalFormDialog($"Hủy hóa đơn {_selectedInvoice.InvoiceNumber}",
        [
            new() { Key = "Reason", Label = "Lý do hủy", Kind = ModalFieldKind.Multiline, Required = true }
        ],
        async vals => await _financeApi.CancelInvoiceAsync(_selectedInvoice.Id, new CancelInvoiceRequest { Reason = vals["Reason"]?.ToString() ?? string.Empty }));
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadInvoicesAndPaymentsAsync();
    }

    private async Task RegisterPaymentAsync()
    {
        var custs = await _crmApi.GetCustomersAsync(null, null, true, _branchContext.ActiveBranchId, new PagedRequest { Page = 1, PageSize = 50 });
        var dlg = new ModalFormDialog("Ghi nhận thanh toán (Payment)",
        [
            new() { Key = "CustomerId", Label = "Khách hàng", Kind = ModalFieldKind.ComboBox, Required = true, InitialValue = _selectedInvoice?.CustomerId, Options = custs.Items.Select(c => new ModalComboOption(c.Id, $"{c.CustomerCode} - {c.Name}")).ToList() },
            new() { Key = "Method", Label = "Phương thức", Kind = ModalFieldKind.ComboBox, InitialValue = "BankTransfer", Options = [new("BankTransfer", "Chuyển khoản"), new("Cash", "Tiền mặt"), new("Card", "Thẻ"), new("Wallet", "Ví")] },
            new() { Key = "Amount", Label = "Số tiền thanh toán", Kind = ModalFieldKind.Decimal, Required = true, InitialValue = _selectedInvoice?.OutstandingAmount ?? 100000m },
            new() { Key = "CurrencyId", Label = "Currency ID (1=VND)", Kind = ModalFieldKind.Integer, InitialValue = 1 },
            new() { Key = "BankName", Label = "Ngân hàng" },
            new() { Key = "TxRef", Label = "Mã giao dịch NH" },
            new() { Key = "AutoAllocInvoiceId", Label = "Phân bổ ngay vào HĐ ID (nếu có)", Kind = ModalFieldKind.Integer, InitialValue = _selectedInvoice != null ? (int)_selectedInvoice.Id : 0 }
        ],
        async vals =>
        {
            var amount = Convert.ToDecimal(vals["Amount"]);
            var allocInv = Convert.ToInt64(vals["AutoAllocInvoiceId"] ?? 0);
            await _financeApi.RegisterPaymentAsync(new RegisterPaymentRequest
            {
                CustomerId = Convert.ToInt64(vals["CustomerId"]),
                PaymentMethod = vals["Method"]?.ToString() ?? "BankTransfer",
                Amount = amount,
                CurrencyId = Convert.ToInt64(vals["CurrencyId"] ?? 1),
                BankName = vals["BankName"]?.ToString(),
                TransactionReference = vals["TxRef"]?.ToString(),
                InitialAllocations = allocInv > 0 ? [new CreatePaymentAllocationRequest { InvoiceId = allocInv, Amount = amount }] : null
            });
        });
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadInvoicesAndPaymentsAsync();
    }

    private async Task AllocatePaymentAsync()
    {
        var pay = _paymentsGrid.GetSelectedRow<PaymentDto>();
        if (pay == null || _selectedInvoice == null)
        {
            UiAsyncRunner.ShowInfo(this, "Vui lòng chọn cả 1 Hóa đơn ở trên và 1 Thanh toán ở dưới để phân bổ.", "Hướng dẫn");
            return;
        }
        var suggested = Math.Min(pay.UnallocatedAmount, _selectedInvoice.OutstandingAmount);
        var dlg = new ModalFormDialog($"Phân bổ {pay.PaymentNumber} -> {_selectedInvoice.InvoiceNumber}",
        [
            new() { Key = "Amount", Label = "Số tiền phân bổ", Kind = ModalFieldKind.Decimal, Required = true, InitialValue = suggested > 0 ? suggested : pay.UnallocatedAmount }
        ],
        async vals => await _financeApi.AllocatePaymentAsync(pay.Id, new CreatePaymentAllocationRequest
        {
            InvoiceId = _selectedInvoice.Id,
            Amount = Convert.ToDecimal(vals["Amount"])
        }));
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadInvoicesAndPaymentsAsync();
    }

    private async Task ReconcileCustomerAsync()
    {
        var row = _balancesGrid.GetSelectedRow<CustomerBalanceDto>();
        if (row == null) return;
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            var res = await _financeApi.ReconcileCustomerBalanceAsync(row.CustomerId);
            UiAsyncRunner.ShowInfo(this,
                $"Khách hàng: {res.CustomerCode}\nSố dư ghi nhận: {AppTheme.FormatMoney(res.RecordedOutstandingBalance)}\nSố dư tính lại (Server): {AppTheme.FormatMoney(res.RecomputedOutstandingBalance)}\nKhớp số liệu: {(res.IsConsistent ? "CÓ" : "LỆCH")}\n{res.Details}",
                "Kết quả đối soát công nợ");
            await LoadBalancesAndLedgerAsync();
        }, null, null, _logger);
    }
}
