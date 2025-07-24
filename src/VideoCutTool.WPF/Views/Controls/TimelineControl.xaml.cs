using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Extensions.DependencyInjection;
using VideoCutTool.WPF.Models;
using VideoCutTool.WPF.Services;
using Path = System.IO.Path;

namespace VideoCutTool.WPF.Views.Controls
{
    public partial class TimelineControl : UserControl
    {
        private readonly IVideoService _videoService;
        private double _zoomLevel = 100.0;
        private double _pixelsPerSecond = 50.0; // 基础缩放比例
        private VideoInfo? _currentVideo;
        private List<Image> _thumbnails = new();
        private List<Rectangle> _waveformBars = new();
        
        public static readonly DependencyProperty ZoomLevelProperty =
            DependencyProperty.Register("ZoomLevel", typeof(double), typeof(TimelineControl), 
                new PropertyMetadata(100.0, OnZoomLevelChanged));
        
        public double ZoomLevel
        {
            get => (double)GetValue(ZoomLevelProperty);
            set => SetValue(ZoomLevelProperty, value);
        }
        
        public TimelineControl()
        {
            InitializeComponent();
            
            // 获取视频服务
            var serviceProvider = ((App)Application.Current).Services;
            _videoService = serviceProvider.GetRequiredService<IVideoService>();
            
            // 初始化事件处理
            TimelineScrollViewer.ScrollChanged += TimelineScrollViewer_ScrollChanged;
        }
        
        private static void OnZoomLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TimelineControl control)
            {
                control.UpdateZoom();
            }
        }
        
        public async Task LoadVideo(VideoInfo videoInfo)
        {
            _currentVideo = videoInfo;
            
            // 更新视频信息
            VideoInfoText.Text = $"{Path.GetFileName(videoInfo.FilePath)} {videoInfo.Duration:hh\\:mm\\:ss}";
            
            // 计算时间轴宽度
            var totalWidth = videoInfo.Duration.TotalSeconds * _pixelsPerSecond * (_zoomLevel / 100.0);
            TimelineCanvas.Width = Math.Max(2000, totalWidth);
            
            // 生成时间标尺
            GenerateTimeRuler();
            
            // 生成缩略图
            await GenerateThumbnails();
            
            // 生成音频波形
            await GenerateAudioWaveform();
        }
        
        private void GenerateTimeRuler()
        {
            RulerCanvas.Children.Clear();
            
            if (_currentVideo == null) return;
            
            var duration = _currentVideo.Duration.TotalSeconds;
            var interval = GetTimeInterval(duration);
            var pixelsPerSecond = _pixelsPerSecond * (_zoomLevel / 100.0);
            
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
        }
        
        private double GetTimeInterval(double duration)
        {
            // 根据缩放级别和视频时长调整时间间隔
            if (_zoomLevel >= 200) return 5; // 5秒间隔
            if (_zoomLevel >= 100) return 10; // 10秒间隔
            if (_zoomLevel >= 50) return 30; // 30秒间隔
            return 60; // 60秒间隔
        }
        
        private async Task GenerateThumbnails()
        {
            VideoTrackCanvas.Children.Clear();
            _thumbnails.Clear();
            
            if (_currentVideo == null) return;
            
            var duration = _currentVideo.Duration.TotalSeconds;
            var pixelsPerSecond = _pixelsPerSecond * (_zoomLevel / 100.0);
            var thumbnailInterval = Math.Max(1, 10 / (_zoomLevel / 100.0)); // 根据缩放调整缩略图间隔
            
            for (double time = 0; time < duration; time += thumbnailInterval)
            {
                try
                {
                    // 生成缩略图
                    var thumbnailPath = await _videoService.GenerateThumbnailAsync(_currentVideo.FilePath, time);
                    
                    if (File.Exists(thumbnailPath))
                    {
                        var image = new Image
                        {
                            Source = new BitmapImage(new Uri(thumbnailPath)),
                            Width = thumbnailInterval * pixelsPerSecond - 2,
                            Height = 76,
                            Stretch = Stretch.UniformToFill
                        };
                        
                        Canvas.SetLeft(image, time * pixelsPerSecond + 1);
                        Canvas.SetTop(image, 2);
                        
                        VideoTrackCanvas.Children.Add(image);
                        _thumbnails.Add(image);
                    }
                }
                catch (Exception ex)
                {
                    // 如果缩略图生成失败，创建一个占位符
                    var placeholder = new Border
                    {
                        Background = Brushes.Gray,
                        Width = thumbnailInterval * pixelsPerSecond - 2,
                        Height = 76,
                        Child = new TextBlock
                        {
                            Text = $"{time:F1}s",
                            Foreground = Brushes.White,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            FontSize = 8
                        }
                    };
                    
                    Canvas.SetLeft(placeholder, time * pixelsPerSecond + 1);
                    Canvas.SetTop(placeholder, 2);
                    
                    VideoTrackCanvas.Children.Add(placeholder);
                }
            }
        }
        
        private async Task GenerateAudioWaveform()
        {
            AudioTrackCanvas.Children.Clear();
            _waveformBars.Clear();
            
            if (_currentVideo == null) return;
            
            try
            {
                // 生成音频波形数据
                var waveformData = await _videoService.GenerateAudioWaveformAsync(_currentVideo.FilePath);
                
                var pixelsPerSecond = _pixelsPerSecond * (_zoomLevel / 100.0);
                var barWidth = Math.Max(1, pixelsPerSecond / 10); // 每0.1秒一个波形条
                
                for (int i = 0; i < waveformData.Count; i++)
                {
                    var amplitude = waveformData[i];
                    var x = i * barWidth;
                    var height = Math.Max(2, amplitude * 50); // 将振幅转换为高度
                    
                    var bar = new Rectangle
                    {
                        Fill = new SolidColorBrush(Color.FromRgb(0, 150, 150)), // 青色
                        Width = barWidth - 1,
                        Height = height,
                        RadiusX = 1,
                        RadiusY = 1
                    };
                    
                    Canvas.SetLeft(bar, x);
                    Canvas.SetTop(bar, 30 - height / 2); // 居中显示
                    
                    AudioTrackCanvas.Children.Add(bar);
                    _waveformBars.Add(bar);
                }
            }
            catch (Exception ex)
            {
                // 如果波形生成失败，创建模拟波形
                var duration = _currentVideo.Duration.TotalSeconds;
                var pixelsPerSecond = _pixelsPerSecond * (_zoomLevel / 100.0);
                var barWidth = Math.Max(1, pixelsPerSecond / 10);
                
                var random = new Random(42); // 固定种子以获得一致的模拟波形
                
                for (double time = 0; time < duration; time += 0.1)
                {
                    var amplitude = random.NextDouble() * 0.8 + 0.2; // 0.2-1.0之间的随机值
                    var x = time * pixelsPerSecond;
                    var height = amplitude * 40;
                    
                    var bar = new Rectangle
                    {
                        Fill = new SolidColorBrush(Color.FromRgb(0, 150, 150)),
                        Width = barWidth - 1,
                        Height = height,
                        RadiusX = 1,
                        RadiusY = 1
                    };
                    
                    Canvas.SetLeft(bar, x);
                    Canvas.SetTop(bar, 30 - height / 2);
                    
                    AudioTrackCanvas.Children.Add(bar);
                    _waveformBars.Add(bar);
                }
            }
        }
        
        public void UpdatePlayhead(TimeSpan currentTime)
        {
            if (_currentVideo == null) return;
            
            var pixelsPerSecond = _pixelsPerSecond * (_zoomLevel / 100.0);
            var x = currentTime.TotalSeconds * pixelsPerSecond;
            
            PlayheadLine.X1 = x;
            PlayheadLine.X2 = x;
            
            // 确保播放头在可视区域内
            if (x > TimelineScrollViewer.HorizontalOffset + TimelineScrollViewer.ViewportWidth - 50)
            {
                TimelineScrollViewer.ScrollToHorizontalOffset(x - TimelineScrollViewer.ViewportWidth + 100);
            }
        }
        
        private void UpdateZoom()
        {
            if (_currentVideo == null) return;
            
            var oldZoom = _zoomLevel;
            _zoomLevel = ZoomLevel;
            
            // 重新计算时间轴宽度
            var totalWidth = _currentVideo.Duration.TotalSeconds * _pixelsPerSecond * (_zoomLevel / 100.0);
            TimelineCanvas.Width = Math.Max(2000, totalWidth);
            
            // 重新生成时间轴内容
            GenerateTimeRuler();
            _ = GenerateThumbnails();
            _ = GenerateAudioWaveform();
            
            // 调整播放头位置
            if (DataContext is ViewModels.MainWindowViewModel viewModel)
            {
                UpdatePlayhead(viewModel.CurrentTime);
            }
        }
        
        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            ZoomLevel = Math.Min(400, ZoomLevel + 25);
        }
        
        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            ZoomLevel = Math.Max(25, ZoomLevel - 25);
        }
        
        private void FitToWindow_Click(object sender, RoutedEventArgs e)
        {
            if (_currentVideo == null) return;
            
            var availableWidth = TimelineScrollViewer.ActualWidth - 50;
            var duration = _currentVideo.Duration.TotalSeconds;
            var optimalZoom = (availableWidth / duration) / _pixelsPerSecond * 100;
            
            ZoomLevel = Math.Max(25, Math.Min(400, optimalZoom));
        }
        
        private void TimelineScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // 可以在这里添加滚动事件处理逻辑
        }
    }
} 