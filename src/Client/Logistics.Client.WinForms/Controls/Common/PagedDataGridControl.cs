using System.Collections;
using AntdUI;
using Logistics.Client.Application.Localization;
using Logistics.Client.WinForms.Themes;
using Logistics.Shared.Contracts.Pagination;
using Label = System.Windows.Forms.Label;
using Timer = System.Windows.Forms.Timer;

namespace Logistics.Client.WinForms.Controls.Common;

/// <summary>
/// Enterprise AntdUI Table container with header toolbar, debounced AntdUI.Input search,
/// custom filter slot, AntdUI.Alert state banner, auto-sized AntdUI.Table, and AntdUI.Pagination.
/// </summary>
public sealed class PagedDataGridControl : AntdUI.Panel, IThemeAware
{
    private readonly Label _titleLabel;
    private readonly Label _subtitleLabel;
    private readonly AntdUI.Input _searchInput;
    private readonly AntdUI.Button _refreshButton;
    private readonly FlowLayoutPanel _actionsPanel;
    private readonly FlowLayoutPanel _filterPanel;
    private readonly AntdUI.Table _table;
    private readonly AntdUI.Alert _bannerAlert;
    private readonly AntdUI.Pagination _pagination;
    private readonly Label _pageSummaryLabel;
    private readonly System.Windows.Forms.Panel _toolbarSeparator;
    private readonly System.Windows.Forms.Panel _footerPanel;
    private readonly Timer _searchDebounce;

    private IList _currentItems = Array.Empty<object>();
    private Type _currentItemType = typeof(object);
    private int _currentPage = 1;
    private int _pageSize = 20;
    private int _totalCount;
    private bool _suppressPageEvents;

    public PagedDataGridControl(string title, string? subtitle = null, bool showSearch = true, bool showPagination = true)
    {
        Dock = DockStyle.Fill;
        Radius = 8;
        BorderWidth = 1F;
        Padding = new Padding(0);
        Margin = new Padding(0);

        // Top Header + Primary Actions Bar
        var headerPanel = new System.Windows.Forms.Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            Padding = new Padding(16, 10, 16, 8),
            BackColor = Color.Transparent
        };

        var titleStack = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent
        };

        _titleLabel = new Label
        {
            Text = title,
            Font = AppTheme.SectionTitleFont,
            AutoSize = true,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };
        _subtitleLabel = new Label
        {
            Text = subtitle ?? string.Empty,
            Font = AppTheme.SmallFont,
            AutoSize = true,
            Visible = !string.IsNullOrWhiteSpace(subtitle),
            BackColor = Color.Transparent,
            Margin = new Padding(0, 2, 0, 0)
        };
        titleStack.Controls.Add(_titleLabel);
        titleStack.Controls.Add(_subtitleLabel);

        _actionsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 4, 0, 0),
            BackColor = Color.Transparent
        };

        _refreshButton = AppTheme.CreateSecondaryButton(AppStrings.T("Refresh"), 102);
        _refreshButton.Click += (_, _) => RaiseReload();
        _actionsPanel.Controls.Add(_refreshButton);

        headerPanel.Controls.Add(titleStack);
        headerPanel.Controls.Add(_actionsPanel);

        // Filter & Search Toolbar
        _filterPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            MinimumSize = new Size(0, 46),
            Padding = new Padding(16, 6, 16, 8),
            WrapContents = true,
            Visible = showSearch,
            BackColor = Color.Transparent
        };

        _searchInput = new AntdUI.Input
        {
            Width = 280,
            Height = 32,
            Radius = 6,
            AllowClear = true,
            PlaceholderText = "Tìm kiếm theo từ khóa...",
            Font = AppTheme.BodyFont,
            Margin = new Padding(0, 0, 8, 4)
        };
        _searchDebounce = new Timer { Interval = 320 };
        _searchDebounce.Tick += (_, _) =>
        {
            _searchDebounce.Stop();
            _currentPage = 1;
            RaiseReload();
        };
        _searchInput.TextChanged += (_, _) =>
        {
            _searchDebounce.Stop();
            _searchDebounce.Start();
        };
        _searchInput.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                _searchDebounce.Stop();
                _currentPage = 1;
                RaiseReload();
                e.Handled = true;
            }
        };

        var searchBtn = AppTheme.CreatePrimaryButton(AppStrings.T("Search"), 96);
        searchBtn.Height = 32;
        searchBtn.MinimumSize = new Size(92, 32);
        searchBtn.Click += (_, _) =>
        {
            _searchDebounce.Stop();
            _currentPage = 1;
            RaiseReload();
        };

        _filterPanel.Controls.Add(_searchInput);
        _filterPanel.Controls.Add(searchBtn);

        _toolbarSeparator = new System.Windows.Forms.Panel
        {
            Dock = DockStyle.Top,
            Height = 1
        };

        // Alert state banner (loading / empty / error)
        _bannerAlert = new AntdUI.Alert
        {
            Dock = DockStyle.Top,
            Height = 38,
            Visible = false,
            Radius = 0,
            BorderWidth = 0F,
            Icon = TType.Info,
            Font = AppTheme.BodyFont,
            Text = string.Empty
        };

        // Main AntdUI Table
        _table = AppTheme.CreateAntdTable();
        _table.SizeChanged += (_, _) =>
        {
            if (_currentItems.Count > 0 || _table.Columns.Count > 0)
                AppTheme.BindAntdTable(_table, _currentItemType, _currentItems);
        };
        _table.SelectIndexChanged += (_, e) => SelectionChanged?.Invoke(this, e);
        _table.CellClick += (_, e) => SelectionChanged?.Invoke(this, e);
        _table.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex > 0)
                RowDoubleClicked?.Invoke(this, e);
        };

        // Pagination Footer
        _footerPanel = new System.Windows.Forms.Panel
        {
            Dock = DockStyle.Bottom,
            Height = 46,
            Visible = showPagination,
            Padding = new Padding(16, 6, 16, 6)
        };

        _pageSummaryLabel = new Label
        {
            Dock = DockStyle.Left,
            AutoSize = false,
            Width = 260,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = AppTheme.SmallFont,
            Text = "0 bản ghi"
        };

        _pagination = new AntdUI.Pagination
        {
            Dock = DockStyle.Right,
            Width = 480,
            Current = 1,
            PageSize = 20,
            Total = 0,
            ShowSizeChanger = true,
            PageSizeOptions = [10, 20, 50, 100],
            SizeChangerWidth = 84,
            Radius = 6,
            Font = AppTheme.SmallFont,
            RecordsPerPageText = "/ trang"
        };
        _pagination.ValueChanged += (_, e) =>
        {
            if (_suppressPageEvents) return;
            _currentPage = Math.Max(1, e.Current);
            _pageSize = Math.Max(10, e.PageSize);
            RaiseReload();
        };

        _footerPanel.Controls.Add(_pagination);
        _footerPanel.Controls.Add(_pageSummaryLabel);

        // Docking order
        Controls.Add(_table);
        Controls.Add(_bannerAlert);
        Controls.Add(_toolbarSeparator);
        Controls.Add(_filterPanel);
        Controls.Add(headerPanel);
        Controls.Add(_footerPanel);

        ApplyTheme();
    }

    public AntdUI.Table Table => _table;
    public int RowCount => _currentItems.Count;
    public string SearchText => _searchInput.Text.Trim();
    public int CurrentPage => _currentPage;
    public int CurrentPageSize => _pageSize;

    public event EventHandler? ReloadRequested;
    public event EventHandler? SelectionChanged;
    public event EventHandler? RowDoubleClicked;

    public void AddActionButton(Control button)
    {
        _actionsPanel.Controls.Add(button);
    }

    public void AddFilterControl(Control filterControl)
    {
        _filterPanel.Controls.Add(filterControl);
    }

    public void SetLoading(bool loading, string? message = null)
    {
        _refreshButton.Loading = loading;
        _refreshButton.Enabled = !loading;

        if (loading)
        {
            _bannerAlert.Text = message ?? "Đang tải dữ liệu từ API...";
            _bannerAlert.Icon = TType.Info;
            _bannerAlert.Visible = true;
        }
        else if (_bannerAlert.Icon == TType.Info && _currentItems.Count > 0)
        {
            _bannerAlert.Visible = false;
        }
    }

    public void SetError(string message)
    {
        _bannerAlert.Text = message;
        _bannerAlert.Icon = TType.Error;
        _bannerAlert.Visible = true;
    }

    public void BindPagedData<T>(PagedResponse<T> result)
    {
        _currentItemType = typeof(T);
        _currentItems = result.Items.ToList();
        _currentPage = Math.Max(1, result.Page);
        _pageSize = Math.Max(10, result.PageSize);
        _totalCount = Math.Max(0, result.TotalCount);

        AppTheme.BindAntdTable(_table, result.Items);

        if (result.Items.Count == 0)
        {
            _bannerAlert.Text = "Không có dữ liệu phù hợp với bộ lọc hiện tại.";
            _bannerAlert.Icon = TType.Warn;
            _bannerAlert.Visible = true;
        }
        else
        {
            _bannerAlert.Visible = false;
        }

        UpdatePaginationState();
    }

    public void BindListData<T>(IReadOnlyList<T> items) => BindList(items);

    public void BindList<T>(IReadOnlyList<T> items)
    {
        _currentItemType = typeof(T);
        _currentItems = items.ToList();
        _currentPage = 1;
        _totalCount = items.Count;

        AppTheme.BindAntdTable(_table, items);
        _bannerAlert.Visible = items.Count == 0;
        if (items.Count == 0)
        {
            _bannerAlert.Text = "Không có dữ liệu.";
            _bannerAlert.Icon = TType.Warn;
        }

        UpdatePaginationState();
    }

    public T? GetSelectedRow<T>() where T : class
        => AppTheme.GetSelectedAntdRow<T>(_table);

    public void SelectFirstRow()
    {
        if (_currentItems.Count > 0)
            _table.SelectedIndex = 1;
    }

    public void ResetPageToFirst() => _currentPage = 1;

    public void ResetAndReload()
    {
        _currentPage = 1;
        RaiseReload();
    }

    private void UpdatePaginationState()
    {
        var totalPages = Math.Max(1, (int)Math.Ceiling(_totalCount / (double)_pageSize));
        _currentPage = Math.Clamp(_currentPage, 1, totalPages);

        _suppressPageEvents = true;
        try
        {
            _pagination.Total = _totalCount;
            _pagination.PageSize = _pageSize;
            _pagination.Current = _currentPage;
        }
        finally
        {
            _suppressPageEvents = false;
        }

        _pageSummaryLabel.Text = $"Tổng: {_totalCount:N0} bản ghi • Trang {_currentPage}/{totalPages}";
    }

    private void RaiseReload() => ReloadRequested?.Invoke(this, EventArgs.Empty);

    public void ApplyTheme()
    {
        Back = AppTheme.CardBg;
        BorderColor = AppTheme.BorderSubtle;
        ForeColor = AppTheme.TextPrimary;

        _titleLabel.ForeColor = AppTheme.TextPrimary;
        _subtitleLabel.ForeColor = AppTheme.TextSecondary;
        _toolbarSeparator.BackColor = AppTheme.BorderSubtle;
        _footerPanel.BackColor = AppTheme.RowAltBg;
        _pageSummaryLabel.ForeColor = AppTheme.TextSecondary;

        _table.Back = AppTheme.CardBg;
        _table.ForeColor = AppTheme.TextPrimary;
        _table.BorderColor = AppTheme.BorderSubtle;
        _table.ColumnBack = AppTheme.RowAltBg;
        _table.ColumnFore = AppTheme.TextSecondary;
        _table.Invalidate();
        Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _searchDebounce.Dispose();
        base.Dispose(disposing);
    }
}
