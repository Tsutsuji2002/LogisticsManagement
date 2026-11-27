using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;
using AntdUI;
using Label = System.Windows.Forms.Label;

namespace Logistics.Client.WinForms.Themes;

public enum AppThemeMode
{
    Light = 0,
    Dark = 1
}

/// <summary>
/// Implemented by Forms and UserControls that refresh custom or nested styles when theme switches.
/// </summary>
public interface IThemeAware
{
    void ApplyTheme();
}

/// <summary>
/// Centralized AntdUI design tokens, Light/Dark theme state, typography, table builders, and formatting helpers.
/// </summary>
public static class AppTheme
{
    private static readonly string ThemePrefPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LogisticsManagement",
        "theme.pref");

    // Brand & Semantic Palette (Ant Design inspired enterprise logistics)
    public static readonly Color NavyDark = Color.FromArgb(15, 23, 42);
    public static readonly Color SidebarBg = Color.FromArgb(0, 21, 41); // Ant Design Dark Sider #001529
    public static readonly Color SidebarHover = Color.FromArgb(17, 45, 78);
    public static readonly Color PrimaryBlue = Color.FromArgb(22, 119, 255); // Ant Design #1677FF
    public static readonly Color PrimaryHover = Color.FromArgb(64, 150, 255);
    public static readonly Color SuccessEmerald = Color.FromArgb(22, 163, 74);
    public static readonly Color WarningAmber = Color.FromArgb(217, 119, 6);
    public static readonly Color DangerRed = Color.FromArgb(220, 38, 38);
    public static readonly Color PurpleAccent = Color.FromArgb(114, 46, 209);

    public static AppThemeMode CurrentMode { get; private set; } = LoadSavedMode();
    public static bool IsDark => CurrentMode == AppThemeMode.Dark;

    // Adaptive Surface & Text Tokens
    public static Color SurfaceBg => IsDark ? Color.FromArgb(20, 20, 20) : Color.FromArgb(245, 247, 250);
    public static Color CardBg => IsDark ? Color.FromArgb(31, 31, 31) : Color.White;
    public static Color RowAltBg => IsDark ? Color.FromArgb(38, 38, 38) : Color.FromArgb(248, 250, 252);
    public static Color BorderSubtle => IsDark ? Color.FromArgb(51, 51, 51) : Color.FromArgb(226, 232, 240);
    public static Color TextPrimary => IsDark ? Color.FromArgb(240, 240, 240) : Color.FromArgb(15, 23, 42);
    public static Color TextSecondary => IsDark ? Color.FromArgb(166, 166, 166) : Color.FromArgb(71, 85, 105);
    public static Color TextMuted => IsDark ? Color.FromArgb(115, 115, 115) : Color.FromArgb(100, 116, 139);
    public static Color HeaderBg => IsDark ? Color.FromArgb(26, 26, 26) : Color.White;

    // Typography (Segoe UI for crisp Vietnamese & English glyph rendering)
    public static readonly Font PageTitleFont = new("Segoe UI", 14F, FontStyle.Bold);
    public static readonly Font SectionTitleFont = new("Segoe UI", 10.5F, FontStyle.Bold);
    public static readonly Font BodyFont = new("Segoe UI", 9.5F, FontStyle.Regular);
    public static readonly Font BodyBoldFont = new("Segoe UI", 9.5F, FontStyle.Bold);
    public static readonly Font SmallFont = new("Segoe UI", 8.5F, FontStyle.Regular);
    public static readonly Font KpiValueFont = new("Segoe UI", 18F, FontStyle.Bold);

    public static event EventHandler? ThemeChanged;

    public static void InitializeAntdTheme()
    {
        AntdUI.Config.Animation = true;
        AntdUI.Config.ShadowEnabled = true;
        AntdUI.Config.TextRenderingHighQuality = true;
        AntdUI.Style.SetPrimary(PrimaryBlue);
        AntdUI.Style.SetSuccess(SuccessEmerald);
        AntdUI.Style.SetWarning(WarningAmber);
        AntdUI.Style.SetError(DangerRed);
        AntdUI.Style.SetInfo(PrimaryBlue);
        AntdUI.Config.IsDark = IsDark;
    }

    public static void SetTheme(AppThemeMode mode, Form? rootForm = null)
    {
        CurrentMode = mode;
        SaveMode(mode);
        InitializeAntdTheme();

        if (rootForm != null && !rootForm.IsDisposed)
            ApplyThemeToTree(rootForm);

        foreach (Form openForm in System.Windows.Forms.Application.OpenForms.Cast<Form>().ToList())
        {
            if (!openForm.IsDisposed && openForm != rootForm)
                ApplyThemeToTree(openForm);
        }

        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }

    public static void ToggleTheme(Form? rootForm = null)
        => SetTheme(IsDark ? AppThemeMode.Light : AppThemeMode.Dark, rootForm);

    public static void ApplyThemeToTree(Control root)
    {
        if (root.IsDisposed) return;

        if (root is AntdUI.BaseForm antForm)
            antForm.Mode = IsDark ? TAMode.Dark : TAMode.Light;

        if (root is IThemeAware aware)
            aware.ApplyTheme();
        else
            ApplyDefaultControlColors(root);

        foreach (Control child in root.Controls)
            ApplyThemeToTree(child);
    }

    private static void ApplyDefaultControlColors(Control ctrl)
    {
        switch (ctrl)
        {
            case AntdUI.Panel antPanel:
                if (antPanel.BorderWidth > 0)
                {
                    antPanel.Back = CardBg;
                    antPanel.BorderColor = BorderSubtle;
                }
                antPanel.ForeColor = TextPrimary;
                antPanel.Invalidate();
                break;

            case AntdUI.Table antTable:
                antTable.Back = CardBg;
                antTable.ForeColor = TextPrimary;
                antTable.BorderColor = BorderSubtle;
                antTable.ColumnBack = RowAltBg;
                antTable.ColumnFore = TextSecondary;
                antTable.Invalidate();
                break;

            case AntdUI.Tabs antTabs:
                antTabs.ForeColor = TextPrimary;
                antTabs.Fill = PrimaryBlue;
                antTabs.Invalidate();
                foreach (AntdUI.TabPage tp in antTabs.Pages)
                {
                    tp.BackColor = SurfaceBg;
                    tp.ForeColor = TextPrimary;
                }
                break;

            case AntdUI.Splitter antSplit:
                antSplit.BackColor = SurfaceBg;
                antSplit.SplitterBack = BorderSubtle;
                antSplit.Invalidate();
                break;

            case DataGridView dgv:
                StyleDataGrid(dgv);
                break;

            case UserControl uc:
                uc.BackColor = SurfaceBg;
                uc.ForeColor = TextPrimary;
                break;

            case Form frm:
                frm.BackColor = SurfaceBg;
                frm.ForeColor = TextPrimary;
                break;

            case Label lbl when lbl.ForeColor != DangerRed && lbl.ForeColor != SuccessEmerald && lbl.ForeColor != PrimaryBlue:
                lbl.ForeColor = lbl.Font.Size <= 8.6F ? TextMuted : TextPrimary;
                break;
        }
    }

    private static AppThemeMode LoadSavedMode()
    {
        try
        {
            if (File.Exists(ThemePrefPath))
            {
                var raw = File.ReadAllText(ThemePrefPath).Trim();
                if (Enum.TryParse<AppThemeMode>(raw, true, out var parsed))
                    return parsed;
            }
        }
        catch
        {
            // Ignore malformed preference file
        }
        return AppThemeMode.Light;
    }

    private static void SaveMode(AppThemeMode mode)
    {
        try
        {
            var dir = Path.GetDirectoryName(ThemePrefPath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(ThemePrefPath, mode.ToString());
        }
        catch
        {
            // Ignore disk write failure on read-only profiles
        }
    }

    // AntdUI Button Factories
    public static AntdUI.Button CreatePrimaryButton(string text, int width = 120)
        => CreateAntButton(text, TTypeMini.Primary, width, ghost: false);

    public static AntdUI.Button CreateSuccessButton(string text, int width = 120)
        => CreateAntButton(text, TTypeMini.Success, width, ghost: false);

    public static AntdUI.Button CreateDangerButton(string text, int width = 120)
        => CreateAntButton(text, TTypeMini.Error, width, ghost: false);

    public static AntdUI.Button CreateWarnButton(string text, int width = 120)
        => CreateAntButton(text, TTypeMini.Warn, width, ghost: false);

    public static AntdUI.Button CreateSecondaryButton(string text, int width = 110)
        => CreateAntButton(text, TTypeMini.Default, width, ghost: false);

    private static AntdUI.Button CreateAntButton(string text, TTypeMini type, int width, bool ghost)
    {
        var desiredWidth = width > 0 ? Math.Max(width, TextRenderer.MeasureText(text, BodyBoldFont).Width + 28) : 110;
        var btn = new AntdUI.Button
        {
            Text = text,
            Type = type,
            Ghost = ghost,
            Radius = 6,
            WaveSize = 2,
            BorderWidth = type == TTypeMini.Default ? 1F : 0F,
            Font = BodyBoldFont,
            Height = 34,
            Width = desiredWidth,
            MinimumSize = new Size(Math.Max(76, desiredWidth), 34),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 8, 4)
        };
        return btn;
    }

    public static AntdUI.Panel CreateCardPanel(Padding? padding = null, int radius = 8)
        => new()
        {
            Radius = radius,
            BorderWidth = 1F,
            BorderColor = BorderSubtle,
            Back = CardBg,
            ForeColor = TextPrimary,
            Padding = padding ?? new Padding(12),
            AutoContainerBgTransparent = true
        };

    public static AntdUI.Tabs CreateAntdTabs()
        => new()
        {
            Dock = DockStyle.Fill,
            Type = TabType.Line,
            Font = BodyFont,
            Fill = PrimaryBlue,
            Gap = 6,
            EnablePageScrolling = true
        };

    /// <summary>
    /// Creates an AntdUI.Splitter with proportional, resize-safe PanelMinSize and SplitterDistance clamping
    /// so horizontal/vertical panes never throw ArgumentOutOfRangeException or clip during shrink/grow.
    /// </summary>
    public static AntdUI.Splitter CreateSafeSplitContainer(
        Orientation orientation,
        int desiredSplitterDistance,
        int panel1MinSize = 120,
        int panel2MinSize = 120)
    {
        var split = new AntdUI.Splitter
        {
            Dock = DockStyle.Fill,
            Orientation = orientation,
            Panel1MinSize = 0,
            Panel2MinSize = 0,
            FixedPanel = FixedPanel.None,
            SplitterWidth = 5,
            SplitterSize = 5,
            BackColor = SurfaceBg,
            SplitterBack = BorderSubtle
        };

        var initialized = false;
        var updating = false;
        var userRatio = 0.0;

        void ApplySafeSizes()
        {
            if (updating || split.IsDisposed) return;

            var total = orientation == Orientation.Horizontal ? split.Height : split.Width;
            if (total < 160) return;

            var p1Min = Math.Min(panel1MinSize, Math.Max(30, total / 3));
            var p2Min = Math.Min(panel2MinSize, Math.Max(30, total / 3));
            var maxDist = total - split.SplitterWidth - p2Min;
            if (maxDist <= p1Min) return;

            updating = true;
            try
            {
                if (!initialized)
                {
                    var initialDist = Math.Clamp(desiredSplitterDistance, p1Min, maxDist);
                    split.Panel1MinSize = p1Min;
                    split.Panel2MinSize = p2Min;
                    split.SplitterDistance = initialDist;
                    userRatio = initialDist / (double)total;
                    initialized = true;
                }
                else
                {
                    var targetDist = Math.Clamp((int)Math.Round(total * userRatio), p1Min, maxDist);
                    if (split.Panel1MinSize != p1Min) split.Panel1MinSize = p1Min;
                    if (split.Panel2MinSize != p2Min) split.Panel2MinSize = p2Min;
                    if (Math.Abs(split.SplitterDistance - targetDist) > 2)
                        split.SplitterDistance = targetDist;
                }
            }
            catch (InvalidOperationException)
            {
                // Retry on next layout pass if host is still mid-resize
            }
            finally
            {
                updating = false;
            }
        }

        split.SplitterMoved += (_, _) =>
        {
            if (updating || !initialized || split.IsDisposed) return;
            var total = orientation == Orientation.Horizontal ? split.Height : split.Width;
            if (total >= 160)
                userRatio = Math.Clamp(split.SplitterDistance / (double)total, 0.15, 0.85);
        };
        split.HandleCreated += (_, _) => split.BeginInvoke((Action)ApplySafeSizes);
        split.SizeChanged += (_, _) => ApplySafeSizes();
        split.Layout += (_, _) => ApplySafeSizes();

        return split;
    }

    /// <summary>
    /// Dynamically sizes an AntdUI.Panel card height to its docked-top children (with multiline Label wrap support)
    /// so cards shrink cleanly and grow instead of clipping when wrapped across narrower widths.
    /// </summary>
    public static void BindAutoHeightCard(AntdUI.Panel card, int minHeight = 52)
    {
        void SyncLayout()
        {
            if (card.IsDisposed) return;

            var innerW = Math.Max(160, card.ClientSize.Width - card.Padding.Horizontal);
            foreach (Control child in card.Controls)
            {
                if (child is Label lbl && lbl.AutoSize && lbl.Dock == DockStyle.Top)
                {
                    if (lbl.MaximumSize.Width != innerW)
                        lbl.MaximumSize = new Size(innerW, 0);
                }
            }

            var contentH = card.Controls.Cast<Control>()
                .Where(c => c.Visible)
                .Sum(c => c.Height + c.Margin.Vertical);
            var targetH = Math.Max(minHeight, contentH + card.Padding.Vertical + 4);
            if (Math.Abs(card.Height - targetH) > 1)
                card.Height = targetH;
        }

        card.Layout += (_, _) => SyncLayout();
        card.SizeChanged += (_, _) => SyncLayout();
        card.ControlAdded += (_, e) =>
        {
            if (e.Control != null)
                e.Control.SizeChanged += (_, _) => SyncLayout();
            SyncLayout();
        };
        foreach (Control existing in card.Controls)
            existing.SizeChanged += (_, _) => SyncLayout();

        SyncLayout();
    }

    public static AntdUI.Table CreateAntdTable(bool bordered = false)
        => new()
        {
            Dock = DockStyle.Fill,
            FixedHeader = true,
            VisibleHeader = true,
            EmptyHeader = true,
            Bordered = bordered,
            EnableHeaderResizing = true,
            LostFocusClearSelection = false,
            ShowTip = true,
            ScrollBarAvoidHeader = true,
            AutoSizeColumnsMode = ColumnsMode.Auto,
            RowHeight = 38,
            RowHeightHeader = 40,
            Radius = 6,
            Gap = 8,
            Font = BodyFont,
            ColumnFont = BodyBoldFont,
            Back = CardBg,
            ForeColor = TextPrimary,
            BorderColor = BorderSubtle,
            ColumnBack = RowAltBg,
            ColumnFore = TextSecondary,
            EmptyText = "Không có dữ liệu phù hợp."
        };

    /// <summary>
    /// Binds typed DTO rows to an AntdUI.Table with auto-measured column widths (no truncated headers),
    /// localized timestamps, numeric alignment, and semantic CellTag status badges.
    /// </summary>
    public static void BindAntdTable<T>(AntdUI.Table table, IEnumerable<T>? items)
        => BindAntdTable(table, typeof(T), items?.Cast<object>().ToList() ?? []);

    public static void BindAntdTable(AntdUI.Table table, Type itemType, IList items)
    {
        if (table.IsDisposed) return;

        var list = items ?? Array.Empty<object>();
        var props = itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && IsDisplayableProperty(p.PropertyType))
            .ToArray();

        var headerFont = table.ColumnFont ?? BodyBoldFont;
        var cellFont = table.Font ?? BodyFont;
        var sampleCount = Math.Min(list.Count, 80);

        var cols = new List<AntdUI.Column>(props.Length);

        foreach (var prop in props)
        {
            var title = FormatColumnTitle(prop.Name);
            var underlying = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            var isStatus = StatusTagHelper.IsStatusColumn(prop.Name, prop.PropertyType);
            var isDate = underlying == typeof(DateTime) || underlying == typeof(DateTimeOffset);
            var isDecimal = underlying == typeof(decimal) || underlying == typeof(double) || underlying == typeof(float);
            var isNumeric = isDecimal || underlying == typeof(int) || underlying == typeof(long);
            var isStringList = typeof(IEnumerable<string>).IsAssignableFrom(prop.PropertyType) && prop.PropertyType != typeof(string);
            var isLongTextCol = IsLongTextColumnName(prop.Name);

            var align = isNumeric && !prop.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase)
                ? ColumnAlign.Right
                : isStatus ? ColumnAlign.Center : ColumnAlign.Left;

            var col = new AntdUI.Column(prop.Name, title, align)
            {
                ReadOnly = true,
                Ellipsis = true,
                Render = BuildColumnRenderer(prop, isStatus, isDate, isDecimal, isStringList)
            };

            var headerWidth = TextRenderer.MeasureText(title, headerFont).Width + 36;
            var contentWidth = 0;

            for (int r = 0; r < sampleCount; r++)
            {
                var row = list[r];
                if (row == null) continue;
                var val = prop.GetValue(row);
                var text = FormatCellSampleText(prop, val, isDate, isDecimal, isStringList);
                if (string.IsNullOrEmpty(text)) continue;

                var extra = isStatus ? 34 : 28;
                var measured = TextRenderer.MeasureText(text, cellFont).Width + extra;
                if (measured > contentWidth)
                    contentWidth = measured;
            }

            var maxColWidth = isLongTextCol ? 460 : 380;
            var desired = Math.Clamp(Math.Max(Math.Max(78, headerWidth), contentWidth), 78, maxColWidth);
            var minW = Math.Clamp(headerWidth, 72, 260);
            var baseWidth = Math.Max(minW, desired);
            col.Tag = baseWidth;
            col.MinWidth = $"{minW}";
            col.Width = $"{baseWidth}";
            cols.Add(col);
        }

        DistributeAntdTableColumnWidths(table, cols);

        table.PauseLayout = true;
        try
        {
            table.Columns.Clear();
            foreach (var c in cols)
                table.Columns.Add(c);
            table.DataSource = list;
        }
        finally
        {
            table.PauseLayout = false;
            table.LoadLayout();
            table.Invalidate();
        }
    }

    /// <summary>
    /// Reflows column widths of an already-bound AntdUI.Table when its viewport size changes,
    /// preserving DataSource, scroll state, and active selection.
    /// </summary>
    public static void UpdateAntdTableColumnWidths(AntdUI.Table table)
    {
        if (table.IsDisposed || table.Columns == null || table.Columns.Count == 0) return;
        if (table.ClientSize.Width < 120) return;

        var cols = table.Columns.Cast<AntdUI.Column>().ToList();
        if (DistributeAntdTableColumnWidths(table, cols))
        {
            table.PauseLayout = true;
            try
            {
                table.LoadLayout();
            }
            finally
            {
                table.PauseLayout = false;
                table.Invalidate();
            }
        }
    }

    private static bool DistributeAntdTableColumnWidths(AntdUI.Table table, IList<AntdUI.Column> cols)
    {
        if (cols.Count == 0) return false;

        var baseWidths = new int[cols.Count];
        var totalBaseW = 0;
        for (int i = 0; i < cols.Count; i++)
        {
            var bw = cols[i].Tag is int tagged && tagged >= 60 ? tagged : 110;
            baseWidths[i] = bw;
            totalBaseW += bw;
        }

        var available = table.ClientSize.Width - 20;
        var extra = available > 200 && available > totalBaseW ? available - totalBaseW : 0;
        var perCol = extra / cols.Count;
        var rem = extra % cols.Count;
        var changed = false;

        for (int i = 0; i < cols.Count; i++)
        {
            var finalW = baseWidths[i] + perCol + (i == cols.Count - 1 ? rem : 0);
            var targetWidth = $"{finalW}";
            if (!string.Equals(cols[i].Width, targetWidth, StringComparison.Ordinal))
            {
                cols[i].Width = targetWidth;
                changed = true;
            }
        }

        return changed;
    }

    private static bool IsLongTextColumnName(string propName)
        => propName.Contains("Description", StringComparison.OrdinalIgnoreCase)
           || propName.Contains("Notes", StringComparison.OrdinalIgnoreCase)
           || propName.Contains("Address", StringComparison.OrdinalIgnoreCase)
           || propName.Contains("Email", StringComparison.OrdinalIgnoreCase)
           || propName.Contains("Reason", StringComparison.OrdinalIgnoreCase)
           || propName.Contains("Message", StringComparison.OrdinalIgnoreCase)
           || propName.Contains("Name", StringComparison.OrdinalIgnoreCase)
           || propName.Contains("Title", StringComparison.OrdinalIgnoreCase);

    public static T? GetSelectedAntdRow<T>(AntdUI.Table table) where T : class
    {
        if (table.IsDisposed) return null;

        var selected = table.SelectedsReal();
        if (selected is { Length: > 0 } && selected[0] is T typed)
            return typed;

        if (table.SelectedIndex > 0 && table.DataSource is IList list)
        {
            // AntdUI Table.SelectedIndex is 1-based when header row is index 0
            var idx = table.SelectedIndex - 1;
            if (idx >= 0 && idx < list.Count && list[idx] is T fallback)
                return fallback;
        }

        return null;
    }

    private static bool IsDisplayableProperty(Type type)
    {
        var u = Nullable.GetUnderlyingType(type) ?? type;
        if (u == typeof(string) || u == typeof(decimal) || u == typeof(DateTime) || u == typeof(DateTimeOffset) ||
            u == typeof(DateOnly) || u == typeof(TimeOnly) || u == typeof(Guid) || u.IsPrimitive || u.IsEnum)
            return true;

        if (typeof(IEnumerable<string>).IsAssignableFrom(type))
            return true;

        return false;
    }

    private static Func<object?, object, int, object?> BuildColumnRenderer(
        PropertyInfo prop,
        bool isStatus,
        bool isDate,
        bool isDecimal,
        bool isStringList)
    {
        if (isStatus)
        {
            return (value, _, _) => StatusTagHelper.CreateCellTag(value);
        }

        if (isDate)
        {
            return (value, _, _) => value switch
            {
                DateTime dt => FormatLocalTime(dt),
                DateTimeOffset dto => FormatLocalTime(dto.UtcDateTime),
                _ => "-"
            };
        }

        if (isDecimal)
        {
            var isRate = prop.Name.Contains("Percent", StringComparison.OrdinalIgnoreCase) ||
                         prop.Name.Contains("Rate", StringComparison.OrdinalIgnoreCase) ||
                         prop.Name.Contains("Weight", StringComparison.OrdinalIgnoreCase);
            return (value, _, _) => value switch
            {
                decimal d => isRate ? $"{d:N2}" : $"{d:N0}",
                double dbl => $"{dbl:N2}",
                float flt => $"{flt:N2}",
                _ => "-"
            };
        }

        if (isStringList)
        {
            return (value, _, _) => value is IEnumerable<string> seq
                ? string.Join(", ", seq)
                : "-";
        }

        return (value, _, _) => value?.ToString() ?? "-";
    }

    private static string FormatCellSampleText(
        PropertyInfo prop,
        object? value,
        bool isDate,
        bool isDecimal,
        bool isStringList)
    {
        if (value == null) return "-";
        if (value is bool) return "Yes";
        if (isDate)
        {
            return value switch
            {
                DateTime dt => FormatLocalTime(dt),
                DateTimeOffset dto => FormatLocalTime(dto.UtcDateTime),
                _ => value.ToString() ?? "-"
            };
        }
        if (isDecimal && value is decimal d)
            return $"{d:N2}";
        if (isStringList && value is IEnumerable<string> seq)
            return string.Join(", ", seq);
        return value.ToString() ?? string.Empty;
    }

    public static string FormatColumnTitle(string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName)) return string.Empty;

        if (propertyName.Equals("PublicId", StringComparison.Ordinal)) return "Public ID";
        if (propertyName.Equals("CreatedAtUtc", StringComparison.Ordinal)) return "Created At";
        if (propertyName.Equals("UpdatedAtUtc", StringComparison.Ordinal)) return "Updated At";
        if (propertyName.Equals("PublishedAtUtc", StringComparison.Ordinal)) return "Published At";
        if (propertyName.Equals("RequestedAtUtc", StringComparison.Ordinal)) return "Requested At";
        if (propertyName.Equals("ApprovedAtUtc", StringComparison.Ordinal)) return "Approved At";
        if (propertyName.Equals("DeliveredAt", StringComparison.Ordinal)) return "Delivered At";
        if (propertyName.Equals("CodAmount", StringComparison.Ordinal)) return "COD Amount";

        var sb = new StringBuilder(propertyName.Length + 6);
        for (int i = 0; i < propertyName.Length; i++)
        {
            var c = propertyName[i];
            if (i > 0 && char.IsUpper(c) && (char.IsLower(propertyName[i - 1]) ||
                (i + 1 < propertyName.Length && char.IsLower(propertyName[i + 1]))))
            {
                sb.Append(' ');
            }
            sb.Append(c);
        }

        var result = sb.ToString();
        if (result.EndsWith(" Utc", StringComparison.Ordinal))
            result = result[..^4];
        return result;
    }

    // Legacy DataGridView styling retained temporarily for compatibility while detail grids complete migration
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
            SelectionBackColor = IsDark ? Color.FromArgb(29, 78, 216) : Color.FromArgb(219, 234, 254),
            SelectionForeColor = IsDark ? Color.White : NavyDark,
            Padding = new Padding(8, 4, 8, 4)
        };
        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = RowAltBg
        };
        grid.RowTemplate.Height = 34;

        typeof(DataGridView)
            .GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
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
