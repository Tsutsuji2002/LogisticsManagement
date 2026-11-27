using AntdUI;
using Logistics.Client.Application.Contracts.Auth;
using Logistics.Client.Application.Contracts.Organization;
using Logistics.Client.Application.Interfaces;
using Logistics.Client.Application.Permissions;
using Logistics.Client.WinForms.Controls.Common;
using Logistics.Client.WinForms.Helpers;
using Logistics.Client.WinForms.Themes;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.Extensions.Logging;
using Label = System.Windows.Forms.Label;

namespace Logistics.Client.WinForms.Controls.Organization;

/// <summary>
/// AntdUI Organization, HR, User Accounts &amp; Roles/Permissions workspace.
/// </summary>
public sealed class OrganizationWorkspaceControl : ModuleViewBase, IThemeAware
{
    private readonly IOrganizationApiClient _orgApi;
    private readonly IIdentityAdminApiClient _identityApi;
    private readonly IAuthorizationService _auth;
    private readonly IBranchContextService _branchContext;
    private readonly ILogger<OrganizationWorkspaceControl> _logger;

    private readonly AntdUI.Tabs _tabs = AppTheme.CreateAntdTabs();
    private readonly AntdUI.TabPage _tabCompany = new() { Text = "Công ty & Tổng quan" };
    private readonly AntdUI.TabPage _tabBranches = new() { Text = "Chi nhánh" };
    private readonly AntdUI.TabPage _tabDepts = new() { Text = "Phòng ban" };
    private readonly AntdUI.TabPage _tabEmployees = new() { Text = "Nhân viên" };
    private readonly AntdUI.TabPage _tabUsers = new() { Text = "Tài khoản người dùng" };
    private readonly AntdUI.TabPage _tabRoles = new() { Text = "Vai trò & Phân quyền" };

    private AntdUI.Panel _companyCard = null!;
    private Label _companyTitleLabel = null!;
    private Label _companyInfoLabel = null!;
    private AntdUI.Button _editCompanyBtn = null!;
    private PagedDataGridControl _branchesGrid = null!;
    private PagedDataGridControl _deptsGrid = null!;
    private PagedDataGridControl _employeesGrid = null!;
    private PagedDataGridControl _usersGrid = null!;
    private AntdUI.Table _rolesTable = null!;
    private AntdUI.Table _permissionsTable = null!;

    public OrganizationWorkspaceControl(
        IOrganizationApiClient orgApi,
        IIdentityAdminApiClient identityApi,
        IAuthorizationService auth,
        IBranchContextService branchContext,
        ILogger<OrganizationWorkspaceControl> logger)
    {
        _orgApi = orgApi;
        _identityApi = identityApi;
        _auth = auth;
        _branchContext = branchContext;
        _logger = logger;

        BuildCompanyTab();
        BuildBranchesTab();
        BuildDeptsTab();
        BuildEmployeesTab();
        BuildUsersTab();
        BuildRolesTab();

        if (_auth.HasPermission(ClientPermissions.Organization.CompanyRead)) _tabs.Pages.Add(_tabCompany);
        if (_auth.HasPermission(ClientPermissions.Organization.BranchesRead)) _tabs.Pages.Add(_tabBranches);
        if (_auth.HasPermission(ClientPermissions.Organization.DepartmentsRead)) _tabs.Pages.Add(_tabDepts);
        if (_auth.HasPermission(ClientPermissions.Employees.Read)) _tabs.Pages.Add(_tabEmployees);
        if (_auth.HasPermission(ClientPermissions.Identity.UsersRead)) _tabs.Pages.Add(_tabUsers);
        if (_auth.HasPermission(ClientPermissions.Identity.RolesRead)) _tabs.Pages.Add(_tabRoles);

        _tabs.SelectedIndexChanged += async (_, _) => await RefreshActiveTabAsync();
        Controls.Add(_tabs);
        ApplyTheme();
    }

    public void SelectSection(string navKey)
    {
        var tab = navKey switch
        {
            "Org.Company" => _tabCompany,
            "Org.Branches" => _tabBranches,
            "Org.Departments" => _tabDepts,
            "Org.Employees" => _tabEmployees,
            "Org.Users" => _tabUsers,
            "Org.Roles" => _tabRoles,
            _ => _tabCompany
        };
        if (_tabs.Pages.Contains(tab))
            _tabs.SelectedTab = tab;
    }

    private void BuildCompanyTab()
    {
        _tabCompany.Padding = new Padding(4);
        _companyCard = AppTheme.CreateCardPanel(new Padding(22));
        _companyCard.Dock = DockStyle.Fill;

        _companyTitleLabel = new Label
        {
            Text = "Hồ sơ Doanh nghiệp & Tổng quan Cơ cấu",
            Font = AppTheme.PageTitleFont,
            Dock = DockStyle.Top,
            Height = 38,
            BackColor = Color.Transparent
        };
        _companyInfoLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 200,
            Font = AppTheme.BodyFont,
            Padding = new Padding(0, 12, 0, 12),
            BackColor = Color.Transparent
        };

        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, BackColor = Color.Transparent };
        _editCompanyBtn = AppTheme.CreatePrimaryButton("Sửa thông tin công ty", 180);
        _editCompanyBtn.Enabled = _auth.HasPermission(ClientPermissions.Organization.CompanyUpdate);
        _editCompanyBtn.Click += async (_, _) => await EditCompanyAsync();

        var refreshBtn = AppTheme.CreateSecondaryButton("Tải lại", 105);
        refreshBtn.Click += async (_, _) => await LoadCompanyAsync();

        bar.Controls.AddRange([_editCompanyBtn, refreshBtn]);
        _companyCard.Controls.Add(bar);
        _companyCard.Controls.Add(_companyInfoLabel);
        _companyCard.Controls.Add(_companyTitleLabel);
        _tabCompany.Controls.Add(_companyCard);
    }

    private void BuildBranchesTab()
    {
        _branchesGrid = new PagedDataGridControl("Chi nhánh vận hành", "Quản lý danh sách chi nhánh, loại hình và trạng thái hoạt động.");
        if (_auth.HasPermission(ClientPermissions.Organization.BranchesCreate))
        {
            var addBtn = AppTheme.CreatePrimaryButton("Thêm chi nhánh", 140);
            addBtn.Click += async (_, _) => await CreateOrEditBranchAsync(null);
            _branchesGrid.AddActionButton(addBtn);
        }
        if (_auth.HasPermission(ClientPermissions.Organization.BranchesUpdate))
        {
            var editBtn = AppTheme.CreateSecondaryButton("Sửa", 80);
            editBtn.Click += async (_, _) =>
            {
                var row = _branchesGrid.GetSelectedRow<BranchDto>();
                if (row != null) await CreateOrEditBranchAsync(row);
            };
            _branchesGrid.AddActionButton(editBtn);
        }
        if (_auth.HasPermission(ClientPermissions.Organization.BranchesManage))
        {
            var toggleBtn = AppTheme.CreateDangerButton("Bật/Tắt TT", 110);
            toggleBtn.Click += async (_, _) =>
            {
                var row = _branchesGrid.GetSelectedRow<BranchDto>();
                if (row == null) return;
                if (!UiAsyncRunner.Confirm(this, $"{(row.IsActive ? "Ngưng hoạt động" : "Kích hoạt")} chi nhánh {row.Code}?")) return;
                await UiAsyncRunner.RunSafeAsync(this, async () =>
                {
                    await _orgApi.ToggleBranchStatusAsync(row.Id.ToString(), new UpdateBranchStatusRequest(!row.IsActive, "Cập nhật từ Desktop"));
                    await LoadBranchesAsync();
                }, logger: _logger);
            };
            _branchesGrid.AddActionButton(toggleBtn);
        }
        var detailBtn = AppTheme.CreateSecondaryButton("Chi tiết", 90);
        detailBtn.Click += async (_, _) =>
        {
            var row = _branchesGrid.GetSelectedRow<BranchDto>();
            if (row == null) return;
            await UiAsyncRunner.RunSafeAsync(this, async () =>
            {
                var d = await _orgApi.GetBranchDetailAsync(row.Id.ToString());
                UiAsyncRunner.ShowInfo(this,
                    $"Mã: {d.Branch.Code} - {d.Branch.Name}\nLoại: {d.Branch.BranchType} | ĐT: {d.Branch.Phone}\nNhân viên: {d.EmployeeCount} | Phòng ban: {d.DepartmentCount} | Kho: {d.WarehouseCount}",
                    "Chi tiết chi nhánh");
            }, logger: _logger);
        };
        _branchesGrid.AddActionButton(detailBtn);
        _branchesGrid.ReloadRequested += async (_, _) => await LoadBranchesAsync();
        _tabBranches.Controls.Add(_branchesGrid);
    }

    private void BuildDeptsTab()
    {
        _deptsGrid = new PagedDataGridControl("Phòng ban", "Danh sách phòng ban theo chi nhánh.");
        if (_auth.HasPermission(ClientPermissions.Organization.DepartmentsCreate))
        {
            var addBtn = AppTheme.CreatePrimaryButton("Thêm phòng ban", 145);
            addBtn.Click += async (_, _) => await CreateOrEditDeptAsync(null);
            _deptsGrid.AddActionButton(addBtn);
        }
        if (_auth.HasPermission(ClientPermissions.Organization.DepartmentsUpdate))
        {
            var editBtn = AppTheme.CreateSecondaryButton("Sửa", 80);
            editBtn.Click += async (_, _) =>
            {
                var row = _deptsGrid.GetSelectedRow<DepartmentDto>();
                if (row != null) await CreateOrEditDeptAsync(row);
            };
            _deptsGrid.AddActionButton(editBtn);
        }
        if (_auth.HasPermission(ClientPermissions.Organization.DepartmentsManage))
        {
            var toggleBtn = AppTheme.CreateDangerButton("Bật/Tắt TT", 110);
            toggleBtn.Click += async (_, _) =>
            {
                var row = _deptsGrid.GetSelectedRow<DepartmentDto>();
                if (row == null) return;
                await UiAsyncRunner.RunSafeAsync(this, async () =>
                {
                    await _orgApi.ToggleDepartmentStatusAsync(row.Id.ToString(), new UpdateDepartmentStatusRequest(!row.IsActive));
                    await LoadDeptsAsync();
                }, logger: _logger);
            };
            _deptsGrid.AddActionButton(toggleBtn);
        }
        _deptsGrid.ReloadRequested += async (_, _) => await LoadDeptsAsync();
        _tabDepts.Controls.Add(_deptsGrid);
    }

    private void BuildEmployeesTab()
    {
        _employeesGrid = new PagedDataGridControl("Hồ sơ nhân viên", "Quản lý nhân sự và liên kết tài khoản người dùng.");
        if (_auth.HasPermission(ClientPermissions.Employees.Create))
        {
            var addBtn = AppTheme.CreatePrimaryButton("Thêm nhân viên", 145);
            addBtn.Click += async (_, _) => await CreateOrEditEmployeeAsync(null);
            _employeesGrid.AddActionButton(addBtn);
        }
        if (_auth.HasPermission(ClientPermissions.Employees.Update))
        {
            var editBtn = AppTheme.CreateSecondaryButton("Sửa hồ sơ", 100);
            editBtn.Click += async (_, _) =>
            {
                var row = _employeesGrid.GetSelectedRow<EmployeeDto>();
                if (row != null) await CreateOrEditEmployeeAsync(row);
            };
            _employeesGrid.AddActionButton(editBtn);
        }
        if (_auth.HasPermission(ClientPermissions.Employees.AccountCreate))
        {
            var linkBtn = AppTheme.CreateSuccessButton("Cấp tài khoản", 125);
            linkBtn.Click += async (_, _) =>
            {
                var row = _employeesGrid.GetSelectedRow<EmployeeDto>();
                if (row == null) return;
                if (row.HasUserAccount) { UiAsyncRunner.ShowInfo(this, $"Nhân viên đã có tài khoản: {row.Username}"); return; }
                await CreateEmployeeAccountAsync(row);
            };
            _employeesGrid.AddActionButton(linkBtn);
        }
        if (_auth.HasPermission(ClientPermissions.Employees.StatusManage))
        {
            var stBtn = AppTheme.CreateDangerButton("Đổi trạng thái", 125);
            stBtn.Click += async (_, _) =>
            {
                var row = _employeesGrid.GetSelectedRow<EmployeeDto>();
                if (row == null) return;
                var dlg = new ModalFormDialog("Trạng thái nhân viên", [
                    new ModalFieldSpec { Key = "Status", Label = "Trạng thái", Kind = ModalFieldKind.ComboBox, Required = true, InitialValue = row.Status,
                        Options = [new("Active","Active"), new("OnLeave","OnLeave"), new("Resigned","Resigned"), new("Terminated","Terminated")] },
                    new ModalFieldSpec { Key = "Reason", Label = "Lý do", Kind = ModalFieldKind.Text }
                ], async v => await _orgApi.UpdateEmployeeStatusAsync(row.Id.ToString(), new UpdateEmployeeStatusRequest(v["Status"]!.ToString()!, v["Reason"]?.ToString())));
                if (dlg.ShowDialog(this) == DialogResult.OK) await LoadEmployeesAsync();
            };
            _employeesGrid.AddActionButton(stBtn);
        }
        _employeesGrid.ReloadRequested += async (_, _) => await LoadEmployeesAsync();
        _tabEmployees.Controls.Add(_employeesGrid);
    }

    private void BuildUsersTab()
    {
        _usersGrid = new PagedDataGridControl("Tài khoản người dùng", "Quản trị tài khoản, vai trò, chi nhánh được cấp và đặt lại mật khẩu.");
        if (_auth.HasPermission(ClientPermissions.Identity.UsersCreate))
        {
            var addBtn = AppTheme.CreatePrimaryButton("Tạo tài khoản", 130);
            addBtn.Click += async (_, _) => await CreateUserAsync();
            _usersGrid.AddActionButton(addBtn);
        }
        if (_auth.HasPermission(ClientPermissions.Identity.UsersRolesManage))
        {
            var roleBtn = AppTheme.CreateSecondaryButton("Gán vai trò", 110);
            roleBtn.Click += async (_, _) => await ManageUserRolesAsync();
            _usersGrid.AddActionButton(roleBtn);
        }
        if (_auth.HasPermission(ClientPermissions.Identity.UsersBranchesManage))
        {
            var brBtn = AppTheme.CreateSecondaryButton("Gán chi nhánh", 120);
            brBtn.Click += async (_, _) => await ManageUserBranchesAsync();
            _usersGrid.AddActionButton(brBtn);
        }
        if (_auth.HasPermission(ClientPermissions.Identity.UsersResetPassword))
        {
            var pwdBtn = AppTheme.CreateDangerButton("Đặt lại MK", 110);
            pwdBtn.Click += (_, _) => ResetUserPassword();
            _usersGrid.AddActionButton(pwdBtn);
        }
        if (_auth.HasPermission(ClientPermissions.Identity.UsersUpdate))
        {
            var stBtn = AppTheme.CreateDangerButton("Khóa/Mở TK", 110);
            stBtn.Click += async (_, _) =>
            {
                var u = _usersGrid.GetSelectedRow<UserAccountDto>();
                if (u == null) return;
                await UiAsyncRunner.RunSafeAsync(this, async () =>
                {
                    await _identityApi.ToggleUserStatusAsync(u.PublicId, new UpdateUserStatusRequest(!u.IsActive, true));
                    await LoadUsersAsync();
                }, logger: _logger);
            };
            _usersGrid.AddActionButton(stBtn);
        }
        _usersGrid.ReloadRequested += async (_, _) => await LoadUsersAsync();
        _tabUsers.Controls.Add(_usersGrid);
    }

    private void BuildRolesTab()
    {
        var split = AppTheme.CreateSafeSplitContainer(Orientation.Vertical, 520, 260, 320);

        var rolesCard = AppTheme.CreateCardPanel(new Padding(12));
        rolesCard.Dock = DockStyle.Fill;
        var rolesHdr = new AntPageHeaderControl("Vai trò hệ thống (Roles)", "Danh sách Role RBAC");
        var refreshBtn = AppTheme.CreateSecondaryButton("Tải lại", 92);
        refreshBtn.Click += async (_, _) => await LoadRolesAndPermissionsAsync();
        rolesHdr.AddAction(refreshBtn);

        _rolesTable = AppTheme.CreateAntdTable();
        rolesCard.Controls.Add(_rolesTable);
        rolesCard.Controls.Add(rolesHdr);

        var permCard = AppTheme.CreateCardPanel(new Padding(12));
        permCard.Dock = DockStyle.Fill;
        var permHdr = new AntPageHeaderControl("Danh mục quyền hạn (Permissions)", "Mã quyền RBAC chuẩn Backend");
        _permissionsTable = AppTheme.CreateAntdTable();
        permCard.Controls.Add(_permissionsTable);
        permCard.Controls.Add(permHdr);

        split.Panel1.Controls.Add(rolesCard);
        split.Panel2.Controls.Add(permCard);
        _tabRoles.Controls.Add(split);
    }

    protected override async Task RefreshDataAsync() => await RefreshActiveTabAsync();

    private async Task RefreshActiveTabAsync()
    {
        if (_tabs.SelectedTab == _tabCompany) await LoadCompanyAsync();
        else if (_tabs.SelectedTab == _tabBranches) await LoadBranchesAsync();
        else if (_tabs.SelectedTab == _tabDepts) await LoadDeptsAsync();
        else if (_tabs.SelectedTab == _tabEmployees) await LoadEmployeesAsync();
        else if (_tabs.SelectedTab == _tabUsers) await LoadUsersAsync();
        else if (_tabs.SelectedTab == _tabRoles) await LoadRolesAndPermissionsAsync();
    }

    private async Task LoadCompanyAsync()
    {
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            var c = await _orgApi.GetCompanyAsync();
            OrganizationOverviewDto? ov = _auth.HasPermission(ClientPermissions.Organization.OverviewRead)
                ? await _orgApi.GetOverviewAsync() : null;
            if (c == null) { _companyInfoLabel.Text = "Không tìm thấy thông tin công ty."; return; }
            _companyInfoLabel.Text =
                $"Mã công ty: {c.Code} | Tên pháp lý: {c.LegalName}\nTên thương mại: {c.TradingName} | MST: {c.TaxNumber}\n" +
                $"Email: {c.Email} | Điện thoại: {c.Phone} | Website: {c.Website ?? "-"}\n\n" +
                (ov != null
                    ? $"TỔNG QUAN ({ov.ScopeType}): Chi nhánh hoạt động: {ov.ActiveBranches}/{ov.TotalBranches} | Phòng ban: {ov.ActiveDepartments}/{ov.TotalDepartments} | Nhân viên Active: {ov.ActiveEmployees}/{ov.TotalEmployees} | Kho: {ov.ActiveWarehouses}/{ov.TotalWarehouses}"
                    : "");
        }, logger: _logger);
    }

    private async Task EditCompanyAsync()
    {
        var cur = await _orgApi.GetCompanyAsync();
        if (cur == null) return;
        var dlg = new ModalFormDialog("Chỉnh sửa thông tin công ty", [
            new ModalFieldSpec { Key = "LegalName", Label = "Tên pháp lý", Required = true, InitialValue = cur.LegalName },
            new ModalFieldSpec { Key = "TradingName", Label = "Tên thương mại", Required = true, InitialValue = cur.TradingName },
            new ModalFieldSpec { Key = "TaxNumber", Label = "Mã số thuế", Required = true, InitialValue = cur.TaxNumber },
            new ModalFieldSpec { Key = "Email", Label = "Email", Required = true, InitialValue = cur.Email },
            new ModalFieldSpec { Key = "Phone", Label = "Điện thoại", Required = true, InitialValue = cur.Phone },
            new ModalFieldSpec { Key = "Website", Label = "Website", InitialValue = cur.Website }
        ], async v => await _orgApi.UpdateCompanyAsync(new UpdateCompanyRequest(
            v["LegalName"]!.ToString()!, v["TradingName"]!.ToString()!, v["TaxNumber"]!.ToString()!,
            v["Email"]!.ToString()!, v["Phone"]!.ToString()!, v["Website"]?.ToString(), cur.LogoUrl)));
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadCompanyAsync();
    }

    private async Task LoadBranchesAsync()
    {
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            _branchesGrid.SetLoading(true);
            var res = await _orgApi.GetBranchesAsync(_branchesGrid.SearchText, null, null, new PagedRequest { Page = _branchesGrid.CurrentPage, PageSize = _branchesGrid.CurrentPageSize });
            _branchesGrid.BindPagedData(res);
        }, logger: _logger, showMessageBoxOnError: false);
        _branchesGrid.SetLoading(false);
    }

    private async Task CreateOrEditBranchAsync(BranchDto? existing)
    {
        var fields = new List<ModalFieldSpec>();
        if (existing == null)
            fields.Add(new ModalFieldSpec { Key = "Code", Label = "Mã chi nhánh", Required = true });
        fields.AddRange([
            new ModalFieldSpec { Key = "Name", Label = "Tên chi nhánh", Required = true, InitialValue = existing?.Name },
            new ModalFieldSpec { Key = "BranchType", Label = "Loại hình", Kind = ModalFieldKind.ComboBox, Required = true, InitialValue = existing?.BranchType ?? "Hub",
                Options = [new("Hub", "Hub"), new("Branch", "Branch"), new("HeadOffice", "HeadOffice"), new("SortingCenter", "SortingCenter")] },
            new ModalFieldSpec { Key = "Phone", Label = "Điện thoại", Required = true, InitialValue = existing?.Phone },
            new ModalFieldSpec { Key = "Email", Label = "Email", InitialValue = existing?.Email }
        ]);
        var dlg = new ModalFormDialog(existing == null ? "Thêm chi nhánh" : $"Sửa chi nhánh {existing.Code}", fields, async v =>
        {
            if (existing == null)
                await _orgApi.CreateBranchAsync(new CreateBranchRequest(null, v["Code"]!.ToString()!, v["Name"]!.ToString()!, v["BranchType"]!.ToString()!, v["Phone"]!.ToString()!, v["Email"]?.ToString()));
            else
                await _orgApi.UpdateBranchAsync(existing.Id.ToString(), new UpdateBranchRequest(v["Name"]!.ToString()!, v["BranchType"]!.ToString()!, v["Phone"]!.ToString()!, v["Email"]?.ToString()));
        });
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadBranchesAsync();
    }

    private async Task LoadDeptsAsync()
    {
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            _deptsGrid.SetLoading(true);
            var res = await _orgApi.GetDepartmentsAsync(_deptsGrid.SearchText, _branchContext.ActiveBranchId, null, new PagedRequest { Page = _deptsGrid.CurrentPage, PageSize = _deptsGrid.CurrentPageSize });
            _deptsGrid.BindPagedData(res);
        }, logger: _logger, showMessageBoxOnError: false);
        _deptsGrid.SetLoading(false);
    }

    private async Task CreateOrEditDeptAsync(DepartmentDto? existing)
    {
        var branches = _branchContext.AccessibleBranches.Select(b => new ModalComboOption(b.BranchId, $"{b.Code} - {b.Name}")).ToList();
        var fields = new List<ModalFieldSpec>();
        if (existing == null)
        {
            fields.Add(new ModalFieldSpec { Key = "BranchId", Label = "Chi nhánh", Kind = ModalFieldKind.ComboBox, Required = true, Options = branches, InitialValue = _branchContext.ActiveBranchId });
            fields.Add(new ModalFieldSpec { Key = "Code", Label = "Mã phòng ban", Required = true });
        }
        fields.Add(new ModalFieldSpec { Key = "Name", Label = "Tên phòng ban", Required = true, InitialValue = existing?.Name });
        fields.Add(new ModalFieldSpec { Key = "Description", Label = "Mô tả", Kind = ModalFieldKind.Multiline, InitialValue = existing?.Description });

        var dlg = new ModalFormDialog(existing == null ? "Thêm phòng ban" : $"Sửa phòng ban {existing.Code}", fields, async v =>
        {
            if (existing == null)
                await _orgApi.CreateDepartmentAsync(new CreateDepartmentRequest(Convert.ToInt64(v["BranchId"]), v["Code"]!.ToString()!, v["Name"]!.ToString()!, v["Description"]?.ToString()));
            else
                await _orgApi.UpdateDepartmentAsync(existing.Id.ToString(), new UpdateDepartmentRequest(v["Name"]!.ToString()!, v["Description"]?.ToString()));
        });
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadDeptsAsync();
    }

    private async Task LoadEmployeesAsync()
    {
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            _employeesGrid.SetLoading(true);
            var res = await _orgApi.GetEmployeesAsync(_employeesGrid.SearchText, _branchContext.ActiveBranchId, null, null, new PagedRequest { Page = _employeesGrid.CurrentPage, PageSize = _employeesGrid.CurrentPageSize });
            _employeesGrid.BindPagedData(res);
        }, logger: _logger, showMessageBoxOnError: false);
        _employeesGrid.SetLoading(false);
    }

    private async Task CreateOrEditEmployeeAsync(EmployeeDto? existing)
    {
        var branches = _branchContext.AccessibleBranches.Select(b => new ModalComboOption(b.BranchId, $"{b.Code} - {b.Name}")).ToList();
        var fields = new List<ModalFieldSpec>
        {
            new() { Key = "BranchId", Label = "Chi nhánh", Kind = ModalFieldKind.ComboBox, Required = true, Options = branches, InitialValue = existing?.BranchId ?? _branchContext.ActiveBranchId }
        };
        if (existing == null)
            fields.Add(new ModalFieldSpec { Key = "Code", Label = "Mã NV", Required = true });
        fields.AddRange([
            new ModalFieldSpec { Key = "FirstName", Label = "Tên", Required = true, InitialValue = existing?.FirstName },
            new ModalFieldSpec { Key = "LastName", Label = "Họ", Required = true, InitialValue = existing?.LastName },
            new ModalFieldSpec { Key = "Email", Label = "Email", Required = true, InitialValue = existing?.Email },
            new ModalFieldSpec { Key = "PhoneNumber", Label = "Điện thoại", Required = true, InitialValue = existing?.PhoneNumber },
            new ModalFieldSpec { Key = "JobTitle", Label = "Chức danh", Required = true, InitialValue = existing?.JobTitle ?? "Staff" },
            new ModalFieldSpec { Key = "HireDate", Label = "Ngày vào làm", Kind = ModalFieldKind.Date, Required = true, InitialValue = existing?.HireDate ?? DateOnly.FromDateTime(DateTime.Today) }
        ]);
        var dlg = new ModalFormDialog(existing == null ? "Thêm nhân viên" : $"Sửa {existing.Code}", fields, async v =>
        {
            var hire = DateOnly.FromDateTime((DateTime)v["HireDate"]!);
            if (existing == null)
                await _orgApi.CreateEmployeeAsync(new CreateEmployeeRequest(Convert.ToInt64(v["BranchId"]), null, v["Code"]!.ToString()!, v["FirstName"]!.ToString()!, v["LastName"]!.ToString()!, v["Email"]!.ToString()!, v["PhoneNumber"]!.ToString()!, null, null, v["JobTitle"]!.ToString()!, hire));
            else
                await _orgApi.UpdateEmployeeAsync(existing.Id.ToString(), new UpdateEmployeeRequest(Convert.ToInt64(v["BranchId"]), existing.DepartmentId, v["FirstName"]!.ToString()!, v["LastName"]!.ToString()!, v["Email"]!.ToString()!, v["PhoneNumber"]!.ToString()!, existing.DateOfBirth, existing.Gender, v["JobTitle"]!.ToString()!, hire));
        });
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadEmployeesAsync();
    }

    private async Task CreateEmployeeAccountAsync(EmployeeDto emp)
    {
        var roles = await _identityApi.GetRolesAsync();
        var dlg = new ModalFormDialog($"Cấp tài khoản cho {emp.FullName}", [
            new ModalFieldSpec { Key = "Username", Label = "Username", Required = true, InitialValue = emp.Code.ToLowerInvariant() },
            new ModalFieldSpec { Key = "Password", Label = "Mật khẩu", Kind = ModalFieldKind.Password, Required = true },
            new ModalFieldSpec { Key = "RoleCode", Label = "Vai trò chính", Kind = ModalFieldKind.ComboBox, Required = true,
                Options = roles.Select(r => new ModalComboOption(r.Code, $"{r.Code} - {r.Name}")).ToList() }
        ], async v =>
        {
            await _orgApi.CreateEmployeeAccountAsync(emp.Id.ToString(), new CreateEmployeeAccountRequest(
                v["Username"]!.ToString()!, v["Password"]!.ToString()!, [v["RoleCode"]!.ToString()!], [emp.BranchId], emp.BranchId));
        });
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadEmployeesAsync();
    }

    private async Task LoadUsersAsync()
    {
        if (!_auth.HasPermission(ClientPermissions.Identity.UsersRead)) return;
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            _usersGrid.SetLoading(true);
            var res = await _identityApi.GetUsersAsync(_usersGrid.SearchText, null, _branchContext.ActiveBranchId, null, new PagedRequest { Page = _usersGrid.CurrentPage, PageSize = _usersGrid.CurrentPageSize });
            _usersGrid.BindPagedData(res);
        }, logger: _logger, showMessageBoxOnError: false);
        _usersGrid.SetLoading(false);
    }

    private async Task LoadRolesAndPermissionsAsync()
    {
        if (!_auth.HasPermission(ClientPermissions.Identity.RolesRead)) return;
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            var roles = await _identityApi.GetRolesAsync();
            var perms = await _identityApi.GetPermissionsAsync();
            AppTheme.BindAntdTable(_rolesTable, roles);
            AppTheme.BindAntdTable(_permissionsTable, perms);
        }, logger: _logger, showMessageBoxOnError: false);
    }

    private async Task CreateUserAsync()
    {
        var roles = await _identityApi.GetRolesAsync();
        var branches = _branchContext.AccessibleBranches.Select(b => new ModalComboOption(b.BranchId, $"{b.Code} - {b.Name}")).ToList();
        var dlg = new ModalFormDialog("Tạo tài khoản mới", [
            new ModalFieldSpec { Key = "Username", Label = "Tên đăng nhập", Required = true },
            new ModalFieldSpec { Key = "Email", Label = "Email", Required = true },
            new ModalFieldSpec { Key = "Password", Label = "Mật khẩu", Kind = ModalFieldKind.Password, Required = true },
            new ModalFieldSpec { Key = "Role", Label = "Vai trò", Kind = ModalFieldKind.ComboBox, Required = true,
                Options = roles.Select(r => new ModalComboOption(r.Code, $"{r.Code} - {r.Name}")).ToList() },
            new ModalFieldSpec { Key = "BranchId", Label = "Chi nhánh mặc định", Kind = ModalFieldKind.ComboBox, Required = true, Options = branches, InitialValue = _branchContext.ActiveBranchId }
        ], async v =>
        {
            var bId = Convert.ToInt64(v["BranchId"]);
            await _identityApi.CreateUserAsync(new CreateUserRequest(v["Username"]!.ToString()!, v["Email"]!.ToString()!, v["Password"]!.ToString()!, null, [v["Role"]!.ToString()!], [bId], bId));
        });
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadUsersAsync();
    }

    private async Task ManageUserRolesAsync()
    {
        var u = _usersGrid.GetSelectedRow<UserAccountDto>();
        if (u == null) return;
        var roles = await _identityApi.GetRolesAsync();
        var dlg = new ModalFormDialog($"Gán vai trò: {u.Username}", [
            new ModalFieldSpec { Key = "Role", Label = "Vai trò chính", Kind = ModalFieldKind.ComboBox, Required = true, InitialValue = u.Roles.FirstOrDefault(),
                Options = roles.Select(r => new ModalComboOption(r.Code, $"{r.Code} - {r.Name}")).ToList() }
        ], async v => await _identityApi.UpdateUserRolesAsync(u.PublicId, new UpdateUserRolesRequest([v["Role"]!.ToString()!])));
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadUsersAsync();
    }

    private async Task ManageUserBranchesAsync()
    {
        var u = _usersGrid.GetSelectedRow<UserAccountDto>();
        if (u == null) return;
        var branches = _branchContext.AccessibleBranches.Select(b => new ModalComboOption(b.BranchId, $"{b.Code} - {b.Name}")).ToList();
        var dlg = new ModalFormDialog($"Gán chi nhánh: {u.Username}", [
            new ModalFieldSpec { Key = "BranchId", Label = "Chi nhánh chính", Kind = ModalFieldKind.ComboBox, Required = true, Options = branches, InitialValue = u.Branches.FirstOrDefault()?.BranchId }
        ], async v =>
        {
            var bId = Convert.ToInt64(v["BranchId"]);
            await _identityApi.UpdateUserBranchesAsync(u.PublicId, new UpdateUserBranchesRequest([bId], bId));
        });
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadUsersAsync();
    }

    private void ResetUserPassword()
    {
        var u = _usersGrid.GetSelectedRow<UserAccountDto>();
        if (u == null) return;
        var dlg = new ModalFormDialog($"Đặt lại mật khẩu: {u.Username}", [
            new ModalFieldSpec { Key = "NewPassword", Label = "Mật khẩu mới", Kind = ModalFieldKind.Password, Required = true }
        ], async v => await _identityApi.ResetUserPasswordAsync(u.PublicId, new AdminResetPasswordRequest(v["NewPassword"]!.ToString()!)));
        if (dlg.ShowDialog(this) == DialogResult.OK) UiAsyncRunner.ShowInfo(this, "Đã đặt lại mật khẩu người dùng.");
    }

    public void ApplyTheme()
    {
        BackColor = AppTheme.SurfaceBg;
        _companyCard.Back = AppTheme.CardBg;
        _companyCard.BorderColor = AppTheme.BorderSubtle;
        _companyTitleLabel.ForeColor = AppTheme.TextPrimary;
        _companyInfoLabel.ForeColor = AppTheme.TextSecondary;
    }
}
