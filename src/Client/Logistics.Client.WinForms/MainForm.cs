using Logistics.Client.Application.Interfaces;
using Logistics.Client.Application.Presenters;
using Logistics.Client.WinForms.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace Logistics.Client.WinForms;

/// <summary>
/// Primary desktop application shell hosting sidebar navigation, header, status bar, and modular views.
/// </summary>
public partial class MainForm : Form
{
    private readonly INavigationService _navigationService;
    private readonly IServiceProvider _serviceProvider;

    private Panel _headerPanel = null!;
    private Panel _sidebarPanel = null!;
    private Panel _contentPanel = null!;
    private StatusStrip _statusStrip = null!;
    private ToolStripStatusLabel _statusLabel = null!;
    private Label _userLabel = null!;

    private readonly Dictionary<string, UserControl> _moduleControls = new(StringComparer.OrdinalIgnoreCase);
    private UserControl? _currentControl;
    private Button? _activeNavButton;

    public MainForm(INavigationService navigationService, IServiceProvider serviceProvider)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        _navigationService.NavigationRequested += OnNavigationRequested;

        InitializeComponent();
        BuildLayout();
        RegisterNavigationItems();

        // Default screen is Dashboard
        NavigateToModule("Dashboard");
    }

    private void BuildLayout()
    {
        BackColor = Color.FromArgb(245, 247, 250);

        // --- Status Bar (Dock: Bottom) ---
        _statusStrip = new StatusStrip
        {
            BackColor = Color.FromArgb(15, 23, 42),
            SizingGrip = true
        };
        _statusLabel = new ToolStripStatusLabel
        {
            Text = "Ready",
            ForeColor = Color.FromArgb(226, 232, 240),
            Spring = true,
            TextAlign = ContentAlignment.MiddleLeft
        };
        var envLabel = new ToolStripStatusLabel
        {
            Text = "Environment: Development | .NET 9.0",
            ForeColor = Color.FromArgb(148, 163, 184)
        };
        _statusStrip.Items.AddRange([_statusLabel, envLabel]);

        // --- Top Header (Dock: Top) ---
        _headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = Color.FromArgb(15, 23, 42),
            Padding = new Padding(20, 0, 20, 0)
        };

        var appTitle = new Label
        {
            Text = "LOGISTICS MANAGEMENT SYSTEM",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(20, 18)
        };
        _headerPanel.Controls.Add(appTitle);

        _userLabel = new Label
        {
            Text = "Operator: System Administrator (Mock Session)",
            ForeColor = Color.FromArgb(148, 163, 184),
            Font = new Font("Segoe UI", 9F),
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _headerPanel.Controls.Add(_userLabel);
        _userLabel.Location = new Point(_headerPanel.Width - _userLabel.PreferredWidth - 20, 20);
        _headerPanel.Resize += (s, e) =>
        {
            _userLabel.Location = new Point(_headerPanel.Width - _userLabel.PreferredWidth - 20, 20);
        };

        // --- Left Sidebar (Dock: Left) ---
        _sidebarPanel = new Panel
        {
            Dock = DockStyle.Left,
            Width = 230,
            BackColor = Color.FromArgb(30, 41, 59),
            AutoScroll = true,
            Padding = new Padding(0, 12, 0, 12)
        };

        // --- Main Content Area (Dock: Fill) ---
        _contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(245, 247, 250)
        };

        // Order of addition controls docking order in WinForms
        Controls.Add(_contentPanel);
        Controls.Add(_sidebarPanel);
        Controls.Add(_headerPanel);
        Controls.Add(_statusStrip);
    }

    private void RegisterNavigationItems()
    {
        var modules = new (string Icon, string Name)[]
        {
            ("[D]", "Dashboard"),
            ("[C]", "Customers"),
            ("[O]", "Orders"),
            ("[S]", "Shipments"),
            ("[W]", "Warehouse"),
            ("[I]", "Inventory"),
            ("[F]", "Finance"),
            ("[R]", "Reports"),
            ("[*]", "Settings")
        };

        int topOffset = 8;
        foreach (var (icon, name) in modules)
        {
            var navButton = new Button
            {
                Text = $"  {icon}  {name}",
                TextAlign = ContentAlignment.MiddleLeft,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(230, 44),
                Location = new Point(0, topOffset),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(203, 213, 225),
                BackColor = Color.FromArgb(30, 41, 59),
                Cursor = Cursors.Hand,
                Tag = name,
                Padding = new Padding(16, 0, 0, 0)
            };
            navButton.FlatAppearance.BorderSize = 0;
            navButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(51, 65, 85);
            navButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(37, 99, 235);

            navButton.Click += (s, e) => _navigationService.NavigateTo(name);

            _sidebarPanel.Controls.Add(navButton);
            topOffset += 44;
        }
    }

    private void NavigateToModule(string moduleName)
    {
        if (!_moduleControls.TryGetValue(moduleName, out var control))
        {
            control = CreateModuleControl(moduleName);
            _moduleControls[moduleName] = control;
        }

        if (_currentControl != null)
        {
            _contentPanel.Controls.Remove(_currentControl);
        }

        control.Dock = DockStyle.Fill;
        _contentPanel.Controls.Add(control);
        _currentControl = control;

        HighlightActiveButton(moduleName);
        _statusLabel.Text = $"Active View: {moduleName}";
    }

    private UserControl CreateModuleControl(string moduleName)
    {
        if (string.Equals(moduleName, "Dashboard", StringComparison.OrdinalIgnoreCase))
        {
            var apiClient = _serviceProvider.GetService<IApiClient>();
            return new DashboardControl(apiClient);
        }

        return new PlaceholderModuleControl(moduleName);
    }

    private void HighlightActiveButton(string selectedModuleName)
    {
        foreach (Control control in _sidebarPanel.Controls)
        {
            if (control is Button btn)
            {
                bool isSelected = string.Equals(btn.Tag as string, selectedModuleName, StringComparison.OrdinalIgnoreCase);
                if (isSelected)
                {
                    btn.BackColor = Color.FromArgb(37, 99, 235);
                    btn.ForeColor = Color.White;
                    btn.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                    _activeNavButton = btn;
                }
                else
                {
                    btn.BackColor = Color.FromArgb(30, 41, 59);
                    btn.ForeColor = Color.FromArgb(203, 213, 225);
                    btn.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
                }
            }
        }
    }

    private void OnNavigationRequested(object? sender, string moduleName)
    {
        if (InvokeRequired)
        {
            Invoke(() => NavigateToModule(moduleName));
        }
        else
        {
            NavigateToModule(moduleName);
        }
    }
}
