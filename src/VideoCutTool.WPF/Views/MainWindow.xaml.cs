using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using VideoCutTool.WPF.ViewModels;

namespace VideoCutTool.WPF.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // 获取主窗口的ViewModel
            var serviceProvider = ((App)Application.Current).Services;
            DataContext = serviceProvider.GetRequiredService<MainWindowViewModel>();
        }
    }
}