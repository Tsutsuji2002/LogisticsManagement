using Logistics.Client.WinForms.Themes;

namespace Logistics.Client.WinForms.Controls.Common;

public sealed class KpiCardControl : Panel
{
    private readonly Panel _accentBar;
    private readonly Label _titleLabel;
    private readonly Label _valueLabel;
    private readonly Label _subtitleLabel;

    public KpiCardControl(string title, string value, string subtitle, Color accentColor)
    {
        BackColor = AppTheme.CardBg;
        Height = 104;
        MinimumSize = new Size(180, 96);
        Margin = new Padding(0, 0, 12, 12);
        Padding = new Padding(16, 12, 14, 10);

        _accentBar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 5,
            BackColor = accentColor
        };

        _titleLabel = new Label
        {
            Text = title,
            Font = AppTheme.SmallFont,
            ForeColor = AppTheme.TextMuted,
            AutoSize = true,
            Location = new Point(16, 12)
        };

        _valueLabel = new Label
        {
            Text = value,
            Font = AppTheme.KpiValueFont,
            ForeColor = AppTheme.TextPrimary,
            AutoSize = true,
            Location = new Point(14, 34)
        };

        _subtitleLabel = new Label
        {
            Text = subtitle,
            Font = AppTheme.SmallFont,
            ForeColor = AppTheme.TextSecondary,
            AutoSize = true,
            Location = new Point(16, 74)
        };

        Controls.Add(_accentBar);
        Controls.Add(_titleLabel);
        Controls.Add(_valueLabel);
        Controls.Add(_subtitleLabel);

        Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.BorderSubtle);
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        };
    }

    public void UpdateMetric(string value, string? subtitle = null)
    {
        _valueLabel.Text = value;
        if (subtitle != null) _subtitleLabel.Text = subtitle;
    }
}

public sealed record BarChartDatum(string Label, decimal Value, string? DisplayValue = null, Color? Color = null);

public sealed class SimpleBarChartControl : Panel
{
    private string _title = string.Empty;
    private IReadOnlyList<BarChartDatum> _data = [];

    public SimpleBarChartControl(string title = "")
    {
        _title = title;
        BackColor = AppTheme.CardBg;
        DoubleBuffered = true;
        MinimumSize = new Size(260, 220);
        Margin = new Padding(0, 0, 12, 12);
    }

    public void SetData(string title, IReadOnlyList<BarChartDatum> data)
    {
        _title = title;
        _data = data;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        using var borderPen = new Pen(AppTheme.BorderSubtle);
        g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);

        using var titleBrush = new SolidBrush(AppTheme.TextPrimary);
        g.DrawString(_title, AppTheme.SectionTitleFont, titleBrush, 14, 12);

        if (_data.Count == 0 || _data.All(d => d.Value <= 0))
        {
            using var muted = new SolidBrush(AppTheme.TextMuted);
            g.DrawString("Không có dữ liệu biểu đồ trong phạm vi lọc.", AppTheme.BodyFont, muted, 16, 54);
            return;
        }

        var maxVal = _data.Max(d => d.Value);
        if (maxVal <= 0) maxVal = 1;

        var top = 46;
        var rowHeight = Math.Max(22, Math.Min(32, (Height - 60) / _data.Count));
        var labelWidth = Math.Min(135, Width / 3);
        var valueColWidth = 75;
        var barAreaWidth = Math.Max(40, Width - labelWidth - valueColWidth - 36);

        for (int i = 0; i < _data.Count && top + rowHeight <= Height - 8; i++)
        {
            var item = _data[i];
            var ratio = (float)(item.Value / maxVal);
            var barW = Math.Max(4, (int)(barAreaWidth * ratio));
            var barColor = item.Color ?? AppTheme.PrimaryBlue;

            using var labelBrush = new SolidBrush(AppTheme.TextSecondary);
            var labelRect = new RectangleF(14, top + 2, labelWidth - 6, rowHeight - 4);
            g.DrawString(item.Label, AppTheme.SmallFont, labelBrush, labelRect);

            using var trackBrush = new SolidBrush(AppTheme.RowAltBg);
            g.FillRectangle(trackBrush, labelWidth + 14, top + 4, barAreaWidth, rowHeight - 10);

            using var barBrush = new SolidBrush(barColor);
            g.FillRectangle(barBrush, labelWidth + 14, top + 4, barW, rowHeight - 10);

            var valText = item.DisplayValue ?? item.Value.ToString("N0");
            g.DrawString(valText, AppTheme.SmallFont, titleBrush, labelWidth + barAreaWidth + 20, top + 2);

            top += rowHeight;
        }
    }
}
