using AntdUI;
using Logistics.Client.WinForms.Themes;
using Label = System.Windows.Forms.Label;

namespace Logistics.Client.WinForms.Controls.Common;

/// <summary>
/// Standardized AntdUI module page header displaying title, description, and right-aligned action buttons.
/// </summary>
public sealed class AntPageHeaderControl : AntdUI.Panel, IThemeAware
{
    private readonly Label _titleLabel;
    private readonly Label _subtitleLabel;
    private readonly FlowLayoutPanel _actionsPanel;

    public AntPageHeaderControl(string title, string? subtitle = null)
    {
        Dock = DockStyle.Top;
        Height = 68;
        Radius = 8;
        BorderWidth = 1F;
        Margin = new Padding(0, 0, 0, 10);
        Padding = new Padding(16, 10, 16, 10);

        var rootLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var titleStack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
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
            Font = AppTheme.PageTitleFont,
            Dock = DockStyle.Top,
            Height = 28,
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
            MinimumSize = new Size(80, 42),
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 4, 0, 0)
        };

        rootLayout.Controls.Add(titleStack, 0, 0);
        rootLayout.Controls.Add(_actionsPanel, 1, 0);
        Controls.Add(rootLayout);
        ApplyTheme();
    }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public string Title
    {
        get => _titleLabel.Text;
        set => _titleLabel.Text = value;
    }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public string Subtitle
    {
        get => _subtitleLabel.Text;
        set
        {
            _subtitleLabel.Text = value;
            _subtitleLabel.Visible = !string.IsNullOrWhiteSpace(value);
        }
    }

    public void AddAction(Control button)
    {
        _actionsPanel.Controls.Add(button);
    }

    public void ClearActions() => _actionsPanel.Controls.Clear();

    public void ApplyTheme()
    {
        Back = AppTheme.CardBg;
        BorderColor = AppTheme.BorderSubtle;
        _titleLabel.ForeColor = AppTheme.TextPrimary;
        _subtitleLabel.ForeColor = AppTheme.TextSecondary;
        Invalidate();
    }
}
