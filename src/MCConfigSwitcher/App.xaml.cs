using System.Windows;
using MCConfigSwitcher.ViewModels;

namespace MCConfigSwitcher
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // Create single main window (avoid double window instantiation)
            var window = Current.MainWindow as MainWindow ?? new MainWindow();
            if (Current.MainWindow == null) Current.MainWindow = window;
            window.Show();
        }
        // No tray lifecycle; app exits when main window closes
    }
}
