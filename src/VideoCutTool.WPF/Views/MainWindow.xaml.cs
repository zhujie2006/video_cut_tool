using System.Windows;
using System.Windows.Input;
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
        
        /// <summary>
        /// 标题栏拖拽事件
        /// </summary>
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }
        
        /// <summary>
        /// 最小化按钮点击事件
        /// </summary>
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
        
        /// <summary>
        /// 关闭按钮点击事件
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}