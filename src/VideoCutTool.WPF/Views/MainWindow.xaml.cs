using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using VideoCutTool.WPF.ViewModels;

namespace VideoCutTool.WPF.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _positionTimer;
        
        public MainWindow()
        {
            InitializeComponent();
            
            // 初始化位置同步定时器
            _positionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _positionTimer.Tick += PositionTimer_Tick;
            
            // 获取主窗口的ViewModel
            var serviceProvider = ((App)Application.Current).Services;
            DataContext = serviceProvider.GetRequiredService<MainWindowViewModel>();
            
            // 订阅ViewModel的播放状态变化
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }
        
        private void PositionTimer_Tick(object? sender, EventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel && VideoPlayer.Source != null)
            {
                // 同步MediaElement的位置到ViewModel
                var oldTime = viewModel.CurrentTime;
                var newTime = VideoPlayer.Position;
                
                // 只有当时间真正改变时才更新，避免循环更新
                if (Math.Abs((newTime - oldTime).TotalMilliseconds) > 50) // 50ms的容差
                {
                    viewModel.CurrentTime = newTime;
                    System.Diagnostics.Debug.WriteLine($"时间同步: {oldTime} -> {newTime}");
                }
            }
        }
        
        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is MainWindowViewModel viewModel)
            {
                switch (e.PropertyName)
                {
                    case nameof(MainWindowViewModel.IsPlaying):
                        UpdateMediaElementPlayback(viewModel);
                        break;
                    case nameof(MainWindowViewModel.CurrentTime):
                        UpdateMediaElementPosition(viewModel);
                        break;
                }
            }
        }
        
        private void UpdateMediaElementPlayback(MainWindowViewModel viewModel)
        {
            if (VideoPlayer.Source != null)
            {
                if (viewModel.IsPlaying)
                {
                    VideoPlayer.Play();
                    _positionTimer.Start();
                }
                else
                {
                    VideoPlayer.Pause();
                    _positionTimer.Stop();
                }
            }
        }
        
        private void UpdateMediaElementPosition(MainWindowViewModel viewModel)
        {
            if (VideoPlayer.Source != null && !viewModel.IsPlaying)
            {
                // 只在非播放状态下更新位置，避免与播放冲突
                VideoPlayer.Position = viewModel.CurrentTime;
            }
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
        
        /// <summary>
        /// MediaElement媒体打开事件
        /// </summary>
        private void VideoPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                // 媒体打开后，可以获取实际的视频信息
                viewModel.StatusMessage = "视频加载完成";
                
                // 设置初始位置
                VideoPlayer.Position = viewModel.CurrentTime;
            }
        }
        
        /// <summary>
        /// MediaElement媒体结束事件
        /// </summary>
        private void VideoPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.IsPlaying = false;
                viewModel.PlayPauseIcon = "Play";
                viewModel.StatusMessage = "视频播放完成";
                _positionTimer.Stop();
            }
        }
    }
}