using AntdUI;
using Logistics.Client.Application.Contracts.Carrier;
using Logistics.Client.Application.Interfaces;
using Logistics.Client.Application.Permissions;
using Logistics.Client.WinForms.Controls.Common;
using Logistics.Client.WinForms.Helpers;
using Logistics.Client.WinForms.Themes;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.Extensions.Logging;
using Label = System.Windows.Forms.Label;

namespace Logistics.Client.WinForms.Controls.Carrier;

public sealed class CarrierWorkspaceControl : ModuleViewBase, IThemeAware
{
    private readonly ICarrierApiClient _carrierApi;
    private readonly IAuthorizationService _auth;
    private readonly ILogger<CarrierWorkspaceControl> _logger;

    private readonly AntdUI.Tabs _tabs = AppTheme.CreateAntdTabs();
    private readonly AntdUI.TabPage _tabCarriers = new() { Text = "Hãng vận chuyển & Cấu hình" };
    private readonly AntdUI.TabPage _tabOps = new() { Text = "Tác vụ tích hợp & Tracking", Padding = new Padding(14) };
    private readonly AntdUI.TabPage _tabLogs = new() { Text = "API Logs / Webhooks / Sync Jobs" };

    private PagedDataGridControl _carriersGrid = null!;
    private AntdUI.Panel _subTabsCard = null!;
    private readonly AntdUI.Table _servicesGrid = AppTheme.CreateAntdTable();
    private readonly AntdUI.Table _configsGrid = AppTheme.CreateAntdTable();
    private readonly AntdUI.Table _mappingsGrid = AppTheme.CreateAntdTable();
    private AntdUI.Panel _opsCard = null!;
    private Label _opsResultLabel = null!;
    private PagedDataGridControl _logsGrid = null!;
    private AntdUI.Select _logTypeCombo = null!;

    public CarrierWorkspaceControl(ICarrierApiClient carrierApi, IAuthorizationService auth, ILogger<CarrierWorkspaceControl> logger)
    {
        _carrierApi = carrierApi;
        _auth = auth;
        _logger = logger;

        BuildCarriersTab();
        BuildOperationsTab();
        BuildLogsTab();

        if (_auth.HasAnyPermission(ClientPermissions.Carrier.CarriersRead, ClientPermissions.Carrier.CarriersManage))
        {
            _tabs.Pages.AddRange([_tabCarriers, _tabOps, _tabLogs]);
        }
        _tabs.SelectedIndexChanged += async (_, _) => await RefreshDataAsync();
        Controls.Add(_tabs);
        ApplyTheme();
    }

    public void SelectSection(string navKey)
    {
        var tab = navKey == "Carrier.Operations" ? _tabOps : _tabCarriers;
        if (_tabs.Pages.Contains(tab)) _tabs.SelectedTab = tab;
    }

    private void BuildCarriersTab()
    {
        var split = AppTheme.CreateSafeSplitContainer(Orientation.Horizontal, 280);
        _carriersGrid = new PagedDataGridControl("Hãng vận chuyển (3PL)", "Quản lý đối tác GHN/GHTK/VNPost. API Keys/Secrets được che ẩn hoàn toàn sau khi lưu.");
        if (_auth.HasPermission(ClientPermissions.Carrier.CarriersManage))
        {
            var addBtn = AppTheme.CreatePrimaryButton("Thêm hãng", 110);
            addBtn.Click += async (_, _) => await CreateCarrierAsync();
            var cfgBtn = AppTheme.CreateSecondaryButton("Cấu hình API Secret", 150);
            cfgBtn.Click += async (_, _) => await UpsertConfigAsync();
            var togBtn = AppTheme.CreateDangerButton("Bật/Tắt", 88);
            togBtn.Click += async (_, _) =>
            {
                var c = _carriersGrid.GetSelectedRow<CarrierDto>();
                if (c == null) return;
                await UiAsyncRunner.RunSafeAsync(this, async () =>
                {
                    await _carrierApi.ToggleCarrierStatusAsync(c.Code, !c.IsActive);
                    await LoadCarriersAsync();
                }, logger: _logger);
            };
            _carriersGrid.AddActionButton(togBtn);
            _carriersGrid.AddActionButton(cfgBtn);
            _carriersGrid.AddActionButton(addBtn);
        }
        _carriersGrid.ReloadRequested += async (_, _) => await LoadCarriersAsync();
        _carriersGrid.SelectionChanged += async (_, _) => await LoadCarrierDetailsAsync();
        split.Panel1.Controls.Add(_carriersGrid);

        var subTabs = AppTheme.CreateAntdTabs();
        subTabs.Pages.Add(new AntdUI.TabPage { Text = "Dịch vụ hãng", Controls = { _servicesGrid } });
        subTabs.Pages.Add(new AntdUI.TabPage { Text = "Cấu hình (Secrets masked)", Controls = { _configsGrid } });
        subTabs.Pages.Add(new AntdUI.TabPage { Text = "Mapping trạng thái", Controls = { _mappingsGrid } });

        _subTabsCard = AppTheme.CreateCardPanel(new Padding(10));
        _subTabsCard.Dock = DockStyle.Fill;
        _subTabsCard.Controls.Add(subTabs);

        split.Panel2.Padding = new Padding(0, 8, 0, 0);
        split.Panel2.Controls.Add(_subTabsCard);
        _tabCarriers.Controls.Add(split);
    }

    private void BuildOperationsTab()
    {
        _tabOps.AutoScroll = true;
        _opsCard = AppTheme.CreateCardPanel(new Padding(20));
        _opsCard.Dock = DockStyle.Top;
        _opsCard.Height = 220;

        _opsResultLabel = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(0, 12, 0, 0),
            Font = AppTheme.BodyFont,
            BackColor = Color.Transparent,
            Text = "Chọn nghiệp vụ bên dưới để gọi trực tiếp API tích hợp hãng vận chuyển."
        };
        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true, BackColor = Color.Transparent };
        var rateBtn = AppTheme.CreatePrimaryButton("Tra cứu giá cước 3PL", 165);
        rateBtn.Click += (_, _) =>
        {
            var dlg = new ModalFormDialog("Tra cứu giá cước hãng", [
                new ModalFieldSpec { Key = "CarrierId", Label = "Carrier ID", Kind = ModalFieldKind.Integer, Required = true, InitialValue = 1 },
                new ModalFieldSpec { Key = "WeightKg", Label = "Trọng lượng (kg)", Kind = ModalFieldKind.Decimal, Required = true, InitialValue = 2m },
                new ModalFieldSpec { Key = "CodAmount", Label = "COD", Kind = ModalFieldKind.Decimal, InitialValue = 0m }
            ], async v =>
            {
                var res = await _carrierApi.QueryRatesAsync(new CarrierRateQueryRequest { CarrierId = Convert.ToInt64(v["CarrierId"]), WeightKg = Convert.ToDecimal(v["WeightKg"]), CodAmount = Convert.ToDecimal(v["CodAmount"]) });
                _opsResultLabel.Text = res.Success ? string.Join("\n", res.Options.Select(o => $"{o.ServiceCode} ({o.ServiceName}): {o.TotalFee:N0} ({o.EstimatedDays} ngày)")) : $"Lỗi: {res.ErrorMessage}";
            });
            dlg.ShowDialog(this);
        };
        var trkBtn = AppTheme.CreateSecondaryButton("Tra cứu Tracking 3PL", 165);
        trkBtn.Click += (_, _) =>
        {
            var dlg = new ModalFormDialog("Tra cứu hành trình hãng", [
                new ModalFieldSpec { Key = "CarrierCode", Label = "Mã hãng (GHN/GHTK...)", Required = true },
                new ModalFieldSpec { Key = "TrackingNumber", Label = "Mã vận đơn hãng", Required = true }
            ], async v =>
            {
                var res = await _carrierApi.QueryTrackingAsync(v["CarrierCode"]!.ToString()!, v["TrackingNumber"]!.ToString()!);
                _opsResultLabel.Text = $"Hãng {res.CarrierCode} | VĐ {res.TrackingNumber} | Hiện tại: {res.CurrentStatus}\nSự kiện: {res.Events.Count}";
            });
            dlg.ShowDialog(this);
        };
        bar.Controls.AddRange([rateBtn, trkBtn]);
        _opsCard.Controls.Add(_opsResultLabel);
        _opsCard.Controls.Add(bar);
        AppTheme.BindAutoHeightCard(_opsCard, 220);
        _tabOps.Controls.Add(_opsCard);
    }

    private void BuildLogsTab()
    {
        _logsGrid = new PagedDataGridControl("Giám sát kết nối 3PL", "Xem API Logs, Webhook Logs và Sync Jobs từ Backend.");
        _logTypeCombo = new AntdUI.Select
        {
            Width = 175,
            Height = 32,
            Radius = 6,
            List = true,
            Font = AppTheme.BodyFont,
            Margin = new Padding(0, 0, 8, 4)
        };
        _logTypeCombo.Items.AddRange([
            new SelectItem("API Logs", "API Logs"),
            new SelectItem("Webhook Logs", "Webhook Logs"),
            new SelectItem("Sync Jobs", "Sync Jobs")
        ]);
        _logTypeCombo.SelectedIndex = 0;
        _logTypeCombo.SelectedIndexChanged += (_, _) => _logsGrid.ResetAndReload();
        _logsGrid.AddFilterControl(_logTypeCombo);
        _logsGrid.ReloadRequested += async (_, _) => await LoadLogsAsync();
        _tabLogs.Controls.Add(_logsGrid);
    }

    protected override async Task RefreshDataAsync()
    {
        if (_tabs.SelectedTab == _tabCarriers) await LoadCarriersAsync();
        else if (_tabs.SelectedTab == _tabLogs) await LoadLogsAsync();
    }

    private async Task LoadCarriersAsync()
    {
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            _carriersGrid.SetLoading(true);
            _carriersGrid.BindPagedData(await _carrierApi.GetCarriersAsync(_carriersGrid.SearchText, null, new PagedRequest { Page = _carriersGrid.CurrentPage, PageSize = _carriersGrid.CurrentPageSize }));
        }, logger: _logger, showMessageBoxOnError: false);
        _carriersGrid.SetLoading(false);
    }

    private async Task LoadCarrierDetailsAsync()
    {
        var c = _carriersGrid.GetSelectedRow<CarrierDto>();
        if (c == null) return;
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            var d = await _carrierApi.GetCarrierDetailAsync(c.Code);
            AppTheme.BindAntdTable(_servicesGrid, d.Services);
            AppTheme.BindAntdTable(_mappingsGrid, d.TrackingMappings);
            if (_auth.HasPermission(ClientPermissions.Carrier.CarriersManage))
                AppTheme.BindAntdTable(_configsGrid, (await _carrierApi.GetConfigurationsAsync(c.Code)).ToList());
        }, logger: _logger, showMessageBoxOnError: false);
    }

    private async Task CreateCarrierAsync()
    {
        var dlg = new ModalFormDialog("Thêm hãng vận chuyển", [
            new ModalFieldSpec { Key = "Code", Label = "Mã hãng", Required = true },
            new ModalFieldSpec { Key = "Name", Label = "Tên hãng", Required = true },
            new ModalFieldSpec { Key = "ApiBaseUrl", Label = "API Base URL", Required = true, InitialValue = "https://api.carrier.local" }
        ], async v => await _carrierApi.CreateCarrierAsync(new CreateCarrierRequest { Code = v["Code"]!.ToString()!, Name = v["Name"]!.ToString()!, ApiBaseUrl = v["ApiBaseUrl"]!.ToString()! }));
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadCarriersAsync();
    }

    private async Task UpsertConfigAsync()
    {
        var c = _carriersGrid.GetSelectedRow<CarrierDto>();
        if (c == null) return;
        var dlg = new ModalFormDialog($"Cấu hình bảo mật {c.Code}", [
            new ModalFieldSpec { Key = "Environment", Label = "Môi trường", Kind = ModalFieldKind.ComboBox, Required = true, Options = [new("Sandbox","Sandbox"), new("Production","Production")] },
            new ModalFieldSpec { Key = "ApiKey", Label = "API Key (bỏ trống nếu giữ nguyên)", Kind = ModalFieldKind.Password },
            new ModalFieldSpec { Key = "ApiSecret", Label = "API Secret", Kind = ModalFieldKind.Password },
            new ModalFieldSpec { Key = "WebhookSecret", Label = "Webhook Secret", Kind = ModalFieldKind.Password }
        ], async v => await _carrierApi.UpsertConfigurationAsync(c.Code, new UpsertCarrierConfigurationRequest
        {
            Environment = v["Environment"]!.ToString()!, ApiKey = v["ApiKey"]?.ToString(), ApiSecret = v["ApiSecret"]?.ToString(), WebhookSecret = v["WebhookSecret"]?.ToString()
        }));
        if (dlg.ShowDialog(this) == DialogResult.OK) await LoadCarrierDetailsAsync();
    }

    private async Task LoadLogsAsync()
    {
        await UiAsyncRunner.RunSafeAsync(this, async () =>
        {
            _logsGrid.SetLoading(true);
            var mode = _logTypeCombo.SelectedValue?.ToString();
            if (mode == "Webhook Logs")
                _logsGrid.BindPagedData(await _carrierApi.GetWebhookLogsAsync(null, null, _logsGrid.CurrentPage, _logsGrid.CurrentPageSize));
            else if (mode == "Sync Jobs")
                _logsGrid.BindPagedData(await _carrierApi.GetSyncJobsAsync(null, null, _logsGrid.CurrentPage, _logsGrid.CurrentPageSize));
            else
                _logsGrid.BindPagedData(await _carrierApi.GetApiLogsAsync(null, null, _logsGrid.CurrentPage, _logsGrid.CurrentPageSize));
        }, logger: _logger, showMessageBoxOnError: false);
        _logsGrid.SetLoading(false);
    }

    public void ApplyTheme()
    {
        BackColor = AppTheme.SurfaceBg;
        _subTabsCard.Back = AppTheme.CardBg;
        _subTabsCard.BorderColor = AppTheme.BorderSubtle;
        _opsCard.Back = AppTheme.CardBg;
        _opsCard.BorderColor = AppTheme.BorderSubtle;
        _opsResultLabel.ForeColor = AppTheme.TextPrimary;
    }
}
