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
    private readonly TableLayoutPanel _footerGrid;
    private readonly Timer _searchDebounce;

    private IList _currentItems = Array.Empty<object>();
    private Type _currentItemType = typeof(object);
    private int _currentPage = 1;
    private int _pageSize = 20;
    private int _totalCount;
    private bool _suppressPageEvents;
    private bool? _lastCompactFooter;

    public PagedDataGridControl(string title, string? subtitle = null, bool showSearch = true, bool showPagination = true)
    {
        Dock = DockStyle.Fill;
        Radius = 8;
        BorderWidth = 1F;
        Padding = new Padding(0);
        Margin = new Padding(0);

        // Top Header + Primary Actions Bar (2-column responsive TableLayout so buttons never overlap title/subtitle)
        var headerPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(16, 10, 16, 6),
            Margin = new Padding(0),
            BackColor = Color.Transparent
        };
        headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        headerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var titleStack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0, 0, 12, 0),
            BackColor = Color.Transparent
        };
        titleStack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        titleStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        titleStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _titleLabel = new Label
        {
            Text = title,
            Font = AppTheme.SectionTitleFont,
            Dock = DockStyle.Top,
            Height = 24,
            AutoEllipsis = true,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };
        _subtitleLabel = new Label
        {
            Text = subtitle ?? string.Empty,
            Font = AppTheme.SmallFont,
            Dock = DockStyle.Top,
            Height = 18,
            AutoEllipsis = true,
            Visible = !string.IsNullOrWhiteSpace(subtitle),
            BackColor = Color.Transparent,
            Margin = new Padding(0, 2, 0, 0)
        };
        titleStack.Controls.Add(_titleLabel, 0, 0);
        titleStack.Controls.Add(_subtitleLabel, 0, 1);

        _actionsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            MaximumSize = new Size(760, 0),
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = true,
            Padding = new Padding(0, 2, 0, 0),
            BackColor = Color.Transparent
        };

        _refreshButton = AppTheme.CreateSecondaryButton(AppStrings.T("Refresh"), 102);
        _refreshButton.Click += (_, _) => RaiseReload();
        _actionsPanel.Controls.Add(_refreshButton);

        headerPanel.Controls.Add(titleStack, 0, 0);
        headerPanel.Controls.Add(_actionsPanel, 1, 0);

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
            AppTheme.UpdateAntdTableColumnWidths(_table);
            ApplyResponsiveGridLayout();
        };
        _table.SelectIndexChanged += (_, e) => SelectionChanged?.Invoke(this, e);
        _table.CellClick += (_, e) => SelectionChanged?.Invoke(this, e);
        _table.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex > 0)
                RowDoubleClicked?.Invoke(this, e);
        };

        // Responsive Pagination Footer (1-row >= 700px, 2-row compact < 700px)
        _footerPanel = new System.Windows.Forms.Panel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(0, 44),
            Visible = showPagination,
            Padding = new Padding(16, 6, 16, 6)
        };

        _footerGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        _footerGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _footerGrid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _footerGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _pageSummaryLabel = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            AutoEllipsis = true,
            Height = 32,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = AppTheme.SmallFont,
            Text = "Tổng: 0 bản ghi • Trang 1/1",
            Margin = new Padding(0, 0, 8, 0)
        };

        _pagination = new AntdUI.Pagination
        {
            Dock = DockStyle.Right,
            Width = 420,
            Height = 32,
            Current = 1,
            PageSize = 20,
            Total = 0,
            ShowSizeChanger = true,
            PageSizeOptions = [10, 20, 50, 100],
            SizeChangerWidth = 78,
            Radius = 6,
            Font = AppTheme.SmallFont,
            RecordsPerPageText = "/ trang",
            Margin = new Padding(0)
        };
        _pagination.ValueChanged += (_, e) =>
        {
            if (_suppressPageEvents) return;
            _currentPage = Math.Max(1, e.Current);
            _pageSize = Math.Max(10, e.PageSize);
            RaiseReload();
        };

        _footerGrid.Controls.Add(_pageSummaryLabel, 0, 0);
        _footerGrid.Controls.Add(_pagination, 1, 0);
        _footerPanel.Controls.Add(_footerGrid);

        // Docking order
        Controls.Add(_table);
        Controls.Add(_bannerAlert);
        Controls.Add(_toolbarSeparator);
        Controls.Add(_filterPanel);
        Controls.Add(headerPanel);
        Controls.Add(_footerPanel);

        SizeChanged += (_, _) => ApplyResponsiveGridLayout();
        ApplyTheme();
        ApplyResponsiveGridLayout();
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
        // EmptyHeader = true keeps column headers + consistent table body visible on 0 records without layout shift.
        _bannerAlert.Visible = false;

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
        _bannerAlert.Visible = false;

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

    private void ApplyResponsiveGridLayout()
    {
        if (IsDisposed) return;

        var width = ClientSize.Width;
        if (width < 120) return;

        var targetSearchW = Math.Clamp(width - 140, 150, 280);
        if (_searchInput.Width != targetSearchW)
            _searchInput.Width = targetSearchW;

        var compactFooter = width < 700;
        if (_lastCompactFooter != compactFooter)
        {
            _lastCompactFooter = compactFooter;
            _footerGrid.SuspendLayout();
            try
            {
                _footerGrid.Controls.Clear();
                _footerGrid.ColumnStyles.Clear();
                _footerGrid.RowStyles.Clear();

                if (compactFooter)
                {
                    _footerGrid.ColumnCount = 1;
                    _footerGrid.RowCount = 2;
                    _footerGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                    _footerGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    _footerGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                    _pagination.Dock = DockStyle.Left;
                    _pageSummaryLabel.Height = 20;
                    _pageSummaryLabel.Margin = new Padding(0, 4, 0, 0);

                    _footerGrid.Controls.Add(_pagination, 0, 0);
                    _footerGrid.Controls.Add(_pageSummaryLabel, 0, 1);
                }
                else
                {
                    _footerGrid.ColumnCount = 2;
                    _footerGrid.RowCount = 1;
                    _footerGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                    _footerGrid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                    _footerGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                    _pagination.Dock = DockStyle.Right;
                    _pageSummaryLabel.Height = 32;
                    _pageSummaryLabel.Margin = new Padding(0, 0, 8, 0);

                    _footerGrid.Controls.Add(_pageSummaryLabel, 0, 0);
                    _footerGrid.Controls.Add(_pagination, 1, 0);
                }
            }
            finally
            {
                _footerGrid.ResumeLayout(true);
            }
        }

        var innerFooterW = Math.Max(240, width - 32);
        _pagination.ShowSizeChanger = innerFooterW >= 360;
        var targetPagW = Math.Clamp(compactFooter ? innerFooterW : Math.Min(440, innerFooterW - 180), 220, 440);
        if (_pagination.Width != targetPagW)
            _pagination.Width = targetPagW;
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
