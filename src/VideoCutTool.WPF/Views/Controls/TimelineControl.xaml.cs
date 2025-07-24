using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VideoCutTool.Core.Models;
using VideoCutTool.Core.Interfaces;
using VideoCutTool.WPF.ViewModels;
using Path = System.IO.Path;

namespace VideoCutTool.WPF.Views.Controls
{
    public partial class TimelineControl : UserControl
    {
        private readonly ILogger<TimelineControl> _logger;
        private TimelineControlViewModel _viewModel;
        
        // UI元素集合
        private List<Image> _thumbnails = new();
        private List<Rectangle> _waveformBars = new();
        private List<Line> _splitPoints = new();
        
        // 播放头拖拽相关
        private bool _isDraggingPlayhead = false;
        private Point _dragStartPoint;
        private double _dragStartX;
        
        // 公共属性，用于外部检查拖拽状态
        public bool IsDraggingPlayhead => _isDraggingPlayhead;
        
        // 事件
        public event Action<TimeSpan>? PlayheadPositionChanged;
        public event Action<TimeSpan>? SplitPointRequested;
        
        public static readonly DependencyProperty ZoomLevelProperty =
            DependencyProperty.Register("ZoomLevel", typeof(double), typeof(TimelineControl), 
                new PropertyMetadata(1.0, OnZoomLevelChanged));
        
        public double ZoomLevel
        {
            get => (double)GetValue(ZoomLevelProperty);
            set => SetValue(ZoomLevelProperty, value);
        }
        
        public TimelineControl()
        {
            InitializeComponent();
            
            _logger = ((App)Application.Current).Services.GetRequiredService<ILogger<TimelineControl>>();
            _viewModel = ((App)Application.Current).Services.GetRequiredService<TimelineControlViewModel>();
            
            _logger.LogInformation("TimelineControl 初始化开始");
            
            // 初始化事件处理
            TimelineScrollViewer.ScrollChanged += TimelineScrollViewer_ScrollChanged;
            
            // 绑定ViewModel事件
            _viewModel.PlayheadPositionChanged += OnViewModelPlayheadPositionChanged;
            _viewModel.SplitPointRequested += OnViewModelSplitPointRequested;
            _viewModel.TimelineContentChanged += OnViewModelTimelineContentChanged;
            
            // 设置初始缩放
            ZoomSlider.Value = _viewModel.ZoomLevel;
            
            _logger.LogInformation("TimelineControl 初始化完成");
        }
        
        private static void OnZoomLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TimelineControl control)
            {
                control._logger.LogInformation($"ZoomLevel属性改变: {e.OldValue} -> {e.NewValue}");
                control._viewModel.ZoomLevel = (double)e.NewValue;
            }
        }
        
        // ViewModel事件处理
        private void OnViewModelPlayheadPositionChanged(TimeSpan time)
        {
            _logger.LogDebug($"ViewModel播放头位置改变: {time:mm\\:ss\\.f}");
            PlayheadPositionChanged?.Invoke(time);
        }
        
        private void OnViewModelSplitPointRequested(TimeSpan time)
        {
            _logger.LogInformation($"ViewModel请求切分点: {time:mm\\:ss\\.f}");
            SplitPointRequested?.Invoke(time);
        }
        
        private async void OnViewModelTimelineContentChanged()
        {
            _logger.LogInformation("ViewModel时间轴内容改变，开始重新生成UI");
            await RegenerateTimelineContent();
        }
        
        public async Task LoadVideo(VideoInfo videoInfo)
        {
            _logger.LogInformation($"开始加载视频: {videoInfo.FilePath}");
            
            try
            {
                // 设置ViewModel的当前视频
                _viewModel.CurrentVideo = videoInfo;
                
                // 更新Grid宽度
                TimelineGrid.Width = _viewModel.TotalWidth;
                
                _logger.LogInformation($"视频加载完成，时间轴宽度: {_viewModel.TotalWidth}px");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载视频时发生错误");
            }
        }
        
        private async Task RegenerateTimelineContent()
        {
            _logger.LogInformation("开始重新生成时间轴内容");
            
            try
            {
                // 更新Grid宽度
                TimelineGrid.Width = _viewModel.TotalWidth;
                
                // 生成时间标尺
                GenerateTimeRuler();
                
                // 生成缩略图
                await GenerateThumbnails();
                
                // 生成音频波形
                await GenerateAudioWaveform();
                
                // 清除切分点
                ClearSplitPoints();
                
                _logger.LogInformation("时间轴内容重新生成完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重新生成时间轴内容时发生错误");
            }
        }
        
        private void GenerateTimeRuler()
        {
            _logger.LogDebug("开始生成时间标尺");
            
            RulerCanvas.Children.Clear();
            
            if (_viewModel.CurrentVideo == null)
            {
                _logger.LogWarning("生成时间标尺时CurrentVideo为null");
                return;
            }
            
            var duration = _viewModel.CurrentVideo.Duration.TotalSeconds;
            var interval = GetTimeInterval(duration);
            var pixelsPerSecond = _viewModel.PixelsPerSecond;
            
            _logger.LogDebug($"时间标尺参数: 时长={duration}s, 间隔={interval}s, 像素/秒={pixelsPerSecond}");
            
            for (double time = 0; time <= duration; time += interval)
            {
                var x = time * pixelsPerSecond;
                
                // 绘制刻度线
                var tickLine = new Line
                {
                    X1 = x,
                    Y1 = 20,
                    X2 = x,
                    Y2 = 30,
                    Stroke = Brushes.White,
                    StrokeThickness = 1
                };
                RulerCanvas.Children.Add(tickLine);
                
                // 绘制时间标签
                var timeText = new TextBlock
                {
                    Text = TimeSpan.FromSeconds(time).ToString(@"mm\:ss"),
                    Foreground = Brushes.White,
                    FontSize = 10,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                
                Canvas.SetLeft(timeText, x - 15);
                Canvas.SetTop(timeText, 5);
                RulerCanvas.Children.Add(timeText);
            }
            
            _logger.LogDebug($"时间标尺生成完成，共{Math.Ceiling(duration / interval)}个刻度");
        }
        
        private double GetTimeInterval(double duration)
        {
            // 根据缩放级别和视频时长调整时间间隔
            if (_viewModel.ZoomLevel >= 200) return 5; // 5秒间隔
            if (_viewModel.ZoomLevel >= 100) return 10; // 10秒间隔
            if (_viewModel.ZoomLevel >= 50) return 30; // 30秒间隔
            return 60; // 60秒间隔
        }
        
        private async Task GenerateThumbnails()
        {
            _logger.LogInformation("开始生成缩略图");
            
            VideoTrackCanvas.Children.Clear();
            _thumbnails.Clear();
            
            if (_viewModel.CurrentVideo == null)
            {
                _logger.LogWarning("生成缩略图时CurrentVideo为null");
                return;
            }
            
            
            var duration = _viewModel.CurrentVideo.Duration.TotalSeconds;
            var thumbnailWidth = TimelineControlViewModel.THUMBNAIL_WIDTH; // 固定缩略图宽度
            var thumbnailHeight = (TimelineControlViewModel.THUMBNAIL_WIDTH / _viewModel.CurrentVideo.Width)  * _viewModel.CurrentVideo.Height;
            var thumbnailTimeInterval = _viewModel.ThumbnailTimeInterval;
            
            _logger.LogDebug($"缩略图参数: 时长={duration}s, 间隔={thumbnailTimeInterval:F2}s, 宽度={thumbnailWidth}px");
            
            // 生成缩略图，每张缩略图的右边框对应的时间就是下一张缩略图的生成时间
            double currentTime = 0;
            double currentX = 0;
            int thumbnailCount = 0;
            
            while (currentTime < duration)
            {
                try
                {
                    // 从ViewModel获取缩略图路径
                    var thumbnailPath = await _viewModel.GetThumbnailPathAsync(currentTime);
                    
                    if (!string.IsNullOrEmpty(thumbnailPath) && File.Exists(thumbnailPath))
                    {
                        var image = new Image
                        {
                            Source = new BitmapImage(new Uri(thumbnailPath)),
                            Width = thumbnailWidth,
                            Height = thumbnailHeight,
                            Stretch = Stretch.UniformToFill
                        };
                        
                        Canvas.SetLeft(image, currentX);
                        Canvas.SetTop(image, 2);
                        
                        VideoTrackCanvas.Children.Add(image);
                        _thumbnails.Add(image);
                        thumbnailCount++;
                    }
                    else
                    {
                        // 创建占位符
                        var placeholder = new Border
                        {
                            Background = Brushes.Gray,
                            Width = thumbnailWidth,
                            Height = thumbnailHeight,
                            Child = new TextBlock
                            {
                                Text = $"{currentTime:F1}s",
                                Foreground = Brushes.White,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                FontSize = 8
                            }
                        };
                        
                        Canvas.SetLeft(placeholder, currentX);
                        Canvas.SetTop(placeholder, 2);
                        
                        VideoTrackCanvas.Children.Add(placeholder);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"生成缩略图失败: {currentTime:F1}s");
                    
                    // 创建错误占位符
                    var placeholder = new Border
                    {
                        Background = Brushes.Red,
                        Width = thumbnailWidth,
                        Height = thumbnailHeight,
                        Child = new TextBlock
                        {
                            Text = $"错误",
                            Foreground = Brushes.White,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            FontSize = 8
                        }
                    };
                    
                    Canvas.SetLeft(placeholder, currentX);
                    Canvas.SetTop(placeholder, 2);
                    
                    VideoTrackCanvas.Children.Add(placeholder);
                }
                
                // 移动到下一个位置
                currentX += thumbnailWidth;
                currentTime += thumbnailTimeInterval;
            }
            
            _logger.LogInformation($"缩略图生成完成，共{thumbnailCount}个有效缩略图");
        }
        

        
        private async Task GenerateAudioWaveform()
        {
            _logger.LogInformation("开始生成音频波形");
            
            AudioTrackCanvas.Children.Clear();
            _waveformBars.Clear();
            
            if (_viewModel.CurrentVideo == null)
            {
                _logger.LogWarning("生成音频波形时CurrentVideo为null");
                return;
            }
            
            try
            {
                // 生成音频波形数据
                var videoService = ((App)Application.Current).Services.GetRequiredService<IVideoService>();
                var waveformData = await videoService.GenerateAudioWaveformAsync(_viewModel.CurrentVideo.FilePath);
                
                var pixelsPerSecond = _viewModel.PixelsPerSecond;
                var barWidth = Math.Max(1, pixelsPerSecond / 10); // 每0.1秒一个波形条
                
                _logger.LogDebug($"音频波形参数: 像素/秒={pixelsPerSecond}, 条宽度={barWidth}px, 数据点={waveformData.Count}");
                
                // 获取AudioTrackCanvas的实际高度
                var canvasHeight = AudioTrackCanvas.ActualHeight;
                if (canvasHeight <= 0)
                {
                    canvasHeight = 40; // 如果还没有渲染，使用默认高度
                }
                
                _logger.LogDebug($"AudioTrackCanvas高度: {canvasHeight}px");
                
                for (int i = 0; i < waveformData.Count; i++)
                {
                    var amplitude = waveformData[i];
                    var x = i * barWidth;
                    var maxBarHeight = canvasHeight * 1.0; // 最大高度为Canvas高度的80%
                    var height = Math.Max(2, amplitude * maxBarHeight); // 将振幅转换为高度
                    
                    var bar = new Rectangle
                    {
                        Fill = new SolidColorBrush(Color.FromRgb(0, 125, 125)), // 青色
                        Width = ((barWidth - 1) < 2) ? 2 : (barWidth - 1),
                        Height = height,
                        RadiusX = 1,
                        RadiusY = 1
                    };
                    
                    Canvas.SetLeft(bar, x);
                    Canvas.SetTop(bar, canvasHeight - height); // 在Canvas中垂直居中
                    
                    AudioTrackCanvas.Children.Add(bar);
                    _waveformBars.Add(bar);
                }
                
                _logger.LogInformation($"音频波形生成完成，共{waveformData.Count}个波形条");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成音频波形失败，使用模拟波形");
                
                // 如果波形生成失败，创建模拟波形
                var duration = _viewModel.CurrentVideo.Duration.TotalSeconds;
                var pixelsPerSecond = _viewModel.PixelsPerSecond;
                var barWidth = Math.Max(1, pixelsPerSecond / 10);
                
                // 获取AudioTrackCanvas的实际高度
                var canvasHeight = AudioTrackCanvas.ActualHeight;
                if (canvasHeight <= 0)
                {
                    canvasHeight = 40; // 如果还没有渲染，使用默认高度
                }
                
                _logger.LogDebug($"模拟波形 - AudioTrackCanvas高度: {canvasHeight}px");
                
                var random = new Random(42); // 固定种子以获得一致的模拟波形
                
                for (double time = 0; time < duration; time += 0.1)
                {
                    var amplitude = random.NextDouble() * 0.8 + 0.2; // 0.2-1.0之间的随机值
                    var x = time * pixelsPerSecond;
                    var maxBarHeight = canvasHeight * 0.8; // 最大高度为Canvas高度的80%
                    var height = amplitude * maxBarHeight;
                    
                    var bar = new Rectangle
                    {
                        Fill = new SolidColorBrush(Color.FromRgb(150, 0, 0)),
                        Width = barWidth - 1,
                        Height = height,
                        RadiusX = 1,
                        RadiusY = 1
                    };
                    
                    Canvas.SetLeft(bar, x);
                    Canvas.SetTop(bar, (canvasHeight - height) / 2); // 在Canvas中垂直居中
                    
                    AudioTrackCanvas.Children.Add(bar);
                    _waveformBars.Add(bar);
                }
                
                _logger.LogInformation($"模拟音频波形生成完成");
            }
        }
        
        public void UpdatePlayhead(TimeSpan currentTime)
        {
            if (_viewModel.CurrentVideo == null)
            {
                _logger.LogWarning("更新播放头时CurrentVideo为null");
                return;
            }
            
            _logger.LogDebug($"更新播放头位置: {currentTime:mm\\:ss\\.f}, 拖拽状态: {_isDraggingPlayhead}");
            
            // 如果正在拖拽，不更新播放头位置
            if (_isDraggingPlayhead)
            {
                _logger.LogDebug("正在拖拽中，跳过播放头位置更新");
                return;
            }
            
            var pixelsPerSecond = _viewModel.PixelsPerSecond;
            var x = currentTime.TotalSeconds * pixelsPerSecond;
            
            _logger.LogDebug($"播放头位置计算 - 像素/秒: {pixelsPerSecond:F2}, X坐标: {x:F2}");
            
            // 使用Dispatcher确保在UI线程上更新
            Dispatcher.Invoke(() =>
            {
                PlayheadLine.X1 = x;
                PlayheadLine.X2 = x;
                
                // 更新播放头块的位置
                Canvas.SetLeft(PlayheadTopBlock, x - 6);
            });
            
            // 确保播放头在可视区域内
            if (x > TimelineScrollViewer.HorizontalOffset + TimelineScrollViewer.ViewportWidth - 50)
            {
                TimelineScrollViewer.ScrollToHorizontalOffset(x - TimelineScrollViewer.ViewportWidth + 100);
                _logger.LogDebug("播放头超出可视区域，已自动滚动");
            }
        }
        
        // 播放头拖拽事件处理
        private void Playhead_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _logger.LogInformation($"播放头鼠标按下事件触发 - 位置: {e.GetPosition(PlayheadCanvas)}, 按钮: {e.ChangedButton}");
            
            _isDraggingPlayhead = true;
            _dragStartPoint = e.GetPosition(PlayheadCanvas);
            _dragStartX = PlayheadLine.X1;
            
            _logger.LogInformation($"开始拖拽 - 起始点: {_dragStartPoint}, 起始X: {_dragStartX}, 当前视频: {_viewModel.CurrentVideo?.FilePath ?? "null"}");
            
            PlayheadCanvas.CaptureMouse();
            e.Handled = true;
            
            _logger.LogInformation("播放头拖拽开始，鼠标已捕获");
        }
        
        private void Playhead_MouseMove(object sender, MouseEventArgs e)
        {
            _logger.LogDebug($"播放头鼠标移动事件触发 - 拖拽状态: {_isDraggingPlayhead}, 当前视频: {_viewModel.CurrentVideo?.FilePath ?? "null"}");
            
            if (_isDraggingPlayhead && _viewModel.CurrentVideo != null)
            {
                var currentPoint = e.GetPosition(PlayheadCanvas);
                var deltaX = currentPoint.X - _dragStartPoint.X;
                var newX = _dragStartX + deltaX;
                
                // 限制在时间轴范围内
                var maxX = _viewModel.CurrentVideo.Duration.TotalSeconds * _viewModel.PixelsPerSecond;
                newX = Math.Max(0, Math.Min(maxX, newX));
                
                _logger.LogDebug($"拖拽计算 - 当前点: {currentPoint}, 偏移: {deltaX:F2}, 新X: {newX:F2}, 最大X: {maxX:F2}");
                
                // 更新播放头位置
                PlayheadLine.X1 = newX;
                PlayheadLine.X2 = newX;
                Canvas.SetLeft(PlayheadTopBlock, newX - 6);
                
                // 计算对应的时间
                var pixelsPerSecond = _viewModel.PixelsPerSecond;
                var time = TimeSpan.FromSeconds(newX / pixelsPerSecond);
                
                _logger.LogDebug($"播放头位置更新 - 像素/秒: {pixelsPerSecond:F2}, 时间: {time:mm\\:ss\\.f}");
                
                // 通过ViewModel更新
                _viewModel.UpdatePlayheadPosition(time);
            }
            else
            {
                _logger.LogDebug($"拖拽条件不满足 - 拖拽状态: {_isDraggingPlayhead}, 视频存在: {_viewModel.CurrentVideo != null}");
            }
        }
        
        private void Playhead_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _logger.LogInformation($"播放头鼠标释放事件触发 - 拖拽状态: {_isDraggingPlayhead}");
            
            if (_isDraggingPlayhead)
            {
                _isDraggingPlayhead = false;
                PlayheadCanvas.ReleaseMouseCapture();
                e.Handled = true;
                
                _logger.LogInformation("播放头拖拽结束，鼠标已释放");
            }
            else
            {
                _logger.LogDebug("播放头鼠标释放但未在拖拽状态");
            }
        }
        
        // 切分点功能
        public void AddSplitPoint(TimeSpan time)
        {
            if (_viewModel.CurrentVideo == null)
            {
                _logger.LogWarning("添加切分点时CurrentVideo为null");
                return;
            }
            
            _logger.LogInformation($"添加切分点: {time:mm\\:ss\\.f}");
            
            var pixelsPerSecond = _viewModel.PixelsPerSecond;
            var x = time.TotalSeconds * pixelsPerSecond;
            
            var splitLine = new Line
            {
                X1 = x,
                Y1 = 0,
                X2 = x,
                Y2 = 200,
                Stroke = Brushes.Red,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 5, 5 }
            };
            
            Canvas.SetLeft(splitLine, 0);
            //SplitPointsCanvas.Children.Add(splitLine);
            _splitPoints.Add(splitLine);
            
            // 通过ViewModel触发切分事件
            _viewModel.RequestSplitPoint(time);
        }
        
        public void ClearSplitPoints()
        {
            //SplitPointsCanvas.Children.Clear();
            _splitPoints.Clear();
        }
        
        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // 只在值真正改变且不是初始化时记录日志
            if (e.OldValue != e.NewValue && e.OldValue != 0)
            {
                _logger.LogDebug($"缩放滑块值改变: {e.OldValue} -> {e.NewValue}");
            }
            
            if (_viewModel != null)
            {
                _viewModel.ZoomLevel = e.NewValue;
            }
        }
        
        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            // 计算下一个整十倍缩放级别
            var currentZoom = ZoomSlider.Value;
            var nextZoom = currentZoom switch
            {
                1 => 10,
                10 => 20,
                20 => 30,
                30 => 40,
                40 => 50,
                50 => 60,
                60 => 70,
                70 => 80,
                80 => 90,
                90 => 100,
                _ => Math.Min(100, currentZoom + 10)
            };
            
            _logger.LogInformation($"放大按钮点击: {currentZoom} -> {nextZoom}");
            ZoomSlider.Value = nextZoom;
        }
        
        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            // 计算上一个整十倍缩放级别
            var currentZoom = ZoomSlider.Value;
            var prevZoom = currentZoom switch
            {
                100 => 90,
                90 => 80,
                80 => 70,
                70 => 60,
                60 => 50,
                50 => 40,
                40 => 30,
                30 => 20,
                20 => 10,
                10 => 1,
                _ => Math.Max(1, currentZoom - 10)
            };
            
            _logger.LogInformation($"缩小按钮点击: {currentZoom} -> {prevZoom}");
            ZoomSlider.Value = prevZoom;
        }
        
        private void FitToWindow_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.CurrentVideo == null)
            {
                _logger.LogWarning("适应窗口时CurrentVideo为null");
                return;
            }
            
            var availableWidth = TimelineScrollViewer.ActualWidth - 50;
            var duration = _viewModel.CurrentVideo.Duration.TotalSeconds;
            
            // 计算最优缩放倍数：可用宽度 / (时长 × 4.6)
            var optimalZoom = availableWidth / (duration * 4.6);
            
            // 找到最接近的整十倍缩放级别
            var zoomLevels = new[] { 1, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 };
            var bestZoom = zoomLevels.OrderBy(z => Math.Abs(z - optimalZoom)).First();
            
            _logger.LogInformation($"适应窗口: 可用宽度={availableWidth}px, 时长={duration}s, 最优缩放={optimalZoom:F1}, 选择缩放={bestZoom}");
            
            ZoomSlider.Value = bestZoom;
        }
        
        private void TimelineScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // 处理滚动事件
            if (e.HorizontalChange != 0)
            {
                _logger.LogDebug($"水平滚动: 变化={e.HorizontalChange:F1}, 新位置={e.HorizontalOffset:F1}, 最大滚动={TimelineScrollViewer.ScrollableWidth:F1}");
                
                // 计算当前显示的时间范围
                var pixelsPerSecond = _viewModel.PixelsPerSecond;
                var startTime = TimeSpan.FromSeconds(e.HorizontalOffset / pixelsPerSecond);
                var endTime = TimeSpan.FromSeconds((e.HorizontalOffset + TimelineScrollViewer.ViewportWidth) / pixelsPerSecond);
                
                _logger.LogDebug($"当前显示时间范围: {startTime:mm\\:ss} - {endTime:mm\\:ss}");
            }
        }
    }
} 