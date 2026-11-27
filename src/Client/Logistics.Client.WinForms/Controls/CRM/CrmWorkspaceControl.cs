using AntdUI;
using Logistics.Client.Application.Contracts.Crm;
using Logistics.Client.Application.Interfaces;
using Logistics.Client.Application.Permissions;
using Logistics.Client.WinForms.Controls.Common;
using Logistics.Client.WinForms.Helpers;
using Logistics.Client.WinForms.Themes;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.Extensions.Logging;

namespace Logistics.Client.WinForms.Controls.CRM;

/// <summary>
/// AntdUI CRM workspace for managing Customers, Contacts, Addresses, Contracts, and Pricing Rules.
/// </summary>
public sealed class CrmWorkspaceControl : ModuleViewBase, IThemeAware
{
    private readonly ICrmApiClient _crmApi;
    private readonly IAuthorizationService _auth;
    private readonly IBranchContextService _branchContext;
    private readonly ILogger<CrmWorkspaceControl> _logger;

    private readonly PagedDataGridControl _customersGrid;
    private readonly AntdUI.Tabs _detailTabs = AppTheme.CreateAntdTabs();
    private readonly AntdUI.Table _contactsTable = AppTheme.CreateAntdTable();
    private readonly AntdUI.Table _addressesTable = AppTheme.CreateAntdTable();
    private readonly AntdUI.Table _contractsTable = AppTheme.CreateAntdTable();
    private readonly AntdUI.Table _pricingTable = AppTheme.CreateAntdTable();
    private readonly AntdUI.Alert _overviewAlert;
    private CustomerDto? _selectedCustomer;

    public CrmWorkspaceControl(
        ICrmApiClient crmApi,
        IAuthorizationService auth,
        IBranchContextService branchContext,
        ILogger<CrmWorkspaceControl> logger)
    {
        _crmApi = crmApi;
        _auth = auth;
        _branchContext = branchContext;
        _logger = logger;

        _overviewAlert = new AntdUI.Alert
        {
            Dock = DockStyle.Top,
            Height = 40,
            Radius = 6,
            Icon = TType.Info,
            Font = AppTheme.BodyBoldFont,
            Text = "Tổng quan khách hàng: Đang tải...",
            Margin = new Padding(0, 0, 0, 8)
        };

        _customersGrid = new PagedDataGridControl("Khách hàng (CRM)", "Quản lý hồ sơ khách hàng, liên hệ, địa chỉ, hợp đồng và chính sách giá.");
        if (_auth.HasPermission(ClientPermissions.Crm.CustomersCreate))
        {
            var addBtn = AppTheme.CreatePrimaryButton("Thêm KH", 105);
            addBtn.Click += async (_, _) => await CreateOrEditCustomerAsync(null);
            _customersGrid.AddActionButton(addBtn);
        }
        if (_auth.HasPermission(ClientPermissions.Crm.CustomersUpdate))
        {
            var editBtn = AppTheme.CreateSecondaryButton("Sửa KH", 90);
            editBtn.Click += async (_, _) =>
            {
                var c = _customersGrid.GetSelectedRow<CustomerDto>();
                if (c != null) await CreateOrEditCustomerAsync(c);
            };
            _customersGrid.AddActionButton(editBtn);
        }
        if (_auth.HasPermission(ClientPermissions.Crm.CustomersStatusManage))
        {
            var stBtn = AppTheme.CreateDangerButton("Bật/Tắt KH", 105);
            stBtn.Click += async (_, _) =>
            {
                var c = _customersGrid.GetSelectedRow<CustomerDto>();
                if (c == null) return;
                await UiAsyncRunner.RunSafeAsync(this, async () =>
                {
                    await _crmApi.ToggleCustomerStatusAsync(c.Id.ToString(), new UpdateCustomerStatusRequest(!c.IsActive));
                    await LoadCustomersAsync();
                }, logger: _logger);
            };
            _customersGrid.AddActionButton(stBtn);
        }

        _customersGrid.ReloadRequested += async (_, _) => await LoadCustomersAsync();
        _customersGrid.SelectionChanged += async (_, _) => await LoadCustomerDetailsAsync();

        BuildDetailTabs();

        var split = AppTheme.CreateSafeSplitContainer(Orientation.Horizontal, 350, 170, 170);
        split.Panel1.Controls.Add(_customersGrid);

        var detailCard = AppTheme.CreateCardPanel(new Padding(10));
        detailCard.Dock = DockStyle.Fill;
        detailCard.Controls.Add(_detailTabs);
        split.Panel2.Controls.Add(detailCard);

        Controls.Add(split);
        Controls.Add(_overviewAlert);
        ApplyTheme();
    }

    private void BuildDetailTabs()
    {
        // Contacts
        var tabContacts = new AntdUI.TabPage { Text = "Liên hệ" };
        var cBar = CreateSubToolbar();
        if (_auth.HasPermission(ClientPermissions.Crm.ContactsCreate))
        {
            var bAdd = AppTheme.CreatePrimaryButton("Thêm liên hệ", 120);
            bAdd.Click += async (_, _) => await AddContactAsync();
            cBar.Controls.Add(bAdd);
        }
        if (_auth.HasPermission(ClientPermissions.Crm.ContactsManage))
        {
            var bPri = AppTheme.CreateSuccessButton("Đặt làm chính", 120);
            bPri.Click += async (_, _) =>
            {
                var row = AppTheme.GetSelectedAntdRow<CustomerContactDto>(_contactsTable);
                if (row == null || _selectedCustomer == null) return;
                await UiAsyncRunner.RunSafeAsync(this, async () =>
                {
                    await _crmApi.SetPrimaryContactAsync(_selectedCustomer.Id.ToString(), row.Id.ToString());
                    await LoadCustomerDetailsAsync();
                }, logger: _logger);
            };
            var bDel = AppTheme.CreateDangerButton("Xóa", 76);
            bDel.Click += async (_, _) =>
            {
                var row = AppTheme.GetSelectedAntdRow<CustomerContactDto>(_contactsTable);
                if (row == null || _selectedCustomer == null || !UiAsyncRunner.Confirm(this, $"Xóa liên hệ {row.ContactName}?")) return;
                await UiAsyncRunner.RunSafeAsync(this, async () =>
                {
                    await _crmApi.DeleteContactAsync(_selectedCustomer.Id.ToString(), row.Id.ToString());
                    await LoadCustomerDetailsAsync();
                }, logger: _logger);
            };
            cBar.Controls.AddRange([bPri, bDel]);
        }
        tabContacts.Controls.Add(_contactsTable);
        tabContacts.Controls.Add(cBar);

        // Addresses
        var tabAddresses = new AntdUI.TabPage { Text = "Địa chỉ" };
        var aBar = CreateSubToolbar();
        if (_auth.HasPermission(ClientPermissions.Crm.AddressesCreate))
        {
            var bAdd = AppTheme.CreatePrimaryButton("Thêm địa chỉ", 120);
            bAdd.Click += async (_, _) => await AddAddressAsync();
            aBar.Controls.Add(bAdd);
        }
        if (_auth.HasPermission(ClientPermissions.Crm.AddressesManage))
        {
            var bDef = AppTheme.CreateSuccessButton("Đặt mặc định", 120);
            bDef.Click += async (_, _) =>
            {
                var row = AppTheme.GetSelectedAntdRow<CustomerAddressDto>(_addressesTable);
                if (row == null || _selectedCustomer == null) return;
                await UiAsyncRunner.RunSafeAsync(this, async () =>
                {
                    await _crmApi.SetDefaultAddressAsync(_selectedCustomer.Id.ToString(), row.Id.ToString(), new SetDefaultAddressRequest(true));
                    await LoadCustomerDetailsAsync();
                }, logger: _logger);
            };
            aBar.Controls.Add(bDef);
        }
        tabAddresses.Controls.Add(_addressesTable);
        tabAddresses.Controls.Add(aBar);

        // Contracts
        var tabContracts = new AntdUI.TabPage { Text = "Hợp đồng" };
        var ctBar = CreateSubToolbar();
        if (_auth.HasPermission(ClientPermissions.Crm.ContractsCreate))
        {
            var bAdd = AppTheme.CreatePrimaryButton("Thêm hợp đồng", 130);
            bAdd.Click += async (_, _) => await AddContractAsync();
            ctBar.Controls.Add(bAdd);
        }
        if (_auth.HasPermission(ClientPermissions.Crm.ContractsManage))
        {
            var bAct = AppTheme.CreateSuccessButton("Kích hoạt HĐ", 115);
            bAct.Click += async (_, _) =>
            {
                var row = AppTheme.GetSelectedAntdRow<CustomerContractDto>(_contractsTable);
                if (row == null || _selectedCustomer == null) return;
                await UiAsyncRunner.RunSafeAsync(this, async () =>
                {
                    await _crmApi.UpdateContractStatusAsync(_selectedCustomer.Id.ToString(), row.Id.ToString(), new UpdateCustomerContractStatusRequest("Active"));
                    await LoadCustomerDetailsAsync();
                }, logger: _logger);
            };
            ctBar.Controls.Add(bAct);
        }
        tabContracts.Controls.Add(_contractsTable);
        tabContracts.Controls.Add(ctBar);

        // Pricing Rules
        var tabPricing = new AntdUI.TabPage { Text = "Chính sách giá riêng" };
        var pBar = CreateSubToolbar();
        if (_auth.HasPermission(ClientPermissions.Crm.PricingRulesCreate))
        {
            var bAdd = AppTheme.CreatePrimaryButton("Thêm chính sách giá", 155);
            bAdd.Click += async (_, _) => await AddPricingRuleAsync();
            pBar.Controls.Add(bAdd);
        }
        if (_auth.HasPermission(ClientPermissions.Crm.PricingRulesManage))
        {
            var bTog = AppTheme.CreateSecondaryButton("Bật/Tắt quy tắc", 130);
            bTog.Click += async (_, _) =>
            {
                var row = AppTheme.GetSelectedAntdRow<CustomerPricingRuleDto>(_pricingTable);
                if (row == null || _selectedCustomer == null) return;
                await UiAsyncRunner.RunSafeAsync(this, async () =>
                {
                    await _crmApi.TogglePricingRuleStatusAsync(_selectedCustomer.Id.ToString(), row.Id.ToString(), new UpdateCustomerPricingRuleStatusRequest(!row.IsActive));
                    await LoadCustomerDetailsAsync();
                }, logger: _logger);
            };
            pBar.Controls.Add(bTog);
        }
        tabPricing.Controls.Add(_pricingTable);
        tabPricing.Controls.Add(pBar);

        _detailTabs.Pages.AddRange([tabContacts, tabAddresses, tabContracts, tabPricing]);
    }

    private static FlowLayoutPanel CreateSubToolbar()
        => new() { Dock = DockStyle.Top, Height = 44, Padding = new Padding(4), BackColor = Color.Transparent };

    protected override Task RefreshDataAsync() => LoadCustomersAsync();

    private async Task LoadCustomersAsync()
    {
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            _customersGrid.SetLoading(true);
            var paged = await _crmApi.GetCustomersAsync(_customersGrid.SearchText, null, null, _branchContext.ActiveBranchId,
                new PagedRequest { Page = _customersGrid.CurrentPage, PageSize = _customersGrid.CurrentPageSize });
            _customersGrid.BindPagedData(paged);

            if (_auth.HasPermission(ClientPermissions.Crm.CustomersOverviewRead))
            {
                var ov = await _crmApi.GetCustomerOverviewAsync();
                _overviewAlert.Text = $"Tổng KH ({ov.ScopeType}): {ov.TotalCustomers} | Đang hoạt động: {ov.ActiveCustomers} | Doanh nghiệp: {ov.CorporateCustomers} | Có HĐ hiệu lực: {ov.CustomersWithActiveContracts}";
            }
        }, logger: _logger, showMessageBoxOnError: false);
        _customersGrid.SetLoading(false);
    }

    private async Task LoadCustomerDetailsAsync()
    {
        _selectedCustomer = _customersGrid.GetSelectedRow<CustomerDto>();
        if (_selectedCustomer == null) return;

        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            var detail = await _crmApi.GetCustomerDetailAsync(_selectedCustomer.Id.ToString());
            AppTheme.BindAntdTable(_contactsTable, detail.Contacts);
            AppTheme.BindAntdTable(_addressesTable, detail.Addresses);
            AppTheme.BindAntdTable(_contractsTable, detail.Contracts);
            AppTheme.BindAntdTable(_pricingTable, detail.PricingRules);
        }, logger: _logger, showMessageBoxOnError: false);
    }

    private async Task CreateOrEditCustomerAsync(CustomerDto? existing)
    {
        var branches = _branchContext.AccessibleBranches.Select(b => new ModalComboOption(b.BranchId, $"{b.Code} - {b.Name}")).ToList();
        var fields = new List<ModalFieldSpec>();
        if (existing == null)
        {
            fields.Add(new ModalFieldSpec { Key = "BranchId", Label = "Chi nhánh", Kind = ModalFieldKind.ComboBox, Options = branches, InitialValue = _branchContext.ActiveBranchId });
            fields.Add(new ModalFieldSpec { Key = "CustomerCode", Label = "Mã KH", Required = true });
        }
        fields.AddRange([
            new ModalFieldSpec { Key = "CustomerType", Label = "Loại KH", Kind = ModalFieldKind.ComboBox, Required = true, InitialValue = existing?.CustomerType ?? "Corporate",
                Options = [new("Corporate", "Corporate"), new("Individual", "Individual")] },
            new ModalFieldSpec { Key = "Name", Label = "Tên khách hàng", Required = true, InitialValue = existing?.Name },
            new ModalFieldSpec { Key = "TaxCode", Label = "Mã số thuế", InitialValue = existing?.TaxCode },
            new ModalFieldSpec { Key = "Email", Label = "Email", Required = true, InitialValue = existing?.Email },
            new ModalFieldSpec { Key = "PhoneNumber", Label = "Điện thoại", Required = true, InitialValue = existing?.PhoneNumber },
            new ModalFieldSpec { Key = "CreditLimit", Label = "Hạn mức công nợ", Kind = ModalFieldKind.Decimal, InitialValue = existing?.CreditLimit ?? 0m },
            new ModalFieldSpec { Key = "CreditTermDays", Label = "Số ngày công nợ", Kind = ModalFieldKind.Integer, InitialValue = existing?.CreditTermDays ?? 30 },
            new ModalFieldSpec { Key = "PaymentType", Label = "Hình thức TT", Kind = ModalFieldKind.ComboBox, Required = true, InitialValue = existing?.PaymentType ?? "PostpaidMonthly",
                Options = [new("Prepaid", "Prepaid"), new("PostpaidMonthly", "PostpaidMonthly"), new("CodOffset", "CodOffset")] }
        ]);
        var dlg = new ModalFormDialog(existing == null ? "Thêm khách hàng" : $"Sửa {existing.CustomerCode}", fields, async v =>
        {
            if (existing == null)
                await _crmApi.CreateCustomerAsync(new CreateCustomerRequest(v["BranchId"] as long?, v["CustomerCode"]!.ToString()!, v["CustomerType"]!.ToString()!, v["Name"]!.ToString()!, null, v["TaxCode"]?.ToString(), v["Email"]!.ToString()!, v["PhoneNumber"]!.ToString()!, Convert.ToDecimal(v["CreditLimit"]), Convert.ToInt32(v["CreditTermDays"]), v["PaymentType"]!.ToString()!));
            else
                await _crmApi.UpdateCustomerAsync(existing.Id.ToString(), new UpdateCustomerRequest(v["CustomerType"]!.ToString()!, v["Name"]!.ToString()!, existing.TradeName, v["TaxCode"]?.ToString(), v["Email"]!.ToString()!, v["PhoneNumber"]!.ToString()!, Convert.ToDecimal(v["CreditLimit"]), Convert.ToInt32(v["CreditTermDays"]), v["PaymentType"]!.ToString()!, existing.AccountManagerEmployeeId));
        });
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadCustomersAsync();
    }

    private async Task AddContactAsync()
    {
        if (_selectedCustomer == null) return;
        var dlg = new ModalFormDialog("Thêm người liên hệ", [
            new ModalFieldSpec { Key = "ContactName", Label = "Họ tên", Required = true },
            new ModalFieldSpec { Key = "PhoneNumber", Label = "SĐT", Required = true },
            new ModalFieldSpec { Key = "Email", Label = "Email" },
            new ModalFieldSpec { Key = "JobTitle", Label = "Chức vụ" },
            new ModalFieldSpec { Key = "IsPrimary", Label = "Liên hệ chính", Kind = ModalFieldKind.CheckBox }
        ], async v => await _crmApi.CreateContactAsync(_selectedCustomer.Id.ToString(), new CreateCustomerContactRequest(v["ContactName"]!.ToString()!, v["PhoneNumber"]!.ToString()!, v["JobTitle"]?.ToString(), v["Email"]?.ToString(), v["IsPrimary"] is true)));
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadCustomerDetailsAsync();
    }

    private async Task AddAddressAsync()
    {
        if (_selectedCustomer == null) return;
        var dlg = new ModalFormDialog("Thêm địa chỉ khách hàng", [
            new ModalFieldSpec { Key = "AddressType", Label = "Loại địa chỉ", Kind = ModalFieldKind.ComboBox, Required = true,
                Options = [new("Pickup", "Pickup (Lấy hàng)"), new("Delivery", "Delivery (Giao hàng)"), new("Billing", "Billing (Hóa đơn)")] },
            new ModalFieldSpec { Key = "ContactName", Label = "Người nhận", Required = true },
            new ModalFieldSpec { Key = "PhoneNumber", Label = "SĐT", Required = true },
            new ModalFieldSpec { Key = "AddressLine1", Label = "Địa chỉ chi tiết", Required = true },
            new ModalFieldSpec { Key = "IsDefault", Label = "Đặt làm mặc định", Kind = ModalFieldKind.CheckBox }
        ], async v => await _crmApi.CreateAddressAsync(_selectedCustomer.Id.ToString(), new CreateCustomerAddressRequest(v["AddressType"]!.ToString()!, v["ContactName"]!.ToString()!, v["PhoneNumber"]!.ToString()!, v["AddressLine1"]!.ToString()!, IsDefault: v["IsDefault"] is true)));
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadCustomerDetailsAsync();
    }

    private async Task AddContractAsync()
    {
        if (_selectedCustomer == null) return;
        var dlg = new ModalFormDialog("Thêm hợp đồng", [
            new ModalFieldSpec { Key = "ContractNumber", Label = "Số hợp đồng", Required = true },
            new ModalFieldSpec { Key = "Title", Label = "Tiêu đề", Required = true },
            new ModalFieldSpec { Key = "StartDate", Label = "Từ ngày", Kind = ModalFieldKind.Date, Required = true, InitialValue = DateTime.Today },
            new ModalFieldSpec { Key = "EndDate", Label = "Đến ngày", Kind = ModalFieldKind.Date, Required = true, InitialValue = DateTime.Today.AddYears(1) },
            new ModalFieldSpec { Key = "DiscountRate", Label = "Chiết khấu (%)", Kind = ModalFieldKind.Decimal, InitialValue = 0m },
            new ModalFieldSpec { Key = "PaymentTermDays", Label = "Hạn thanh toán (ngày)", Kind = ModalFieldKind.Integer, InitialValue = 30 }
        ], async v => await _crmApi.CreateContractAsync(_selectedCustomer.Id.ToString(), new CreateCustomerContractRequest(
            v["ContractNumber"]!.ToString()!, v["Title"]!.ToString()!, DateOnly.FromDateTime((DateTime)v["StartDate"]!), DateOnly.FromDateTime((DateTime)v["EndDate"]!),
            "Draft", Convert.ToDecimal(v["DiscountRate"]), Convert.ToInt32(v["PaymentTermDays"]))));
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadCustomerDetailsAsync();
    }

    private async Task AddPricingRuleAsync()
    {
        if (_selectedCustomer == null) return;
        var dlg = new ModalFormDialog("Thêm quy tắc giá riêng", [
            new ModalFieldSpec { Key = "Name", Label = "Tên quy tắc", Required = true },
            new ModalFieldSpec { Key = "DiscountPercent", Label = "Giảm %", Kind = ModalFieldKind.Decimal, InitialValue = 0m },
            new ModalFieldSpec { Key = "FlatDiscountAmount", Label = "Giảm cố định", Kind = ModalFieldKind.Decimal, InitialValue = 0m },
            new ModalFieldSpec { Key = "Priority", Label = "Độ ưu tiên", Kind = ModalFieldKind.Integer, InitialValue = 1 },
            new ModalFieldSpec { Key = "EffectiveFrom", Label = "Hiệu lực từ", Kind = ModalFieldKind.Date, Required = true, InitialValue = DateTime.Today }
        ], async v => await _crmApi.CreatePricingRuleAsync(_selectedCustomer.Id.ToString(), new CreateCustomerPricingRuleRequest(
            v["Name"]!.ToString()!, (DateTime)v["EffectiveFrom"]!, DiscountPercent: Convert.ToDecimal(v["DiscountPercent"]), FlatDiscountAmount: Convert.ToDecimal(v["FlatDiscountAmount"]), Priority: Convert.ToInt32(v["Priority"]))));
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadCustomerDetailsAsync();
    }

    public void ApplyTheme()
    {
        BackColor = AppTheme.SurfaceBg;
    }
}
