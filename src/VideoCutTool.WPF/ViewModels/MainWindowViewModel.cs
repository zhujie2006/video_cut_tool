using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using System.Windows.Threading;
using VideoCutTool.Core.Constant;
using VideoCutTool.Core.Interfaces;
using VideoCutTool.Core.Models;
using VideoCutTool.WPF.Views;

namespace VideoCutTool.WPF.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject, IMainPageNotifyHandler
    {
        private readonly IVideoService _videoService;
        private readonly IFileDialogService _fileDialogService;
        private readonly IProjectService _projectService;
        private readonly ILogger _logger;

        public MainWindowViewModel(IVideoService videoService, IFileDialogService fileDialogService, IProjectService projectService)
        {
            _videoService = videoService;
            _fileDialogService = fileDialogService;
            _projectService = projectService;
            _logger = Log.ForContext<MainWindowViewModel>();
            
            _logger.Information("MainWindowViewModel 初始化开始");
            
            // 初始化最近导出列表
            LoadRecentExports();
            
            _logger.Information("MainWindowViewModel 初始化完成");
        }

        #region 属性

        [ObservableProperty]
        private TimelineControlViewModel? _timelineViewModel;

        [ObservableProperty]
        private SettingViewModel? _settingViewModel;

        [ObservableProperty]
        private VideoInfo? _videoInfo;

        [ObservableProperty]
        private string? _videoSource;

        [ObservableProperty]
        private TimeSpan _currentTime = TimeSpan.Zero;

        [ObservableProperty]
        private double _currentPosition;

        [ObservableProperty]
        private double _volume = 50;

        [ObservableProperty]
        private string _timeDisplay = "00:00 / 00:00";

        [ObservableProperty]
        private string _playPauseIcon = "Play";

        [ObservableProperty]
        private string _statusMessage = "就绪";

        [ObservableProperty]
        private ProjectInfo _projectInfo = new();

        [ObservableProperty]
        private string _selectedExportFormat = "MP4";

        [ObservableProperty]
        private string _selectedExportQuality = "高质量 (1080p)";

        [ObservableProperty]
        private string _selectedFrameRate = "30 fps";

        [ObservableProperty]
        private ObservableCollection<RecentExport> _recentExports = new();

        [ObservableProperty]
        private string _preciseTimeDisplay = "0:00.0";

        [ObservableProperty]
        private bool _isPlaying = false;

        [ObservableProperty]
        private bool _isExporting = false;

        [ObservableProperty]
        private int _exportProgress = 0;

        #endregion

        #region 命令

        // Commands
        [RelayCommand]
        private async Task ImportVideoAsync()
        {
            _logger.Information("开始导入视频流程");
            
            try
            {
                StatusMessage = "选择视频文件...";
                _logger.Debug("显示文件选择对话框");
                
                var filePath = _fileDialogService.SelectVideoFile();
                if (string.IsNullOrEmpty(filePath))
                {
                    _logger.Information("用户取消文件选择");
                    StatusMessage = "未选择文件";
                    return;
                }
                
                _logger.Information("用户选择文件: {FilePath}", filePath);
                
                StatusMessage = "正在分析视频文件...";
                
                // 检查文件是否存在
                if (!File.Exists(filePath))
                {
                    _logger.Warning("选择的文件不存在: {FilePath}", filePath);
                    StatusMessage = "选择的文件不存在";
                    return;
                }
                
                var fileInfo = new FileInfo(filePath);
                _logger.Information("文件信息 - 大小: {FileSize} bytes, 创建时间: {CreatedTime}", 
                    fileInfo.Length, fileInfo.CreationTime);
                
                // 获取视频信息
                _logger.Debug("开始调用视频服务获取视频信息");
                VideoInfo = await _videoService.GetVideoInfoAsync(filePath);
                _logger.Information("视频信息获取成功 - 名称: {Name}, 时长: {Duration}, 分辨率: {Resolution}, 帧率: {FrameRate}", 
                    VideoInfo.Name, VideoInfo.Duration, VideoInfo.Resolution, VideoInfo.FrameRate);
                
                VideoSource = filePath;
                
                // 初始化项目信息
                _logger.Debug("初始化项目信息");
                ProjectInfo.Reset();
                ProjectInfo.Name = VideoInfo.Name;
                ProjectInfo.Duration = VideoInfo.Duration;
                 // 复制当前时间轴片段
                ProjectInfo.OutputDuration = VideoInfo.Duration;
                ProjectInfo.EstimatedSize = "0 MB";
                ProjectInfo.VideoInfo = VideoInfo;

                // 重置时间轴和播放状态
                _logger.Debug("重置时间轴和播放状态");
                CurrentPosition = 0;
                CurrentTime = TimeSpan.Zero;
                IsPlaying = false;
                PlayPauseIcon = "Play";
                
                // 更新状态
                StatusMessage = $"已导入视频: {VideoInfo.Name} ({VideoInfo.Resolution}, {VideoInfo.Duration:mm\\:ss})";
                UpdateTimeDisplay();
                UpdatePreciseTimeDisplay();

                _logger.Information("视频导入流程完成成功");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "视频导入过程中发生异常");
                StatusMessage = $"导入失败: {ex.Message}";
                
                // 显示详细错误信息（在调试模式下）
                System.Diagnostics.Debug.WriteLine($"视频导入错误: {ex}");
            }
        }

        [RelayCommand]
        private void PlayPause()
        {
            if (VideoInfo == null || string.IsNullOrEmpty(VideoSource))
            {
                StatusMessage = "请先导入视频";
                return;
            }
            
            var oldIsPlaying = IsPlaying;
            IsPlaying = !IsPlaying;
            PlayPauseIcon = IsPlaying ? "Pause" : "Play";
            StatusMessage = IsPlaying ? "播放中" : "已暂停";
            
            _logger.Information("播放状态切换 - {OldState} -> {NewState}", oldIsPlaying ? "播放" : "暂停", IsPlaying ? "播放" : "暂停");
            
            // 通知UI更新播放状态
            OnPropertyChanged(nameof(IsPlaying));
        }
        
        // private void PlaybackTimer_Tick(object? sender, EventArgs e)
        // {
        //     if (IsPlaying && VideoInfo != null)
        //     {
        //         // 更新播放位置
        //         CurrentTime = CurrentTime.Add(TimeSpan.FromMilliseconds(100));
                
        //         // 检查是否到达视频结尾
        //         if (CurrentTime >= VideoInfo.Duration)
        //         {
        //             IsPlaying = false;
        //             PlayPauseIcon = "Play";
        //             _playbackTimer.Stop();
        //             CurrentTime = TimeSpan.Zero;
        //             _logger.Information("视频播放完成");
        //         }
        //     }
        // }

        [RelayCommand]
        private void PreviousFrame()
        {
            if (VideoInfo == null)
            {
                StatusMessage = "请先导入视频";
                return;
            }
            
            // 跳转 5 秒前
            var newTime = CurrentTime.Subtract(TimeSpan.FromSeconds(5));
            if (newTime < TimeSpan.Zero)
            {
                newTime = TimeSpan.Zero;
            }
            
            SetCurrentTime(newTime);
            StatusMessage = $"跳转到 {newTime:hh\\:mm\\:ss}";
            _logger.Information("跳转 5 秒前: {NewTime}", newTime);
        }

        [RelayCommand]
        private void NextFrame()
        {
            if (VideoInfo == null)
            {
                StatusMessage = "请先导入视频";
                return;
            }
            
            // 跳转 5 秒后
            var newTime = CurrentTime.Add(TimeSpan.FromSeconds(5));
            if (newTime > VideoInfo.Duration)
            {
                newTime = VideoInfo.Duration;
            }
            
            SetCurrentTime(newTime);
            StatusMessage = $"跳转到 {newTime:hh\\:mm\\:ss}";
            _logger.Information("跳转 5 秒后: {NewTime}", newTime);
        }

        [RelayCommand]
        private void Seek()
        {
            StatusMessage = "跳转功能待实现";
            // TODO: 实现跳转功能
        }

        [RelayCommand]
        private async Task SaveProjectAsync()
        {
            _logger.Information("开始保存项目");
            
            if (VideoInfo == null)
            {
                _logger.Warning("尝试保存项目但未导入视频");
                StatusMessage = "请先导入视频";
                return;
            }
            
            try
            {
                // 选择保存路径
                var filePath = _fileDialogService.SaveProjectFile(VideoInfo?.Name ?? "新项目");
                if (string.IsNullOrEmpty(filePath))
                {
                    _logger.Information("用户取消保存项目");
                    StatusMessage = "未选择保存路径";
                    return;
                }
                
                _logger.Information("选择保存路径: {FilePath}", filePath);
                
                StatusMessage = "正在保存项目...";
                
                // 更新项目信息
                if (ProjectInfo != null)
                {
                    
                    var success = await _projectService.SaveProjectAsync(ProjectInfo, filePath);
                    if (success)
                    {
                        _logger.Information("项目保存成功: {FilePath}", filePath);
                        StatusMessage = $"项目已保存: {Path.GetFileName(filePath)}";
                        return;
                    }
                }
                else
                {
                    _logger.Error("项目信息为空，无法保存");
                    StatusMessage = "项目信息无效，无法保存";
                    return;
                }

                _logger.Error("项目保存失败: {FilePath}", filePath);
                StatusMessage = "项目保存失败";
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "保存项目过程中发生异常");
                StatusMessage = $"保存失败: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task LoadProjectAsync()
        {
            _logger.Information("开始加载项目");
            
            try
            {
                // 选择项目文件
                var filePath = _fileDialogService.OpenProjectFile();
                if (string.IsNullOrEmpty(filePath))
                {
                    _logger.Information("用户取消加载项目");
                    StatusMessage = "未选择项目文件";
                    return;
                }
                
                _logger.Information("选择项目文件: {FilePath}", filePath);
                
                StatusMessage = "正在加载项目...";
                
                var project = await _projectService.LoadProjectAsync(filePath);
                if (project == null || project.VideoInfo == null)
                {
                    _logger.Error("项目文件加载失败: {FilePath}", filePath);
                    StatusMessage = "项目文件加载失败";
                    return;
                }
                
                // 验证视频文件是否存在
                if (!string.IsNullOrEmpty(project.VideoInfo.FilePath) && !File.Exists(project.VideoInfo.FilePath))
                {
                    _logger.Warning("项目中的视频文件不存在: {VideoSource}", project.VideoInfo.FilePath);
                    StatusMessage = "项目中的视频文件不存在，请重新选择";
                    
                    // 让用户重新选择视频文件
                    var newVideoPath = _fileDialogService.SelectVideoFile();
                    if (!string.IsNullOrEmpty(newVideoPath))
                    {
                        project.VideoInfo.FilePath = newVideoPath;
                        if (project.VideoInfo != null)
                        {
                            project.VideoInfo.FilePath = newVideoPath;
                        }
                    }
                    else
                    {
                        StatusMessage = "未选择视频文件，项目加载取消";
                        return;
                    }
                }
                
                // 加载项目数据
                VideoInfo = project.VideoInfo;
                if (project.VideoInfo != null)
                {
                    VideoSource = project.VideoInfo.FilePath;
                }
                
                Volume = project.Volume;
                
                // 更新导出设置
                if (project.ExportSettings != null)
                {
                    SelectedExportFormat = project.ExportSettings.Format;
                    SelectedExportQuality = project.ExportSettings.OutputQuality + "";
                    SelectedFrameRate = project.ExportSettings.FrameRate + "";
                }
                
                _logger.Information("项目加载成功: {FilePath}", filePath);
                StatusMessage = $"项目已加载: {Path.GetFileName(filePath)}";
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "加载项目过程中发生异常");
                StatusMessage = $"加载失败: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task ExportAsync()
        {
            _logger.Information("开始导出视频流程");
            
            if (VideoInfo == null)
            {
                _logger.Warning("尝试导出但未导入视频");
                StatusMessage = "请先导入视频";
                return;
            }
            
            if (ProjectInfo.TimelineSegments.Count == 0)
            {
                _logger.Warning("尝试导出但时间轴为空");
                StatusMessage = "请先添加时间轴片段";
                return;
            }
            
            try
            {
                // 获取活跃的片段
                var activeSegments = ProjectInfo.TimelineSegments.Where(s => !s.IsDeleted).ToList();
                if (activeSegments.Count == 0)
                {
                    _logger.Warning("没有活跃的片段可以导出");
                    StatusMessage = "没有可导出的片段";
                    return;
                }
                
                _logger.Information("准备导出 {SegmentCount} 个活跃片段", activeSegments.Count);
                
                // 获取输出文件路径
                var defaultFileName = VideoInfo?.Name ?? "导出视频";
                var filter = $"{SelectedExportFormat} 文件 (*.{SelectedExportFormat.ToLower()})|*.{SelectedExportFormat.ToLower()}|所有文件 (*.*)|*.*";
                var outputPath = _fileDialogService.SelectSaveFile(defaultFileName, filter);
                if (string.IsNullOrEmpty(outputPath))
                {
                    _logger.Information("用户取消了导出操作");
                    return;
                }
                
                _logger.Information("开始导出视频 - 输出路径: {OutputPath}, 片段数量: {SegmentCount}", outputPath, activeSegments.Count);
                
                // 设置导出状态
                IsExporting = true;
                ExportProgress = 0;
                
                // 创建进度报告
                var progress = new Progress<int>(percent =>
                {
                    ExportProgress = percent;
                    StatusMessage = $"正在导出视频... {percent}%";
                    _logger.Debug("导出进度: {Progress}%", percent);
                });
                
                // 获取导出设置
                var exportSettings = new ExportSettings
                {
                    Format = SelectedExportFormat,
                    OutputQuality = ParseQuality(SelectedExportQuality),
                    FrameRate = ParseFrameRate(SelectedFrameRate),
                    MaintainOriginalResolution = true,
                    MaintainOriginalFrameRate = true
                };
                
                _logger.Information("导出设置 - 格式: {Format}, 质量: {Quality}, 帧率: {FrameRate}", 
                    exportSettings.Format, exportSettings.OutputQuality, exportSettings.FrameRate);
                
                // 执行多片段拼接导出
                var success = await _videoService.ExportMultipleSegmentsAsync(
                    VideoInfo.FilePath, 
                    outputPath, 
                    activeSegments, 
                    exportSettings, 
                    progress);
                
                if (success)
                {
                    _logger.Information("视频导出成功: {OutputPath}", outputPath);
                    StatusMessage = $"视频导出成功: {Path.GetFileName(outputPath)}";
                    
                    // 记录导出历史
                    var recentExport = new RecentExport
                    {
                        FilePath = outputPath,
                        ExportTime = DateTime.Now,
                        Format = Path.GetExtension(outputPath).TrimStart('.'),
                        FileSize = new FileInfo(outputPath).Length,
                        ExportSettings = exportSettings,
                        Status = "成功"
                    };
                    
                    RecentExports.Insert(0, recentExport);
                    
                    // 保持最近导出列表不超过10个
                    if (RecentExports.Count > 10)
                    {
                        RecentExports.RemoveAt(RecentExports.Count - 1);
                    }
                    
                    // 保存最近导出列表
                    SaveRecentExports();
                }
                else
                {
                    _logger.Error("视频导出失败");
                    StatusMessage = "视频导出失败，请检查输出路径和设置";
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "导出视频时发生异常");
                StatusMessage = $"导出失败: {ex.Message}";
            }
            finally
            {
                // 重置导出状态
                IsExporting = false;
                ExportProgress = 0;
            }
        }

        [RelayCommand]
        private void AdvancedSettings()
        {
            _logger.Information("打开高级设置对话框");
            
            try
            {
                var settings = new ExportSettings
                {
                    Format = SelectedExportFormat,
                    OutputQuality = ParseQuality(SelectedExportQuality),
                    FrameRate = ParseFrameRate(SelectedFrameRate),
                    VideoCodec = "h264",
                    AudioCodec = "aac",
                    VideoBitrate = 5000,
                    AudioChannels = 1
                };
                
                var advancedSettingsViewModel = new AdvancedSettingsViewModel(settings);
                var advancedSettingsWindow = new AdvancedSettingsWindow
                {
                    DataContext = advancedSettingsViewModel,
                    Owner = System.Windows.Application.Current.MainWindow
                };
                
                var result = advancedSettingsWindow.ShowDialog();
                if (result == true)
                {
                    var updatedSettings = advancedSettingsViewModel.GetUpdatedSettings();
                    
                    // 更新导出设置
                    SelectedExportFormat = updatedSettings.Format;
                    SelectedExportQuality = updatedSettings.OutputQuality + "";
                    SelectedFrameRate = updatedSettings.FrameRate + "";
                    
                    _logger.Information("高级设置已保存");
                    StatusMessage = "高级设置已更新";
                }
                else
                {
                    _logger.Information("高级设置对话框已取消");
                    StatusMessage = "高级设置未更改";
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "打开高级设置对话框时发生异常");
                StatusMessage = $"高级设置错误: {ex.Message}";
            }
        }
        
        [RelayCommand]
        private void OpenExportFile(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                _fileDialogService.OpenFileInExplorer(filePath);
            }
        }

        #endregion

        #region 公共方法

        #region 实现 IMainPageNotifyHandler

        /// <summary>
        ///  实现状态信息接口
        /// </summary>
        /// <param name="message"></param>
        public void NotifyStatusMessage(string message)
        {
            StatusMessage = message;
        }

        /// <summary>
        /// 通知界面更新项目信息
        /// </summary>
        public void UpdateProjectInfo()
        {
            if (VideoInfo == null)
            {
                _logger.Warning("尝试更新项目信息但视频信息为空");
                return;
            }

            if (TimelineViewModel != null)
            {
                ProjectInfo.TimelineSegments = TimelineViewModel.TimelineSegments.ToList();
            }
            ProjectInfo.VideoInfo = VideoInfo;
            ProjectInfo.Volume = Volume;
            ProjectInfo.ExportSettings = new ExportSettings
            {
                Format = SelectedExportFormat,
                OutputQuality = ParseQuality(SelectedExportQuality),
                FrameRate = ParseFrameRate(SelectedFrameRate)
            };

            var segmentCount = ProjectInfo.TimelineSegments.Count;
            var oldOutputDuration = ProjectInfo.OutputDuration;

            ProjectInfo.OutputDuration = TimeSpan.FromSeconds(ProjectInfo.TimelineSegments.Sum(s => s.Duration.TotalSeconds));

            // 估算文件大小（基于视频质量和时长）
            var totalSeconds = ProjectInfo.OutputDuration.TotalSeconds;
            var estimatedSizeMB = totalSeconds * 2; // 简单估算：每秒2MB
            ProjectInfo.EstimatedSize = $"{estimatedSizeMB:F1} MB";

            _logger.Debug("项目信息更新 - 片段数: {NewCount}, 输出时长: {OldDuration} -> {NewDuration}, 预估大小: {EstimatedSize}",
                segmentCount, oldOutputDuration, ProjectInfo.OutputDuration, ProjectInfo.EstimatedSize);
        }

        public void SetCurrentTime(TimeSpan time)
        {
            CurrentTime = time;

            // 如果正在播放，先暂停
            if (IsPlaying)
            {
                IsPlaying = false;
                PlayPauseIcon = "Play";
            }
        }

        public TimeSpan GetCurrentTime()
        {
            var time = CurrentTime;
            return time;
        }

        #endregion

        #endregion

        #region 界面事件

        partial void OnCurrentPositionChanged(double value)
        {
            var oldTime = CurrentTime;
            CurrentTime = TimeSpan.FromSeconds(value);
            
            _logger.Debug("播放位置更新 - {OldTime} -> {NewTime}", oldTime, CurrentTime);
            
            UpdateTimeDisplay();
            UpdatePreciseTimeDisplay();
        }

        partial void OnCurrentTimeChanged(TimeSpan value)
        {
            //_logger.Debug("当前时间更新: {CurrentTime}", value);
            
            // 更新时间显示
            UpdateTimeDisplay();
            UpdatePreciseTimeDisplay();
        }

        partial void OnVolumeChanged(double value)
        {
            // TODO: 实现音量控制
        }

        #endregion

        #region 私有方法

        private void UpdateTimeDisplay()
        {
            if (VideoInfo != null)
            {
                TimeDisplay = $"{CurrentTime:mm\\:ss} / {VideoInfo.Duration:mm\\:ss}";
            }
            else
            {
                TimeDisplay = $"{CurrentTime:mm\\:ss} / 00:00";
            }
        }

        private void UpdatePreciseTimeDisplay()
        {
            var minutes = (int)CurrentTime.TotalMinutes;
            var seconds = CurrentTime.Seconds;
            var tenths = (int)(CurrentTime.Milliseconds / 100.0);
            PreciseTimeDisplay = $"{minutes}:{seconds:D2}.{tenths}";
        }
        
        private void LoadRecentExports()
        {
            try
            {
                var recentExportsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VideoCutTool", "recent_exports.json");
                if (File.Exists(recentExportsPath))
                {
                    var json = File.ReadAllText(recentExportsPath);
                    // 这里可以添加JSON反序列化逻辑
                    // 暂时使用空实现
                }
            }
            catch
            {
                // 忽略加载错误
            }
        }
        
        private void SaveRecentExports()
        {
            try
            {
                var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VideoCutTool");
                if (!Directory.Exists(appDataPath))
                {
                    Directory.CreateDirectory(appDataPath);
                }
                
                var recentExportsPath = Path.Combine(appDataPath, "recent_exports.json");
                // 这里可以添加JSON序列化逻辑
                // 暂时使用空实现
            }
            catch
            {
                // 忽略保存错误
            }
        }

        private int ParseQuality(string strQuality)
        {
            if (string.IsNullOrEmpty(strQuality))
            {
                return VideoFormatConst.QualityMediumValue;
            }

            switch (strQuality)
            {
                case VideoFormatConst.QualityHigh:
                    return VideoFormatConst.QualityHighValue;
                case VideoFormatConst.QualityMedium:
                    return VideoFormatConst.QualityMediumValue;
                case VideoFormatConst.QualityLow:
                    return VideoFormatConst.QualityLowValue;
                default:
                    return VideoFormatConst.QualityMediumValue;
            }
        }

        private int ParseFrameRate(string strFrameRate)
        {
            if (string.IsNullOrEmpty(strFrameRate))
            {
                return VideoFormatConst.MediumFrameRateValue;
            }
            switch (strFrameRate)
            {
                case VideoFormatConst.LowFrameRate:
                    return VideoFormatConst.LowFrameRateValue;
                case VideoFormatConst.MediumFrameRate:
                    return VideoFormatConst.MediumFrameRateValue;
                case VideoFormatConst.HighFrameRate:
                    return VideoFormatConst.HighFrameRateValue;
                case VideoFormatConst.MaxFrameRate:
                    return VideoFormatConst.MaxFrameRateValue;
                default:
                    return VideoFormatConst.MediumFrameRateValue;
            }
        }

        #endregion
    }
} 