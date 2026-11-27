using System.Globalization;

namespace Logistics.Client.WinForms.Themes;

/// <summary>
/// Centralized WinForms design tokens and formatting helpers.
/// </summary>
public static class AppTheme
{
    // Color Palette
    public static readonly Color NavyDark = Color.FromArgb(15, 23, 42);
    public static readonly Color SidebarBg = Color.FromArgb(30, 41, 59);
    public static readonly Color SidebarHover = Color.FromArgb(51, 65, 85);
    public static readonly Color PrimaryBlue = Color.FromArgb(37, 99, 235);
    public static readonly Color PrimaryHover = Color.FromArgb(29, 78, 216);
    public static readonly Color SuccessEmerald = Color.FromArgb(16, 185, 129);
    public static readonly Color WarningAmber = Color.FromArgb(245, 158, 11);
    public static readonly Color DangerRed = Color.FromArgb(220, 38, 38);
    public static readonly Color PurpleAccent = Color.FromArgb(124, 58, 237);

    public static readonly Color SurfaceBg = Color.FromArgb(245, 247, 250);
    public static readonly Color CardBg = Color.White;
    public static readonly Color BorderSubtle = Color.FromArgb(226, 232, 240);
    public static readonly Color TextPrimary = Color.FromArgb(15, 23, 42);
    public static readonly Color TextSecondary = Color.FromArgb(71, 85, 105);
    public static readonly Color TextMuted = Color.FromArgb(100, 116, 139);
    public static readonly Color RowAltBg = Color.FromArgb(248, 250, 252);

    // Typography
    public static readonly Font PageTitleFont = new("Segoe UI", 14F, FontStyle.Bold);
    public static readonly Font SectionTitleFont = new("Segoe UI", 10.5F, FontStyle.Bold);
    public static readonly Font BodyFont = new("Segoe UI", 9.5F, FontStyle.Regular);
    public static readonly Font BodyBoldFont = new("Segoe UI", 9.5F, FontStyle.Bold);
    public static readonly Font SmallFont = new("Segoe UI", 8.5F, FontStyle.Regular);
    public static readonly Font KpiValueFont = new("Segoe UI", 18F, FontStyle.Bold);

    public static Button CreatePrimaryButton(string text, int width = 120)
        => CreateStyledButton(text, PrimaryBlue, Color.White, width);

    public static Button CreateSuccessButton(string text, int width = 120)
        => CreateStyledButton(text, SuccessEmerald, Color.White, width);

    public static Button CreateDangerButton(string text, int width = 120)
        => CreateStyledButton(text, DangerRed, Color.White, width);

    public static Button CreateSecondaryButton(string text, int width = 110)
    {
        var btn = CreateStyledButton(text, Color.White, TextPrimary, width);
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.BorderColor = BorderSubtle;
        btn.FlatAppearance.MouseOverBackColor = RowAltBg;
        return btn;
    }

    private static Button CreateStyledButton(string text, Color bg, Color fg, int width)
    {
        var btn = new Button
        {
            Text = text,
            BackColor = bg,
            ForeColor = fg,
            FlatStyle = FlatStyle.Flat,
            Font = BodyBoldFont,
            Height = 34,
            Width = width,
            AutoSize = width <= 0,
            MinimumSize = new Size(Math.Max(80, width), 34),
            Padding = new Padding(10, 0, 10, 0),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 8, 4)
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    public static SplitContainer CreateSafeSplitContainer(
        Orientation orientation,
        int desiredSplitterDistance,
        int panel1MinSize = 120,
        int panel2MinSize = 120)
    {
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = orientation,
            Panel1MinSize = 0,
            Panel2MinSize = 0,
            FixedPanel = FixedPanel.None
        };

        var applied = false;
        void ApplySafeSizes()
        {
            if (applied || split.IsDisposed) return;

            var total = orientation == Orientation.Horizontal ? split.Height : split.Width;
            if (total < 160) return;

            var p1Min = Math.Min(panel1MinSize, Math.Max(25, total / 3));
            var p2Min = Math.Min(panel2MinSize, Math.Max(25, total / 3));
            var maxDist = total - split.SplitterWidth - p2Min;
            if (maxDist <= p1Min) return;

            try
            {
                split.Panel1MinSize = p1Min;
                split.Panel2MinSize = p2Min;
                split.SplitterDistance = Math.Clamp(desiredSplitterDistance, p1Min, maxDist);
                applied = true;
            }
            catch (InvalidOperationException)
            {
                // Retry on next layout pass if host is still mid-resize
            }
        }

        split.HandleCreated += (_, _) => split.BeginInvoke((Action)ApplySafeSizes);
        split.SizeChanged += (_, _) => ApplySafeSizes();
        split.Layout += (_, _) => ApplySafeSizes();

        return split;
    }

    public static void StyleDataGrid(DataGridView grid)
    {
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.ReadOnly = true;
        grid.MultiSelect = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.RowHeadersVisible = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        grid.ScrollBars = ScrollBars.Both;
        grid.ShowCellToolTips = true;
        grid.BackgroundColor = CardBg;
        grid.BorderStyle = BorderStyle.None;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.GridColor = BorderSubtle;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.ColumnHeadersHeight = 38;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = RowAltBg,
            ForeColor = TextSecondary,
            Font = BodyBoldFont,
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            WrapMode = DataGridViewTriState.False,
            Padding = new Padding(8, 0, 8, 0)
        };
        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = CardBg,
            ForeColor = TextPrimary,
            Font = BodyFont,
            WrapMode = DataGridViewTriState.False,
            SelectionBackColor = Color.FromArgb(219, 234, 254),
            SelectionForeColor = NavyDark,
            Padding = new Padding(8, 4, 8, 4)
        };
        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = RowAltBg
        };
        grid.RowTemplate.Height = 34;

        // Enable DoubleBuffered via reflection to eliminate DataGridView flicker during resize/scroll
        typeof(DataGridView)
            .GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.SetValue(grid, true);

        grid.DataBindingComplete -= OnGridDataBindingComplete;
        grid.DataBindingComplete += OnGridDataBindingComplete;
        grid.SizeChanged -= OnGridSizeChanged;
        grid.SizeChanged += OnGridSizeChanged;
    }

    private static void OnGridDataBindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
    {
        if (sender is DataGridView grid)
            ConfigureGridColumns(grid);
    }

    private static void OnGridSizeChanged(object? sender, EventArgs e)
    {
        if (sender is DataGridView grid && grid.Columns.Count > 0)
            ConfigureGridColumns(grid);
    }

    /// <summary>
    /// Sizes every visible DataGridView column to fit its header and content cleanly without header ellipsis,
    /// using horizontal scrolling for wide multi-column DTOs and proportional expansion when extra width is available.
    /// </summary>
    public static void ConfigureGridColumns(DataGridView grid)
    {
        if (grid.IsDisposed || grid.Columns.Count == 0) return;

        var headerFont = grid.ColumnHeadersDefaultCellStyle.Font ?? grid.Font ?? BodyBoldFont;
        var cellFont = grid.DefaultCellStyle.Font ?? grid.Font ?? BodyFont;
        var visibleCols = grid.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).ToList();
        if (visibleCols.Count == 0) return;

        grid.SuspendLayout();
        try
        {
            var baseWidths = new Dictionary<DataGridViewColumn, int>(visibleCols.Count);
            var rowSampleLimit = Math.Min(grid.Rows.Count, 100);

            foreach (var col in visibleCols)
            {
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                col.HeaderCell.Style.WrapMode = DataGridViewTriState.False;

                var headerText = string.IsNullOrWhiteSpace(col.HeaderText) ? col.Name : col.HeaderText;
                var headerWidth = TextRenderer.MeasureText(headerText, headerFont).Width + 28;
                var contentWidth = 0;

                for (int r = 0; r < rowSampleLimit; r++)
                {
                    var row = grid.Rows[r];
                    if (row.IsNewRow) continue;
                    var formatted = row.Cells[col.Index].FormattedValue?.ToString();
                    if (string.IsNullOrEmpty(formatted)) continue;

                    var measured = TextRenderer.MeasureText(formatted, cellFont).Width + 24;
                    if (measured > contentWidth)
                        contentWidth = measured;
                }

                var desired = Math.Max(Math.Max(72, headerWidth), contentWidth);
                desired = Math.Min(desired, 380);
                col.MinimumWidth = Math.Clamp(headerWidth, 64, 240);
                baseWidths[col] = Math.Max(col.MinimumWidth, desired);
            }

            var totalBaseWidth = baseWidths.Values.Sum();
            var availableWidth = grid.ClientSize.Width
                - (grid.RowHeadersVisible ? grid.RowHeadersWidth : 0)
                - (grid.Rows.Count * grid.RowTemplate.Height > grid.ClientSize.Height - grid.ColumnHeadersHeight
                    ? SystemInformation.VerticalScrollBarWidth
                    : 0)
                - 2;

            if (availableWidth > totalBaseWidth && visibleCols.Count > 0)
            {
                var extra = availableWidth - totalBaseWidth;
                var perCol = extra / visibleCols.Count;
                var remainder = extra % visibleCols.Count;

                for (int i = 0; i < visibleCols.Count; i++)
                {
                    var col = visibleCols[i];
                    col.Width = baseWidths[col] + perCol + (i == visibleCols.Count - 1 ? remainder : 0);
                }
            }
            else
            {
                foreach (var col in visibleCols)
                    col.Width = baseWidths[col];
            }
        }
        finally
        {
            grid.ResumeLayout(true);
        }
    }

    public static string FormatMoney(decimal amount, string? currencyCode = null)
    {
        var code = string.IsNullOrWhiteSpace(currencyCode) ? "" : $" {currencyCode}";
        return string.Equals(currencyCode, "VND", StringComparison.OrdinalIgnoreCase)
            ? $"{amount:N0} ₫"
            : $"{amount:N2}{code}";
    }

    public static string FormatLocalTime(DateTime? utcDateTime)
    {
        if (!utcDateTime.HasValue) return "-";
        var local = utcDateTime.Value.Kind == DateTimeKind.Utc
            ? utcDateTime.Value.ToLocalTime()
            : DateTime.SpecifyKind(utcDateTime.Value, DateTimeKind.Utc).ToLocalTime();
        return local.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
    }
}
