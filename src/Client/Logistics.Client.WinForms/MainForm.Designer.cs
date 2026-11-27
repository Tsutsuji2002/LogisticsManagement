#nullable enable
namespace Logistics.Client.WinForms;

partial class MainForm
{
    private System.ComponentModel.IContainer? components = null;

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        SuspendLayout();
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1440, 880);
        MinimumSize = new Size(1024, 660);
        MaximumSize = new Size(1920, 1200);
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Logistics Management System — Enterprise Desktop";
        ResumeLayout(false);
    }
}
