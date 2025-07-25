using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using VideoCutTool.WPF.ViewModels;
using VideoCutTool.Core.Models;
using System;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Windows.Media;

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
            var vm = serviceProvider.GetRequiredService<MainWindowViewModel>();
            vm.SettingViewModel = new SettingViewModel();
            var subVm = serviceProvider.GetRequiredService<TimelineControlViewModel>();

            subVm.UINotifier = vm;
            subVm.ControlNotifier = TimelineControl;
            vm.TimelineViewModel = subVm;

            DataContext = vm;
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
                // 检查TimelineControl是否正在拖拽
                if (TimelineControl.IsDraggingPlayhead)
                {
                    // 如果正在拖拽，暂停定时器更新
                    return;
                }
                
                // 同步MediaElement的位置到ViewModel
                var oldTime = viewModel.CurrentTime;
                var newTime = VideoPlayer.Position;
                
                // 只有当时间真正改变时才更新，避免循环更新
                if (Math.Abs((newTime - oldTime).TotalMilliseconds) > 50) // 50ms的容差
                {
                    viewModel.CurrentTime = newTime;
                    //System.Diagnostics.Debug.WriteLine($"时间同步: {oldTime} -> {newTime}");
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
                        // 更新时间轴播放头
                        TimelineControl.UpdatePlayhead(viewModel.CurrentTime);
                        break;
                    case nameof(MainWindowViewModel.VideoInfo):
                        // 当视频信息更新时，加载时间轴
                        if (viewModel.VideoInfo != null)
                        {
                            _ = LoadTimelineAsync(viewModel.VideoInfo);
                        }
                        break;
                }
            }
        }
        
        private async Task LoadTimelineAsync(VideoInfo videoInfo)
        {
            try
            {
                await TimelineControl.LoadVideo(videoInfo);
                
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载时间轴失败: {ex.Message}");
            }
        }
        
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                switch (e.Key)
                {
                    case Key.Space:
                        // Space键播放/暂停
                        if (viewModel.VideoInfo != null)
                        {
                            viewModel.PlayPauseCommand.Execute(null);
                            e.Handled = true;
                        }
                        break;
                    case Key.S:
                        // S键添加切分点
                        if (viewModel.TimelineViewModel != null)
                        {
                            viewModel.TimelineViewModel.SplitVideoCommand.Execute(null);
                            e.Handled = true;
                        }
                        break;
                    case Key.Delete:
                        // Delete键删除当前时间点的切分点
                        if (viewModel.TimelineViewModel != null)
                        {
                            viewModel.TimelineViewModel.DeleteSegmentCommand.Execute(null);
                            e.Handled = true;
                        }
                        break;
                    case Key.X:
                        // Ctrl+X清空所有切分点
                        if (Keyboard.Modifiers == ModifierKeys.Control)
                        {
                            if (viewModel.TimelineViewModel != null)
                            {
                                viewModel.TimelineViewModel.ClearSegmentCommand.Execute(null);
                                e.Handled = true;
                            }
                        }
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

        #region 时间轴高度调整

        private bool _isResizingTimeline = false;
        private double _startY;
        private double _startHeight;

        /// <summary>
        /// 时间轴分隔线鼠标按下事件
        /// </summary>
        private void TimelineSeparator_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isResizingTimeline = true;
                _startY = e.GetPosition(this).Y;
                _startHeight = TimelineRow.Height.Value;
                
                // 捕获鼠标，确保在窗口外也能接收到鼠标事件
                TimelineSeparator.CaptureMouse();
                e.Handled = true;
            }
        }

        /// <summary>
        /// 时间轴分隔线鼠标移动事件
        /// </summary>
        private void TimelineSeparator_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isResizingTimeline && e.LeftButton == MouseButtonState.Pressed)
            {
                var currentY = e.GetPosition(this).Y;
                var deltaY = currentY - _startY;
                var newHeight = _startHeight - deltaY; // 向上拖拽增加高度，向下拖拽减少高度
                
                // 限制在最小和最大高度范围内
                newHeight = Math.Max(TimelineRow.MinHeight, Math.Min(TimelineRow.MaxHeight, newHeight));
                
                TimelineRow.Height = new GridLength(newHeight);
                e.Handled = true;
            }
        }

        /// <summary>
        /// 时间轴分隔线鼠标释放事件
        /// </summary>
        private void TimelineSeparator_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isResizingTimeline)
            {
                _isResizingTimeline = false;
                TimelineSeparator.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        #endregion
    }
}