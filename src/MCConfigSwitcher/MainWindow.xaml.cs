using System.Windows;
using MCConfigSwitcher.ViewModels;

namespace MCConfigSwitcher
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}
