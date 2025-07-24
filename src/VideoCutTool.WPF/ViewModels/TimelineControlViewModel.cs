using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using VideoCutTool.Core.Models;
using VideoCutTool.Core.Interfaces;
using Serilog;

namespace VideoCutTool.WPF.ViewModels
{
    public class TimelineControlViewModel : INotifyPropertyChanged
    {
        private readonly IVideoService _videoService;
        private readonly ILogger _logger;
        
        // 时间轴配置
        private const double PIXELS_PER_SECOND = 4.6; // 4.6像素/秒，1分钟=276像素
        public static double THUMBNAIL_WIDTH = 64.0; // 缩略图固定宽度
        
        // 视频信息
        private VideoInfo? _currentVideo;
        private double _zoomLevel = 1.0; // 缩放倍数：1, 10, 20, 30, ..., 100
        private TimeSpan _currentTime = TimeSpan.Zero;
        private double _totalWidth = 1000; // 时间轴总宽度
        
        // 缩略图缓存
        private Dictionary<string, string> _thumbnailCache = new();
        
        // 事件
        public event Action<TimeSpan>? PlayheadPositionChanged;
        public event Action<TimeSpan>? SplitPointRequested;
        public event Action? TimelineContentChanged;
        
        public TimelineControlViewModel(IVideoService videoService)
        {
            _videoService = videoService;
            _logger = Log.ForContext<TimelineControlViewModel>();
            
            _logger.Information("TimelineControlViewModel 初始化完成");
        }
        
        #region 属性
        
        public VideoInfo? CurrentVideo
        {
            get => _currentVideo;
            set
            {
                if (SetProperty(ref _currentVideo, value))
                {
                    _logger.Information($"设置当前视频: {value?.FilePath ?? "null"}");
                    _ = LoadVideoAsync();
                }
            }
        }
        
        public double ZoomLevel
        {
            get => _zoomLevel;
            set
            {
                if (SetProperty(ref _zoomLevel, value))
                {
                    _logger.Debug($"设置缩放级别: {value}");
                    _ = UpdateTimelineContentAsync();
                }
            }
        }
        
        public TimeSpan CurrentTime
        {
            get => _currentTime;
            set
            {
                if (SetProperty(ref _currentTime, value))
                {
                    _logger.Debug($"设置当前时间: {value:mm\\:ss\\.f}");
                    PlayheadPositionChanged?.Invoke(value);
                }
            }
        }
        
        public double TotalWidth
        {
            get => _totalWidth;
            set
            {
                if (SetProperty(ref _totalWidth, value))
                {
                    _logger.Debug($"设置时间轴总宽度: {value}px");
                    TimelineContentChanged?.Invoke();
                }
            }
        }
        
        // 计算属性
        public double PixelsPerSecond => PIXELS_PER_SECOND * _zoomLevel; // 4.6 × 缩放倍数
        public double ThumbnailTimeInterval => THUMBNAIL_WIDTH / PixelsPerSecond;
        
        #endregion
        
        #region 公共方法
        
        public async Task LoadVideoAsync()
        {
            if (_currentVideo == null)
            {
                _logger.Warning("尝试加载视频但CurrentVideo为null");
                return;
            }
            
            _logger.Information($"开始加载视频: {_currentVideo.FilePath}");
            
            try
            {
                // 计算时间轴宽度
                var duration = _currentVideo.Duration.TotalSeconds;
                TotalWidth = duration * PixelsPerSecond;
                
                _logger.Information($"视频时长: {_currentVideo.Duration:mm\\:ss}, 时间轴宽度: {TotalWidth}px");
                
                // 清除缓存
                _thumbnailCache.Clear();
                
                // 触发内容更新
                TimelineContentChanged?.Invoke();
                
                _logger.Information("视频加载完成");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "加载视频时发生错误");
            }
        }
        
        public async Task UpdateTimelineContentAsync()
        {
            if (_currentVideo == null)
            {
                _logger.Warning("尝试更新时间轴内容但CurrentVideo为null");
                return;
            }
            
            _logger.Debug($"开始更新时间轴内容，缩放级别: {_zoomLevel}");
            
            try
            {
                // 重新计算时间轴宽度
                var duration = _currentVideo.Duration.TotalSeconds;
                TotalWidth = duration * PixelsPerSecond;
                
                _logger.Debug($"更新后时间轴宽度: {TotalWidth}px, 像素/秒: {PixelsPerSecond}");
                
                // 触发内容更新
                TimelineContentChanged?.Invoke();
                
                _logger.Debug("时间轴内容更新完成");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "更新时间轴内容时发生错误");
            }
        }
        
        public void UpdatePlayheadPosition(TimeSpan time)
        {
            _logger.Debug($"更新播放头位置: {time:mm\\:ss\\.f}");
            CurrentTime = time;
        }
        
        public void RequestSplitPoint(TimeSpan time)
        {
            _logger.Information($"请求切分点: {time:mm\\:ss\\.f}");
            SplitPointRequested?.Invoke(time);
        }
        
        public async Task<string?> GetThumbnailPathAsync(double time)
        {
            if (_currentVideo == null)
            {
                _logger.Warning("尝试获取缩略图但CurrentVideo为null");
                return null;
            }
            
            var cacheKey = GetThumbnailCacheKey(time);
            
            // 检查内存缓存
            if (_thumbnailCache.ContainsKey(cacheKey))
            {
                _logger.Debug($"从内存缓存获取缩略图: {time:F1}s");
                return _thumbnailCache[cacheKey];
            }
            
            try
            {
                _logger.Debug($"生成缩略图: {time:F1}s");
                var thumbnailPath = await _videoService.GenerateThumbnailAsync(_currentVideo.FilePath, time);
                
                if (System.IO.File.Exists(thumbnailPath))
                {
                    // 添加到内存缓存
                    _thumbnailCache[cacheKey] = thumbnailPath;
                    _logger.Debug($"缩略图生成成功: {thumbnailPath}");
                    return thumbnailPath;
                }
                else
                {
                    _logger.Warning($"缩略图文件不存在: {thumbnailPath}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"生成缩略图失败: {time:F1}s");
                return null;
            }
        }
        
        #endregion
        
        #region 私有方法
        
        private string GetThumbnailCacheKey(double time)
        {
            var videoFileName = System.IO.Path.GetFileNameWithoutExtension(_currentVideo?.FilePath ?? "");
            var timeKey = time.ToString("F1").Replace(".", "_");
            return $"{videoFileName}_{timeKey}s";
        }
        
        #endregion
        
        #region INotifyPropertyChanged
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        
        #endregion
    }
} 