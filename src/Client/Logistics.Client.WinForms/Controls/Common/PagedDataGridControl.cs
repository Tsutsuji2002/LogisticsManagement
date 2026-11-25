using Logistics.Client.Application.Localization;
using Logistics.Client.WinForms.Themes;
using Logistics.Shared.Contracts.Pagination;

namespace Logistics.Client.WinForms.Controls.Common;

/// <summary>
/// Reusable enterprise server-paged DataGridView workspace with search, filter/action toolbars,
/// loading/empty/error states, and full pagination controls.
/// </summary>
public class PagedDataGridControl : UserControl
{
    private readonly Panel _headerCard;
    private readonly Label _titleLabel;
    private readonly Label _descLabel;
    private readonly FlowLayoutPanel _actionsToolbar;
    private readonly FlowLayoutPanel _filterPanel;
    private readonly TextBox _searchBox;
    private readonly Button _searchButton;
    private readonly Button _refreshButton;
    private readonly Panel _statusBanner;
    private readonly Label _statusBannerLabel;
    private readonly DataGridView _grid;
    private readonly Panel _pagerPanel;
    private readonly Button _btnFirst;
    private readonly Button _btnPrev;
    private readonly Label _pageInfoLabel;
    private readonly Button _btnNext;
    private readonly Button _btnLast;
    private readonly ComboBox _pageSizeBox;

    private int _currentPage = 1;
    private int _totalPages = 1;
    private int _pageSize = 20;

    public PagedDataGridControl(string title, string description, bool showSearch = true, bool showPagination = true)
    {
        Dock = DockStyle.Fill;
        BackColor = AppTheme.SurfaceBg;
        Padding = new Padding(16);

        // Top Title + Action Bar
        _headerCard = new Panel
        {
            Dock = DockStyle.Top,
            Height = 76,
            BackColor = AppTheme.CardBg,
            Padding = new Padding(16, 12, 16, 10)
        };
        _headerCard.Paint += (_, e) =>
        {
            using var p = new Pen(AppTheme.BorderSubtle);
            e.Graphics.DrawRectangle(p, 0, 0, _headerCard.Width - 1, _headerCard.Height - 1);
        };

        _titleLabel = new Label
        {
            Text = title,
            Font = AppTheme.PageTitleFont,
            ForeColor = AppTheme.NavyDark,
            AutoSize = true,
            Location = new Point(16, 12)
        };
        _descLabel = new Label
        {
            Text = description,
            Font = AppTheme.SmallFont,
            ForeColor = AppTheme.TextMuted,
            AutoSize = true,
            Location = new Point(18, 42)
        };
        _actionsToolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0)
        };

        _headerCard.Controls.Add(_actionsToolbar);
        _headerCard.Controls.Add(_titleLabel);
        _headerCard.Controls.Add(_descLabel);

        // Filter Bar
        _filterPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            MinimumSize = new Size(0, 48),
            BackColor = AppTheme.CardBg,
            Padding = new Padding(12, 8, 12, 6),
            Margin = new Padding(0, 8, 0, 8)
        };

        _searchBox = new TextBox
        {
            Width = 240,
            Font = AppTheme.BodyFont,
            PlaceholderText = AppStrings.T("Search") + "...",
            Visible = showSearch,
            Margin = new Padding(0, 4, 8, 4)
        };
        _searchBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                ResetAndReload();
            }
        };

        _searchButton = AppTheme.CreateSecondaryButton(AppStrings.T("Filter"), 88);
        _searchButton.Visible = showSearch;
        _searchButton.Click += (_, _) => ResetAndReload();

        _refreshButton = AppTheme.CreateSecondaryButton(AppStrings.T("Refresh"), 96);
        _refreshButton.Click += (_, _) => ReloadRequested?.Invoke(this, EventArgs.Empty);

        _filterPanel.Controls.Add(_searchBox);
        _filterPanel.Controls.Add(_searchButton);
        _filterPanel.Controls.Add(_refreshButton);

        // Status / Loading / Empty / Error Banner
        _statusBanner = new Panel
        {
            Dock = DockStyle.Top,
            Height = 36,
            Visible = false,
            Padding = new Padding(12, 8, 12, 8)
        };
        _statusBannerLabel = new Label
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.BodyBoldFont,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _statusBanner.Controls.Add(_statusBannerLabel);

        // Data Grid
        _grid = new DataGridView { Dock = DockStyle.Fill };
        AppTheme.StyleDataGrid(_grid);
        _grid.SelectionChanged += (_, _) => SelectionChanged?.Invoke(this, EventArgs.Empty);
        _grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0) RowDoubleClicked?.Invoke(this, EventArgs.Empty);
        };

        // Footer Pager
        _pagerPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            BackColor = AppTheme.CardBg,
            Visible = showPagination,
            Padding = new Padding(12, 5, 12, 5)
        };
        _btnFirst = AppTheme.CreateSecondaryButton("<<", 48);
        _btnPrev = AppTheme.CreateSecondaryButton("<", 48);
        _btnNext = AppTheme.CreateSecondaryButton(">", 48);
        _btnLast = AppTheme.CreateSecondaryButton(">>", 48);
        _pageInfoLabel = new Label
        {
            Text = "Trang 1 / 1 (0 bản ghi)",
            Font = AppTheme.BodyFont,
            ForeColor = AppTheme.TextSecondary,
            AutoSize = true,
            Padding = new Padding(8, 8, 8, 0)
        };
        _pageSizeBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 76,
            Font = AppTheme.BodyFont,
            Margin = new Padding(8, 4, 0, 0)
        };
        _pageSizeBox.Items.AddRange([10, 20, 50, 100]);
        _pageSizeBox.SelectedItem = 20;
        _pageSizeBox.SelectedIndexChanged += (_, _) =>
        {
            if (_pageSizeBox.SelectedItem is int size)
            {
                _pageSize = size;
                ResetAndReload();
            }
        };

        var pagerFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        pagerFlow.Controls.AddRange([_btnFirst, _btnPrev, _pageInfoLabel, _btnNext, _btnLast, _pageSizeBox]);
        _pagerPanel.Controls.Add(pagerFlow);

        _btnFirst.Click += (_, _) => GoToPage(1);
        _btnPrev.Click += (_, _) => GoToPage(_currentPage - 1);
        _btnNext.Click += (_, _) => GoToPage(_currentPage + 1);
        _btnLast.Click += (_, _) => GoToPage(_totalPages);

        // Docking order
        Controls.Add(_grid);
        Controls.Add(_statusBanner);
        Controls.Add(_pagerPanel);
        Controls.Add(_filterPanel);
        Controls.Add(_headerCard);
    }

    public DataGridView Grid => _grid;
    public FlowLayoutPanel ActionsToolbar => _actionsToolbar;
    public FlowLayoutPanel FilterPanel => _filterPanel;
    public string SearchText => _searchBox.Text.Trim();
    public int CurrentPage => _currentPage;
    public int CurrentPageSize => _pageSize;

    public event EventHandler? ReloadRequested;
    public event EventHandler? SelectionChanged;
    public event EventHandler? RowDoubleClicked;

    public T? GetSelectedRow<T>() where T : class
        => _grid.SelectedRows.Count > 0 ? _grid.SelectedRows[0].DataBoundItem as T : null;

    public void AddActionButton(Button btn)
    {
        _actionsToolbar.Controls.Add(btn);
    }

    public void AddFilterControl(Control ctrl)
    {
        // Insert custom filters before Refresh button
        _filterPanel.Controls.Add(ctrl);
        _filterPanel.Controls.SetChildIndex(ctrl, Math.Max(0, _filterPanel.Controls.Count - 2));
    }

    public void ResetAndReload()
    {
        _currentPage = 1;
        ReloadRequested?.Invoke(this, EventArgs.Empty);
    }

    public void SetLoading(bool isLoading)
    {
        if (isLoading)
        {
            _statusBanner.BackColor = Color.FromArgb(239, 246, 255);
            _statusBannerLabel.ForeColor = AppTheme.PrimaryBlue;
            _statusBannerLabel.Text = AppStrings.T("Loading");
            _statusBanner.Visible = true;
        }
        else if (_statusBannerLabel.Text == AppStrings.T("Loading"))
        {
            _statusBanner.Visible = false;
        }
    }

    public void SetError(string message)
    {
        _statusBanner.BackColor = Color.FromArgb(254, 242, 242);
        _statusBannerLabel.ForeColor = AppTheme.DangerRed;
        _statusBannerLabel.Text = message;
        _statusBanner.Visible = true;
    }

    public void BindPagedData<T>(PagedResponse<T> paged)
    {
        _currentPage = Math.Max(1, paged.Page);
        _totalPages = Math.Max(1, paged.TotalPages);

        _grid.DataSource = null;
        _grid.DataSource = paged.Items.ToList();

        _btnFirst.Enabled = paged.HasPreviousPage;
        _btnPrev.Enabled = paged.HasPreviousPage;
        _btnNext.Enabled = paged.HasNextPage;
        _btnLast.Enabled = paged.HasNextPage;
        _pageInfoLabel.Text = $"Trang {paged.Page} / {Math.Max(1, paged.TotalPages)} ({paged.TotalCount:N0} bản ghi)";

        if (paged.Items.Count == 0)
        {
            _statusBanner.BackColor = AppTheme.RowAltBg;
            _statusBannerLabel.ForeColor = AppTheme.TextMuted;
            _statusBannerLabel.Text = AppStrings.T("EmptyData");
            _statusBanner.Visible = true;
        }
        else
        {
            _statusBanner.Visible = false;
        }
    }

    public void BindListData<T>(IReadOnlyList<T> items)
    {
        _grid.DataSource = null;
        _grid.DataSource = items.ToList();
        _pageInfoLabel.Text = $"{items.Count:N0} bản ghi";
        if (items.Count == 0)
        {
            _statusBanner.BackColor = AppTheme.RowAltBg;
            _statusBannerLabel.ForeColor = AppTheme.TextMuted;
            _statusBannerLabel.Text = AppStrings.T("EmptyData");
            _statusBanner.Visible = true;
        }
        else
        {
            _statusBanner.Visible = false;
        }
    }

    private void GoToPage(int target)
    {
        var clamped = Math.Clamp(target, 1, Math.Max(1, _totalPages));
        if (clamped == _currentPage) return;
        _currentPage = clamped;
        ReloadRequested?.Invoke(this, EventArgs.Empty);
    }
}
