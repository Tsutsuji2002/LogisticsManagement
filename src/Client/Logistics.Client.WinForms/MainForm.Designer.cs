#nullable enable

namespace Logistics.Client.WinForms;

partial class MainForm
{
    private System.ComponentModel.IContainer? components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
            _navigationService.NavigationRequested -= OnNavigationRequested;
            foreach (var control in _moduleControls.Values)
            {
                control.Dispose();
            }
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1280, 800);
        MinimumSize = new Size(1024, 640);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Logistics Management System - Desktop Client";

        ResumeLayout(false);
    }
}
