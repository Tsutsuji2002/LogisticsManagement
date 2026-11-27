using System.Drawing.Drawing2D;
using AntdUI;
using Logistics.Client.WinForms.Themes;
using Label = System.Windows.Forms.Label;

namespace Logistics.Client.WinForms.Controls.Common;

/// <summary>
/// AntdUI Card displaying an enterprise KPI metric with semantic accent indicator.
/// </summary>
public sealed class KpiCardControl : AntdUI.Panel, IThemeAware
{
    private readonly Label _titleLabel;
    private readonly Label _valueLabel;
    private readonly Label _subtitleLabel;
    private readonly Color _accentColor;
    private readonly System.Windows.Forms.Panel _accentStripe;

    public KpiCardControl(string title, Color accentColor)
        : this(title, "0", string.Empty, accentColor)
    {
    }

    public KpiCardControl(string title, string initialValue, string initialSubtitle, Color accentColor)
    {
        _accentColor = accentColor;
        Radius = 8;
        BorderWidth = 1F;
        Margin = new Padding(0, 0, 12, 12);
        Padding = new Padding(14, 12, 14, 12);
        MinimumSize = new Size(190, 108);
        Height = 112;

        _accentStripe = new System.Windows.Forms.Panel
        {
            Dock = DockStyle.Left,
            Width = 4,
            BackColor = _accentColor,
            Margin = new Padding(0, 2, 10, 2)
        };

        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(10, 0, 0, 0),
            BackColor = Color.Transparent
        };
        stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        stack.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _titleLabel = new Label
        {
            Text = title,
            Font = AppTheme.SmallFont,
            AutoSize = true,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };

        _valueLabel = new Label
        {
            Text = initialValue,
            Font = AppTheme.KpiValueFont,
            AutoSize = true,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 4, 0, 2)
        };

        _subtitleLabel = new Label
        {
            Text = initialSubtitle,
            Font = AppTheme.SmallFont,
            AutoSize = true,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };

        stack.Controls.Add(_titleLabel, 0, 0);
        stack.Controls.Add(_valueLabel, 0, 1);
        stack.Controls.Add(_subtitleLabel, 0, 2);

        Controls.Add(stack);
        Controls.Add(_accentStripe);
        ApplyTheme();
    }

    public void UpdateMetric(string value, string? subtitle = null)
        => SetData(value, subtitle);

    public void SetData(string value, string? subtitle = null)
    {
        _valueLabel.Text = value;
        _subtitleLabel.Text = subtitle ?? string.Empty;
    }

    public void ApplyTheme()
    {
        Back = AppTheme.CardBg;
        BorderColor = AppTheme.BorderSubtle;
        _accentStripe.BackColor = _accentColor;
        _titleLabel.ForeColor = AppTheme.TextSecondary;
        _valueLabel.ForeColor = AppTheme.TextPrimary;
        _subtitleLabel.ForeColor = AppTheme.TextMuted;
        Invalidate();
    }
}

public sealed record BarChartDatum(string Label, decimal Value, string? DisplayValue = null, Color? Color = null)
{
    public BarChartDatum(string label, decimal value, Color? color)
        : this(label, value, null, color)
    {
    }
}

/// <summary>
/// Theme-aware horizontal bar chart inside an AntdUI Card container.
/// </summary>
public sealed class SimpleBarChartControl : AntdUI.Panel, IThemeAware
{
    private string _title = string.Empty;
    private IReadOnlyList<BarChartDatum> _data = Array.Empty<BarChartDatum>();

    public SimpleBarChartControl()
        : this(string.Empty)
    {
    }

    public SimpleBarChartControl(string title)
    {
        _title = title;
        DoubleBuffered = true;
        Radius = 8;
        BorderWidth = 1F;
        Padding = new Padding(14);
        MinimumSize = new Size(280, 220);
        ApplyTheme();
    }

    public void SetData(string title, IReadOnlyList<BarChartDatum> data)
        => SetSeries(title, data);

    public void SetSeries(string title, IReadOnlyList<BarChartDatum> data)
    {
        _title = title;
        _data = data ?? Array.Empty<BarChartDatum>();
        Invalidate();
    }

    public void ApplyTheme()
    {
        Back = AppTheme.CardBg;
        BorderColor = AppTheme.BorderSubtle;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using var titleBrush = new SolidBrush(AppTheme.TextPrimary);
        using var textBrush = new SolidBrush(AppTheme.TextSecondary);
        using var mutedBrush = new SolidBrush(AppTheme.TextMuted);
        using var trackBrush = new SolidBrush(AppTheme.IsDark ? Color.FromArgb(46, 46, 46) : AppTheme.RowAltBg);

        g.DrawString(_title, AppTheme.SectionTitleFont, titleBrush, 16, 14);

        if (_data.Count == 0)
        {
            g.DrawString("Không có dữ liệu biểu đồ.", AppTheme.BodyFont, mutedBrush, 16, 48);
            return;
        }

        var max = Math.Max(1m, _data.Max(d => d.Value));
        var top = 46;
        var rowHeight = Math.Max(22, Math.Min(34, (Height - top - 16) / Math.Max(1, _data.Count)));
        var labelWidth = Math.Clamp((int)(Width * 0.30), 100, 195);
        var valueWidth = Math.Clamp((int)(Width * 0.18), 76, 110);
        var barAreaWidth = Math.Max(48, Width - labelWidth - valueWidth - 40);

        using var ellipsisLeft = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
        using var ellipsisRight = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap, Alignment = StringAlignment.Far };

        for (int i = 0; i < _data.Count; i++)
        {
            var y = top + i * rowHeight;
            if (y + rowHeight > Height - 8) break;

            var item = _data[i];
            var barColor = item.Color ?? AppTheme.PrimaryBlue;
            var ratio = (double)(item.Value / max);
            var barWidth = (int)Math.Round(barAreaWidth * Math.Clamp(ratio, 0, 1));

            g.DrawString(item.Label, AppTheme.SmallFont, textBrush, new RectangleF(16, y + 4, labelWidth - 8, rowHeight), ellipsisLeft);
            var trackRect = new Rectangle(16 + labelWidth, y + 6, barAreaWidth, Math.Max(10, rowHeight - 14));
            g.FillRectangle(trackBrush, trackRect);

            if (barWidth > 0)
            {
                using var barBrush = new SolidBrush(barColor);
                var fillRect = new Rectangle(trackRect.X, trackRect.Y, Math.Max(4, barWidth), trackRect.Height);
                g.FillRectangle(barBrush, fillRect);
            }

            g.DrawString(item.DisplayValue ?? item.Value.ToString("N0"), AppTheme.SmallFont, titleBrush,
                new RectangleF(16 + labelWidth + barAreaWidth + 8, y + 4, valueWidth - 8, rowHeight), ellipsisRight);
        }
    }
}
