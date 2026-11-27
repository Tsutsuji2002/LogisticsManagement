using AntdUI;
using Logistics.Client.Application.Contracts.Catalog;
using Logistics.Client.Application.Interfaces;
using Logistics.Client.Application.Permissions;
using Logistics.Client.WinForms.Controls.Common;
using Logistics.Client.WinForms.Helpers;
using Logistics.Client.WinForms.Themes;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.Extensions.Logging;

namespace Logistics.Client.WinForms.Controls.Catalog;

/// <summary>
/// AntdUI Catalog workspace managing Products (SKU), Categories, Units of Measure, and Read-Only Services.
/// </summary>
public sealed class CatalogWorkspaceControl : ModuleViewBase, IThemeAware
{
    private readonly ICatalogApiClient _catalogApi;
    private readonly IAuthorizationService _auth;
    private readonly ILogger<CatalogWorkspaceControl> _logger;

    private readonly AntdUI.Tabs _tabs = AppTheme.CreateAntdTabs();
    private readonly AntdUI.TabPage _tabProducts = new() { Text = "Sản phẩm (SKU)" };
    private readonly AntdUI.TabPage _tabCategories = new() { Text = "Nhóm sản phẩm" };
    private readonly AntdUI.TabPage _tabUnits = new() { Text = "Đơn vị tính" };
    private readonly AntdUI.TabPage _tabServices = new() { Text = "Dịch vụ (Chỉ xem)" };

    private PagedDataGridControl _productsGrid = null!;
    private PagedDataGridControl _categoriesGrid = null!;
    private PagedDataGridControl _unitsGrid = null!;
    private PagedDataGridControl _servicesGrid = null!;

    public CatalogWorkspaceControl(ICatalogApiClient catalogApi, IAuthorizationService auth, ILogger<CatalogWorkspaceControl> logger)
    {
        _catalogApi = catalogApi;
        _auth = auth;
        _logger = logger;

        BuildProductsTab();
        BuildCategoriesTab();
        BuildUnitsTab();
        BuildServicesTab();

        if (_auth.HasPermission(ClientPermissions.Catalog.ProductsRead)) _tabs.Pages.Add(_tabProducts);
        if (_auth.HasPermission(ClientPermissions.Catalog.CategoriesRead)) _tabs.Pages.Add(_tabCategories);
        if (_auth.HasPermission(ClientPermissions.Catalog.UnitsRead)) _tabs.Pages.Add(_tabUnits);
        if (_auth.HasPermission(ClientPermissions.Catalog.ServicesRead)) _tabs.Pages.Add(_tabServices);

        _tabs.SelectedIndexChanged += async (_, _) => await RefreshDataAsync();
        Controls.Add(_tabs);
        ApplyTheme();
    }

    public void SelectSection(string navKey)
    {
        var tab = navKey switch
        {
            "Catalog.Products" => _tabProducts,
            "Catalog.Categories" => _tabCategories,
            "Catalog.Units" => _tabUnits,
            "Catalog.Services" => _tabServices,
            _ => _tabProducts
        };
        if (_tabs.Pages.Contains(tab))
            _tabs.SelectedTab = tab;
    }

    private void BuildProductsTab()
    {
        _productsGrid = new PagedDataGridControl("Sản phẩm (SKU)", "Quản lý danh mục sản phẩm, trọng lượng, kích thước và giá trị khai báo.");
        if (_auth.HasPermission(ClientPermissions.Catalog.ProductsCreate))
        {
            var addBtn = AppTheme.CreatePrimaryButton("Thêm SKU", 110);
            addBtn.Click += async (_, _) => await CreateOrEditProductAsync(null);
            _productsGrid.AddActionButton(addBtn);
        }
        if (_auth.HasPermission(ClientPermissions.Catalog.ProductsUpdate))
        {
            var editBtn = AppTheme.CreateSecondaryButton("Sửa", 80);
            editBtn.Click += async (_, _) =>
            {
                var p = _productsGrid.GetSelectedRow<ProductDto>();
                if (p != null) await CreateOrEditProductAsync(p);
            };
            _productsGrid.AddActionButton(editBtn);
        }
        if (_auth.HasPermission(ClientPermissions.Catalog.ProductsManageStatus))
        {
            var togBtn = AppTheme.CreateDangerButton("Bật/Tắt TT", 105);
            togBtn.Click += async (_, _) =>
            {
                var p = _productsGrid.GetSelectedRow<ProductDto>();
                if (p == null) return;
                await UiAsyncRunner.RunSafeAsync(this, async () =>
                {
                    await _catalogApi.ToggleProductStatusAsync(p.Id.ToString(), new UpdateProductStatusRequest { IsActive = !p.IsActive });
                    await LoadProductsAsync();
                }, logger: _logger);
            };
            _productsGrid.AddActionButton(togBtn);
        }
        _productsGrid.ReloadRequested += async (_, _) => await LoadProductsAsync();
        _tabProducts.Controls.Add(_productsGrid);
    }

    private void BuildCategoriesTab()
    {
        _categoriesGrid = new PagedDataGridControl("Nhóm sản phẩm", "Quản lý cây danh mục sản phẩm.");
        if (_auth.HasPermission(ClientPermissions.Catalog.CategoriesCreate))
        {
            var addBtn = AppTheme.CreatePrimaryButton("Thêm nhóm", 110);
            addBtn.Click += async (_, _) => await CreateOrEditCategoryAsync(null);
            _categoriesGrid.AddActionButton(addBtn);
        }
        if (_auth.HasPermission(ClientPermissions.Catalog.CategoriesUpdate))
        {
            var editBtn = AppTheme.CreateSecondaryButton("Sửa", 80);
            editBtn.Click += async (_, _) =>
            {
                var c = _categoriesGrid.GetSelectedRow<CategoryDto>();
                if (c != null) await CreateOrEditCategoryAsync(c);
            };
            _categoriesGrid.AddActionButton(editBtn);
        }
        if (_auth.HasPermission(ClientPermissions.Catalog.CategoriesManageStatus))
        {
            var togBtn = AppTheme.CreateDangerButton("Bật/Tắt", 90);
            togBtn.Click += async (_, _) =>
            {
                var c = _categoriesGrid.GetSelectedRow<CategoryDto>();
                if (c == null) return;
                await UiAsyncRunner.RunSafeAsync(this, async () =>
                {
                    await _catalogApi.ToggleCategoryStatusAsync(c.Id.ToString(), new UpdateCategoryStatusRequest { IsActive = !c.IsActive });
                    await LoadCategoriesAsync();
                }, logger: _logger);
            };
            _categoriesGrid.AddActionButton(togBtn);
        }
        _categoriesGrid.ReloadRequested += async (_, _) => await LoadCategoriesAsync();
        _tabCategories.Controls.Add(_categoriesGrid);
    }

    private void BuildUnitsTab()
    {
        _unitsGrid = new PagedDataGridControl("Đơn vị tính", "Quản lý đơn vị đo lường trọng lượng, kích thước, số lượng.");
        if (_auth.HasPermission(ClientPermissions.Catalog.UnitsManage))
        {
            var addBtn = AppTheme.CreatePrimaryButton("Thêm ĐVT", 105);
            addBtn.Click += async (_, _) =>
            {
                var dlg = new ModalFormDialog("Thêm đơn vị tính", [
                    new ModalFieldSpec { Key = "Code", Label = "Mã ĐVT", Required = true },
                    new ModalFieldSpec { Key = "Name", Label = "Tên ĐVT", Required = true },
                    new ModalFieldSpec { Key = "Type", Label = "Loại", Kind = ModalFieldKind.ComboBox, Required = true,
                        Options = [new("Count","Count"), new("Weight","Weight"), new("Volume","Volume"), new("Dimension","Dimension")] },
                    new ModalFieldSpec { Key = "ConversionFactor", Label = "Hệ số quy đổi", Kind = ModalFieldKind.Decimal, InitialValue = 1m },
                    new ModalFieldSpec { Key = "IsBaseUnit", Label = "Đơn vị chuẩn", Kind = ModalFieldKind.CheckBox }
                ], async v => await _catalogApi.CreateUnitAsync(new CreateUnitOfMeasureRequest
                {
                    Code = v["Code"]!.ToString()!, Name = v["Name"]!.ToString()!, Type = v["Type"]!.ToString()!,
                    ConversionFactor = Convert.ToDecimal(v["ConversionFactor"]), IsBaseUnit = v["IsBaseUnit"] is true
                }));
                if (dlg.ShowDialog(this) == DialogResult.OK) await LoadUnitsAsync();
            };
            _unitsGrid.AddActionButton(addBtn);
        }
        _unitsGrid.ReloadRequested += async (_, _) => await LoadUnitsAsync();
        _tabUnits.Controls.Add(_unitsGrid);
    }

    private void BuildServicesTab()
    {
        _servicesGrid = new PagedDataGridControl(
            "Dịch vụ vận chuyển (Read-only)",
            "Backend /api/v1/catalog/services chỉ hỗ trợ tra cứu danh mục dịch vụ (không có API tạo/sửa).",
            showSearch: false, showPagination: false);
        _servicesGrid.ReloadRequested += async (_, _) => await LoadServicesAsync();
        _tabServices.Controls.Add(_servicesGrid);
    }

    protected override async Task RefreshDataAsync()
    {
        if (_tabs.SelectedTab == _tabProducts) await LoadProductsAsync();
        else if (_tabs.SelectedTab == _tabCategories) await LoadCategoriesAsync();
        else if (_tabs.SelectedTab == _tabUnits) await LoadUnitsAsync();
        else if (_tabs.SelectedTab == _tabServices) await LoadServicesAsync();
    }

    private async Task LoadProductsAsync()
    {
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            _productsGrid.SetLoading(true);
            var res = await _catalogApi.GetProductsAsync(_productsGrid.SearchText, null, null, new PagedRequest { Page = _productsGrid.CurrentPage, PageSize = _productsGrid.CurrentPageSize });
            _productsGrid.BindPagedData(res);
        }, logger: _logger, showMessageBoxOnError: false);
        _productsGrid.SetLoading(false);
    }

    private async Task CreateOrEditProductAsync(ProductDto? existing)
    {
        var cats = await _catalogApi.GetCategoriesAsync(null, true, new PagedRequest { Page = 1, PageSize = 100 });
        var units = await _catalogApi.GetUnitsAsync(null, null, true, new PagedRequest { Page = 1, PageSize = 100 });
        var fields = new List<ModalFieldSpec>
        {
            new() { Key = "CategoryId", Label = "Nhóm hàng", Kind = ModalFieldKind.ComboBox, Required = true, InitialValue = existing?.CategoryId,
                Options = cats.Items.Select(c => new ModalComboOption(c.Id, $"{c.Code} - {c.Name}")).ToList() },
            new() { Key = "UnitOfMeasureId", Label = "Đơn vị tính", Kind = ModalFieldKind.ComboBox, Required = true, InitialValue = existing?.UnitOfMeasureId,
                Options = units.Items.Select(u => new ModalComboOption(u.Id, $"{u.Code} - {u.Name}")).ToList() }
        };
        if (existing == null)
            fields.Add(new ModalFieldSpec { Key = "Sku", Label = "Mã SKU", Required = true });
        fields.AddRange([
            new ModalFieldSpec { Key = "Name", Label = "Tên sản phẩm", Required = true, InitialValue = existing?.Name },
            new ModalFieldSpec { Key = "WeightKg", Label = "Trọng lượng (kg)", Kind = ModalFieldKind.Decimal, InitialValue = existing?.WeightKg ?? 1m },
            new ModalFieldSpec { Key = "DeclaredValue", Label = "Giá trị khai báo", Kind = ModalFieldKind.Decimal, InitialValue = existing?.DeclaredValue ?? 0m },
            new ModalFieldSpec { Key = "IsPerishable", Label = "Dễ hư hỏng", Kind = ModalFieldKind.CheckBox, InitialValue = existing?.IsPerishable ?? false },
            new ModalFieldSpec { Key = "IsDangerous", Label = "Hàng nguy hiểm", Kind = ModalFieldKind.CheckBox, InitialValue = existing?.IsDangerous ?? false }
        ]);
        var dlg = new ModalFormDialog(existing == null ? "Thêm sản phẩm" : $"Sửa {existing.Sku}", fields, async v =>
        {
            if (existing == null)
                await _catalogApi.CreateProductAsync(new CreateProductRequest
                {
                    CategoryId = Convert.ToInt64(v["CategoryId"]), UnitOfMeasureId = Convert.ToInt64(v["UnitOfMeasureId"]),
                    Sku = v["Sku"]!.ToString()!, Name = v["Name"]!.ToString()!, WeightKg = Convert.ToDecimal(v["WeightKg"]),
                    DeclaredValue = Convert.ToDecimal(v["DeclaredValue"]), IsPerishable = v["IsPerishable"] is true, IsDangerous = v["IsDangerous"] is true
                });
            else
                await _catalogApi.UpdateProductAsync(existing.Id.ToString(), new UpdateProductRequest
                {
                    CategoryId = Convert.ToInt64(v["CategoryId"]), UnitOfMeasureId = Convert.ToInt64(v["UnitOfMeasureId"]),
                    Name = v["Name"]!.ToString()!, WeightKg = Convert.ToDecimal(v["WeightKg"]),
                    DeclaredValue = Convert.ToDecimal(v["DeclaredValue"]), IsPerishable = v["IsPerishable"] is true, IsDangerous = v["IsDangerous"] is true
                });
        });
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadProductsAsync();
    }

    private async Task LoadCategoriesAsync()
    {
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            _categoriesGrid.SetLoading(true);
            _categoriesGrid.BindPagedData(await _catalogApi.GetCategoriesAsync(_categoriesGrid.SearchText, null, new PagedRequest { Page = _categoriesGrid.CurrentPage, PageSize = _categoriesGrid.CurrentPageSize }));
        }, logger: _logger, showMessageBoxOnError: false);
        _categoriesGrid.SetLoading(false);
    }

    private async Task CreateOrEditCategoryAsync(CategoryDto? existing)
    {
        var fields = new List<ModalFieldSpec>();
        if (existing == null) fields.Add(new ModalFieldSpec { Key = "Code", Label = "Mã nhóm", Required = true });
        fields.Add(new ModalFieldSpec { Key = "Name", Label = "Tên nhóm", Required = true, InitialValue = existing?.Name });
        fields.Add(new ModalFieldSpec { Key = "Description", Label = "Mô tả", Kind = ModalFieldKind.Multiline, InitialValue = existing?.Description });
        var dlg = new ModalFormDialog(existing == null ? "Thêm nhóm SP" : $"Sửa {existing.Code}", fields, async v =>
        {
            if (existing == null) await _catalogApi.CreateCategoryAsync(new CreateCategoryRequest { Code = v["Code"]!.ToString()!, Name = v["Name"]!.ToString()!, Description = v["Description"]?.ToString() });
            else await _catalogApi.UpdateCategoryAsync(existing.Id.ToString(), new UpdateCategoryRequest { Name = v["Name"]!.ToString()!, Description = v["Description"]?.ToString() });
        });
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadCategoriesAsync();
    }

    private async Task LoadUnitsAsync()
    {
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            _unitsGrid.SetLoading(true);
            _unitsGrid.BindPagedData(await _catalogApi.GetUnitsAsync(_unitsGrid.SearchText, null, null, new PagedRequest { Page = _unitsGrid.CurrentPage, PageSize = _unitsGrid.CurrentPageSize }));
        }, logger: _logger, showMessageBoxOnError: false);
        _unitsGrid.SetLoading(false);
    }

    private async Task LoadServicesAsync()
    {
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            _servicesGrid.SetLoading(true);
            _servicesGrid.BindListData(await _catalogApi.GetServicesAsync());
        }, logger: _logger, showMessageBoxOnError: false);
        _servicesGrid.SetLoading(false);
    }

    public void ApplyTheme()
    {
        BackColor = AppTheme.SurfaceBg;
    }
}
