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

    public static void StyleDataGrid(DataGridView grid)
    {
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.ReadOnly = true;
        grid.MultiSelect = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.RowHeadersVisible = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
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
            Padding = new Padding(8, 0, 8, 0)
        };
        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = CardBg,
            ForeColor = TextPrimary,
            Font = BodyFont,
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
