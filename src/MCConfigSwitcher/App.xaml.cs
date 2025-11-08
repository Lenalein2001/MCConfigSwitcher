using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using MCConfigSwitcher.ViewModels;

namespace MCConfigSwitcher
{
    public partial class App : Application
    {
        private TaskbarIcon? _tray;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // Create single main window (avoid double window instantiation)
            var window = Current.MainWindow as MainWindow ?? new MainWindow();
            if (Current.MainWindow == null) Current.MainWindow = window;
            window.Show();

            // Initialize tray icon if resource and icon exist
            try
            {
                _tray = (TaskbarIcon?)Resources["AppTrayIcon"];
                if (_tray != null)
                {
                    var vm = window.DataContext as MainViewModel;
                    BuildTrayMenu(window, vm);
                    _tray.Visibility = System.Windows.Visibility.Visible;

                    if (vm != null)
                    {
                        // Rebuild tray menu when profiles change or selection changes
                        vm.PropertyChanged += (_, __) => BuildTrayMenu(window, vm);
                        vm.Profiles.CollectionChanged += (_, __) => BuildTrayMenu(window, vm);
                    }
                }
            }
            catch
            {
                // ignore tray failures (e.g., missing icon), app still usable
            }
        }

        private void BuildTrayMenu(MainWindow window, MainViewModel? vm)
        {
            if (_tray == null) return;
            var menu = new ContextMenu();

            var openItem = new MenuItem { Header = "Open" };
            openItem.Click += (_, __) => {
                if (window.WindowState == WindowState.Minimized) window.WindowState = WindowState.Normal;
                window.Show();
                window.Activate();
            };
            menu.Items.Add(openItem);
            menu.Items.Add(new Separator());

            if (vm != null)
            {
                var applyHeader = new MenuItem { Header = "Apply Profile" };
                foreach (var p in vm.Profiles)
                {
                    var mi = new MenuItem { Header = p.Name };
                    mi.Click += (_, __) => { vm.SelectedProfile = p; if (vm.ApplyCommand.CanExecute(null)) vm.ApplyCommand.Execute(null); };
                    applyHeader.Items.Add(mi);
                }
                if (applyHeader.Items.Count == 0)
                {
                    var emptyMi = new MenuItem { Header = "(no profiles)", IsEnabled = false };
                    applyHeader.Items.Add(emptyMi);
                }
                menu.Items.Add(applyHeader);
            }

            menu.Items.Add(new Separator());
            var exitItem = new MenuItem { Header = "Exit" };
            exitItem.Click += (_, __) => Current.Shutdown();
            menu.Items.Add(exitItem);

            _tray.ContextMenu = menu;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _tray?.Dispose();
            base.OnExit(e);
        }
    }
}
