using Logistics.Client.Application.Interfaces;
using Logistics.Client.Application.Presenters;
using Logistics.Client.Application.Views;

namespace Logistics.Client.WinForms.Controls;

/// <summary>
/// Dashboard screen implementing <see cref="IDashboardView"/> following the MVP pattern.
/// </summary>
public class DashboardControl : UserControl, IDashboardView
{
    private Label _welcomeLabel = null!;
    private Label _statusLabel = null!;
    private Label _ordersCountLabel = null!;
    private Label _shipmentsCountLabel = null!;
    private Label _customersCountLabel = null!;
    private Label _warehousesCountLabel = null!;

    private readonly DashboardPresenter? _presenter;

    public DashboardControl() : this((IApiClient?)null)
    {
    }

    public DashboardControl(IApiClient? apiClient)
    {
        InitializeLayout();

        if (apiClient != null)
        {
            _presenter = new DashboardPresenter(this, apiClient);
            _ = _presenter.InitializeAsync();
        }
        else
        {
            SetWelcomeMessage("Welcome to Logistics Management System");
            SetStatusMessage("Operational - Ready");
            SetDashboardMetrics(0, 0, 0, 0);
        }
    }

    public DashboardControl(DashboardPresenter presenter)
    {
        _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
        InitializeLayout();
        _ = _presenter.InitializeAsync();
    }

    private void InitializeLayout()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(245, 247, 250);
        AutoScroll = true;

        var mainContainer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(24),
            AutoSize = true
        };
        mainContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // 1. Header Banner
        var headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 80,
            BackColor = Color.White,
            Margin = new Padding(0, 0, 0, 16),
            Padding = new Padding(20, 14, 20, 14)
        };
        headerPanel.Paint += DrawCardBorder;

        _welcomeLabel = new Label
        {
            Text = "Dashboard Overview",
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            ForeColor = Color.FromArgb(24, 48, 89),
            AutoSize = true,
            Location = new Point(16, 12)
        };
        headerPanel.Controls.Add(_welcomeLabel);

        _statusLabel = new Label
        {
            Text = "System Status: Online | Ready",
            Font = new Font("Segoe UI", 9.5F),
            ForeColor = Color.FromArgb(100, 116, 139),
            AutoSize = true,
            Location = new Point(18, 46)
        };
        headerPanel.Controls.Add(_statusLabel);
        mainContainer.Controls.Add(headerPanel, 0, 0);

        // 2. Metrics Cards Row
        var metricsPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 4,
            RowCount = 1,
            Height = 120,
            Margin = new Padding(0, 0, 0, 16)
        };
        metricsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        metricsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        metricsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        metricsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

        var (card1, count1) = CreateMetricCard("Orders", "0", "Total orders placed", Color.FromArgb(37, 99, 235));
        var (card2, count2) = CreateMetricCard("Shipments", "0", "Active in-transit", Color.FromArgb(16, 185, 129));
        var (card3, count3) = CreateMetricCard("Customers", "0", "Registered clients", Color.FromArgb(245, 158, 11));
        var (card4, count4) = CreateMetricCard("Warehouses", "0", "Operating facilities", Color.FromArgb(139, 92, 246));

        _ordersCountLabel = count1;
        _shipmentsCountLabel = count2;
        _customersCountLabel = count3;
        _warehousesCountLabel = count4;

        metricsPanel.Controls.Add(card1, 0, 0);
        metricsPanel.Controls.Add(card2, 1, 0);
        metricsPanel.Controls.Add(card3, 2, 0);
        metricsPanel.Controls.Add(card4, 3, 0);
        mainContainer.Controls.Add(metricsPanel, 0, 1);

        // 3. Information & Quick Actions Area
        var infoPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 220,
            BackColor = Color.White,
            Margin = new Padding(0, 0, 0, 16),
            Padding = new Padding(20)
        };
        infoPanel.Paint += DrawCardBorder;

        var sectionTitle = new Label
        {
            Text = "Architecture & Module Status",
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 41, 59),
            AutoSize = true,
            Location = new Point(20, 16)
        };
        infoPanel.Controls.Add(sectionTitle);

        var detailsText = new Label
        {
            Text = "• Primary Client: Windows Desktop (WinForms .NET 9)\n" +
                   "• Architecture: Clean Architecture + Modular Monolith\n" +
                   "• API Communication: HttpClientFactory via REST Endpoints (Non-blocking async)\n" +
                   "• Backend Integration: Ready for ASP.NET Core Web API, EF Core, SQL Server & Redis\n" +
                   "• Database Direct Access: Prohibited (Enforced by Architectural Boundary)",
            Font = new Font("Segoe UI", 9.5F),
            ForeColor = Color.FromArgb(71, 85, 105),
            AutoSize = true,
            Location = new Point(20, 52)
        };
        infoPanel.Controls.Add(detailsText);
        mainContainer.Controls.Add(infoPanel, 0, 2);

        Controls.Add(mainContainer);
    }

    private static (Panel Card, Label CountLabel) CreateMetricCard(string title, string value, string description, Color accentColor)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Margin = new Padding(4),
            Padding = new Padding(16, 12, 16, 12)
        };
        card.Paint += DrawCardBorder;

        var accentStrip = new Panel
        {
            Dock = DockStyle.Left,
            Width = 4,
            BackColor = accentColor
        };
        card.Controls.Add(accentStrip);

        var titleLabel = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 116, 139),
            AutoSize = true,
            Location = new Point(14, 12)
        };
        card.Controls.Add(titleLabel);

        var countLabel = new Label
        {
            Text = value,
            Font = new Font("Segoe UI", 20F, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            AutoSize = true,
            Location = new Point(12, 34)
        };
        card.Controls.Add(countLabel);

        var descLabel = new Label
        {
            Text = description,
            Font = new Font("Segoe UI", 8F),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize = true,
            Location = new Point(14, 76)
        };
        card.Controls.Add(descLabel);

        return (card, countLabel);
    }

    private static void DrawCardBorder(object? sender, PaintEventArgs e)
    {
        if (sender is Control control)
        {
            using var pen = new Pen(Color.FromArgb(226, 232, 240), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, control.Width - 1, control.Height - 1);
        }
    }

    public void SetWelcomeMessage(string message)
    {
        if (InvokeRequired)
        {
            Invoke(() => SetWelcomeMessage(message));
            return;
        }
        _welcomeLabel.Text = message;
    }

    public void SetStatusMessage(string status)
    {
        if (InvokeRequired)
        {
            Invoke(() => SetStatusMessage(status));
            return;
        }
        _statusLabel.Text = $"Status: {status}";
    }

    public void SetDashboardMetrics(int totalOrders, int activeShipments, int totalCustomers, int activeWarehouses)
    {
        if (InvokeRequired)
        {
            Invoke(() => SetDashboardMetrics(totalOrders, activeShipments, totalCustomers, activeWarehouses));
            return;
        }
        _ordersCountLabel.Text = totalOrders.ToString("N0");
        _shipmentsCountLabel.Text = activeShipments.ToString("N0");
        _customersCountLabel.Text = totalCustomers.ToString("N0");
        _warehousesCountLabel.Text = activeWarehouses.ToString("N0");
    }
}
