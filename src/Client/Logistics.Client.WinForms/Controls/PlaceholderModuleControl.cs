namespace Logistics.Client.WinForms.Controls;

/// <summary>
/// Reusable placeholder control rendered for future business modules.
/// </summary>
public class PlaceholderModuleControl : UserControl
{
    public PlaceholderModuleControl(string moduleName, string description = "")
    {
        InitializeLayout(moduleName, description);
    }

    private void InitializeLayout(string moduleName, string description)
    {
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(245, 247, 250);
        Padding = new Padding(24);

        var cardPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(32)
        };
        cardPanel.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(226, 232, 240), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, cardPanel.Width - 1, cardPanel.Height - 1);
        };

        var badge = new Label
        {
            Text = "MODULAR MONOLITH COMPONENT",
            Font = new Font("Segoe UI", 8F, FontStyle.Bold),
            ForeColor = Color.FromArgb(37, 99, 235),
            BackColor = Color.FromArgb(239, 246, 255),
            Padding = new Padding(8, 4, 8, 4),
            AutoSize = true,
            Location = new Point(32, 28)
        };
        cardPanel.Controls.Add(badge);

        var titleLabel = new Label
        {
            Text = $"{moduleName} Module",
            Font = new Font("Segoe UI", 18F, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            AutoSize = true,
            Location = new Point(30, 60)
        };
        cardPanel.Controls.Add(titleLabel);

        var descText = string.IsNullOrWhiteSpace(description)
            ? $"The {moduleName} module architecture is prepared and ready for business implementation.\n" +
              "It will consume backend endpoints via the client infrastructure layer without direct database access."
            : description;

        var descriptionLabel = new Label
        {
            Text = descText,
            Font = new Font("Segoe UI", 10F),
            ForeColor = Color.FromArgb(100, 116, 139),
            AutoSize = true,
            Location = new Point(32, 105)
        };
        cardPanel.Controls.Add(descriptionLabel);

        var infoBox = new GroupBox
        {
            Text = "Module Architecture Information",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 41, 59),
            Location = new Point(32, 170),
            Size = new Size(580, 140),
            Padding = new Padding(16)
        };

        var moduleDetails = new Label
        {
            Text = $"• Layer: Presentation (WinForms MVP UserControl)\n" +
                   $"• Application Contract: Logistics.Client.Application.{moduleName}\n" +
                   $"• Backend Controller: Logistics.Server.API.Controllers.{moduleName}Controller\n" +
                   $"• Communication: Async HTTP via IApiClient (No direct DB connection)",
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            ForeColor = Color.FromArgb(71, 85, 105),
            AutoSize = true,
            Location = new Point(16, 28)
        };
        infoBox.Controls.Add(moduleDetails);
        cardPanel.Controls.Add(infoBox);

        Controls.Add(cardPanel);
    }
}
