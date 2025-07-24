using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.IO;
using System.Windows.Threading;
using VideoCutTool.Core.Models;
using VideoCutTool.Core.Interfaces;
using VideoCutTool.WPF.Views;
using Serilog;

namespace VideoCutTool.WPF.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly IVideoService _videoService;
        private readonly IFileDialogService _fileDialogService;
        private readonly IProjectService _projectService;
        private readonly Stack<Action> _undoStack = new();
        private readonly Stack<Action> _redoStack = new();
        private readonly ILogger _logger;
        
        public MainWindowViewModel(IVideoService videoService, IFileDialogService fileDialogService, IProjectService projectService)
        {
            _videoService = videoService;
            _fileDialogService = fileDialogService;
            _projectService = projectService;
            _logger = Log.ForContext<MainWindowViewModel>();
            
            _logger.Information("MainWindowViewModel 初始化开始");
            
            // 初始化播放定时器
            // _playbackTimer = new DispatcherTimer
            // {
            //     Interval = TimeSpan.FromMilliseconds(100) // 100ms更新一次
            // };
            // _playbackTimer.Tick += PlaybackTimer_Tick;
            
            // 初始化最近导出列表
            LoadRecentExports();
            
            _logger.Information("MainWindowViewModel 初始化完成");
        }
        
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
        private ObservableCollection<string> _exportFormats = new() { "MP4", "AVI", "MOV", "MKV" };

        [ObservableProperty]
        private string _selectedExportFormat = "MP4";

        [ObservableProperty]
        private ObservableCollection<string> _exportQualities = new() { "高质量 (1080p)", "中等质量 (720p)", "低质量 (480p)" };

        [ObservableProperty]
        private string _selectedExportQuality = "高质量 (1080p)";

        [ObservableProperty]
        private ObservableCollection<string> _frameRates = new() { "15 fps", "30 fps", "45 fps", "60 fps" };

        [ObservableProperty]
        private string _selectedFrameRate = "30 fps";

        [ObservableProperty]
        private ObservableCollection<TimelineSegment> _timelineSegments = new();
        
        [ObservableProperty]
        private ObservableCollection<RecentExport> _recentExports = new();

        // 新增属性
        [ObservableProperty]
        private bool _canUndo = false;

        [ObservableProperty]
        private bool _canRedo = false;

        [ObservableProperty]
        private bool _canDelete = false;

        [ObservableProperty]
        private bool _canCopy = false;

        [ObservableProperty]
        private string _preciseTimeDisplay = "0:00.0";

        [ObservableProperty]
        private bool _isPlaying = false;

        [ObservableProperty]
        private TimelineSegment? _selectedSegment;

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
                ProjectInfo.Name = VideoInfo.Name;
                ProjectInfo.Duration = VideoInfo.Duration;
                ProjectInfo.SegmentCount = 0;
                ProjectInfo.OutputDuration = TimeSpan.Zero;
                ProjectInfo.EstimatedSize = "0 MB";
                
                // 重置时间轴和播放状态
                _logger.Debug("重置时间轴和播放状态");
                TimelineSegments.Clear();
                CurrentPosition = 0;
                CurrentTime = TimeSpan.Zero;
                IsPlaying = false;
                PlayPauseIcon = "Play";
                // _playbackTimer.Stop(); // Removed as per edit hint
                
                // 更新状态
                StatusMessage = $"已导入视频: {VideoInfo.Name} ({VideoInfo.Resolution}, {VideoInfo.Duration:mm\\:ss})";
                UpdateTimeDisplay();
                UpdatePreciseTimeDisplay();
                
                // 更新命令状态
                UpdateCommandStates();
                
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
        private async Task SplitVideoAsync()
        {
            _logger.Information("开始分割视频流程");
            
            if (VideoInfo == null)
            {
                _logger.Warning("尝试分割视频但未导入视频");
                StatusMessage = "请先导入视频";
                return;
            }
            
            try
            {
                _logger.Debug("当前播放时间: {CurrentTime}, 视频总时长: {TotalDuration}", 
                    CurrentTime, VideoInfo.Duration);
                
                // 创建新的时间轴片段
                var segment = new TimelineSegment
                {
                    Name = $"片段_{TimelineSegments.Count + 1:D3}",
                    StartTime = CurrentTime,
                    EndTime = VideoInfo.Duration,
                    CreatedDate = DateTime.Now
                };
                
                _logger.Information("创建新片段 - 名称: {SegmentName}, 开始时间: {StartTime}, 结束时间: {EndTime}, 时长: {Duration}", 
                    segment.Name, segment.StartTime, segment.EndTime, segment.Duration);
                
                // 添加到时间轴
                TimelineSegments.Add(segment);
                _logger.Debug("片段已添加到时间轴，当前片段数量: {SegmentCount}", TimelineSegments.Count);
                
                // 保存撤销操作
                var segmentIndex = TimelineSegments.Count - 1;
                _undoStack.Push(() =>
                {
                    if (segmentIndex < TimelineSegments.Count)
                    {
                        TimelineSegments.RemoveAt(segmentIndex);
                        UpdateProjectInfo();
                        _logger.Debug("撤销操作：删除片段 {SegmentIndex}", segmentIndex);
                    }
                });
                
                // 更新项目信息
                UpdateProjectInfo();
                
                StatusMessage = $"已添加片段: {segment.Name}";
                _logger.Information("分割视频流程完成成功");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "分割视频过程中发生异常");
                StatusMessage = $"分割失败: {ex.Message}";
            }
        }

        [RelayCommand]
        private void DeleteSegment()
        {
            _logger.Information("开始删除片段流程");
            
            var selectedSegment = TimelineSegments.FirstOrDefault(s => s.IsSelected);
            if (selectedSegment == null)
            {
                _logger.Warning("尝试删除片段但未选择任何片段");
                StatusMessage = "请先选择要删除的片段";
                return;
            }
            
            _logger.Information("准备删除片段 - 名称: {SegmentName}, 开始时间: {StartTime}, 结束时间: {EndTime}", 
                selectedSegment.Name, selectedSegment.StartTime, selectedSegment.EndTime);
            
            var segmentIndex = TimelineSegments.IndexOf(selectedSegment);
            var segment = selectedSegment;
            
            // 保存撤销操作
            _undoStack.Push(() =>
            {
                TimelineSegments.Insert(segmentIndex, segment);
                UpdateProjectInfo();
                _logger.Debug("撤销操作：恢复片段 {SegmentName} 到位置 {SegmentIndex}", segment.Name, segmentIndex);
            });
            
            // 删除片段
            TimelineSegments.Remove(selectedSegment);
            UpdateProjectInfo();
            
            _logger.Information("片段删除成功，剩余片段数量: {RemainingSegments}", TimelineSegments.Count);
            StatusMessage = $"已删除片段: {selectedSegment.Name}";
        }

        [RelayCommand]
        private void CopySegment()
        {
            _logger.Information("开始复制片段流程");
            
            var selectedSegment = TimelineSegments.FirstOrDefault(s => s.IsSelected);
            if (selectedSegment == null)
            {
                _logger.Warning("尝试复制片段但未选择任何片段");
                StatusMessage = "请先选择要复制的片段";
                return;
            }
            
            _logger.Information("准备复制片段 - 原片段名称: {OriginalName}, 开始时间: {StartTime}, 结束时间: {EndTime}", 
                selectedSegment.Name, selectedSegment.StartTime, selectedSegment.EndTime);
            
            // 创建副本
            var copiedSegment = new TimelineSegment
            {
                Name = $"{selectedSegment.Name}_副本",
                StartTime = selectedSegment.StartTime,
                EndTime = selectedSegment.EndTime,
                CreatedDate = DateTime.Now
            };
            
            _logger.Debug("创建片段副本 - 新名称: {CopiedName}", copiedSegment.Name);
            
            // 添加到时间轴
            TimelineSegments.Add(copiedSegment);
            
            // 保存撤销操作
            var segmentIndex = TimelineSegments.Count - 1;
            _undoStack.Push(() =>
            {
                if (segmentIndex < TimelineSegments.Count)
                {
                    TimelineSegments.RemoveAt(segmentIndex);
                    UpdateProjectInfo();
                    _logger.Debug("撤销操作：删除复制的片段 {SegmentIndex}", segmentIndex);
                }
            });
            
            UpdateProjectInfo();
            _logger.Information("片段复制成功，当前片段数量: {SegmentCount}", TimelineSegments.Count);
            StatusMessage = $"已复制片段: {copiedSegment.Name}";
        }

        [RelayCommand]
        private void Undo()
        {
            _logger.Debug("尝试撤销操作，撤销栈大小: {UndoStackCount}", _undoStack.Count);
            
            if (_undoStack.Count > 0)
            {
                var action = _undoStack.Pop();
                _redoStack.Push(action);
                action();
                UpdateCommandStates();
                
                _logger.Information("撤销操作成功，撤销栈剩余: {UndoStackCount}, 重做栈大小: {RedoStackCount}", 
                    _undoStack.Count, _redoStack.Count);
                StatusMessage = "已撤销操作";
            }
            else
            {
                _logger.Debug("没有可撤销的操作");
                StatusMessage = "没有可撤销的操作";
            }
        }

        [RelayCommand]
        private void Redo()
        {
            _logger.Debug("尝试重做操作，重做栈大小: {RedoStackCount}", _redoStack.Count);
            
            if (_redoStack.Count > 0)
            {
                var action = _redoStack.Pop();
                _undoStack.Push(action);
                action();
                UpdateCommandStates();
                
                _logger.Information("重做操作成功，重做栈剩余: {RedoStackCount}, 撤销栈大小: {UndoStackCount}", 
                    _redoStack.Count, _undoStack.Count);
                StatusMessage = "已重做操作";
            }
            else
            {
                _logger.Debug("没有可重做的操作");
                StatusMessage = "没有可重做的操作";
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
            StatusMessage = "上一帧功能待实现";
            // TODO: 实现上一帧功能
        }

        [RelayCommand]
        private void NextFrame()
        {
            StatusMessage = "下一帧功能待实现";
            // TODO: 实现下一帧功能
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
                var filePath = _fileDialogService.SaveProjectFile();
                if (string.IsNullOrEmpty(filePath))
                {
                    _logger.Information("用户取消保存项目");
                    StatusMessage = "未选择保存路径";
                    return;
                }
                
                _logger.Information("选择保存路径: {FilePath}", filePath);
                
                StatusMessage = "正在保存项目...";
                
                // 创建项目文件
                var project = new ProjectFile
                {
                    //Name = ProjectInfo.Name,
                    VideoInfo = VideoInfo,
                    //VideoSource = VideoSource ?? string.Empty,
                    TimelineSegments = TimelineSegments.ToList(),
                    CurrentTime = CurrentTime,
                    Volume = Volume,
                    ExportSettings = new ExportSettings
                    {
                        Format = SelectedExportFormat,
                       // OutputQuality = SelectedExportQuality,
                       // FrameRate = SelectedFrameRate
                    }
                };
                
                var success = await _projectService.SaveProjectAsync(project, filePath);
                
                if (success)
                {
                    _logger.Information("项目保存成功: {FilePath}", filePath);
                    StatusMessage = $"项目已保存: {Path.GetFileName(filePath)}";
                }
                else
                {
                    _logger.Error("项目保存失败: {FilePath}", filePath);
                    StatusMessage = "项目保存失败";
                }
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
                VideoSource = project.VideoInfo.FilePath;
                CurrentTime = project.CurrentTime;
                Volume = project.Volume;
                
                TimelineSegments.Clear();
                foreach (var segment in project.TimelineSegments)
                {
                    TimelineSegments.Add(segment);
                }
                
                // 更新导出设置
                if (project.ExportSettings != null)
                {
                    SelectedExportFormat = project.ExportSettings.Format;
                    SelectedExportQuality = project.ExportSettings.OutputQuality + "";
                    SelectedFrameRate = project.ExportSettings.FrameRate + "";
                }
                
                // 更新项目信息
                UpdateProjectInfo();
                
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
            
            if (TimelineSegments.Count == 0)
            {
                _logger.Warning("尝试导出但时间轴为空");
                StatusMessage = "请先添加时间轴片段";
                return;
            }
            
            try
            {
                _logger.Information("准备导出 {SegmentCount} 个片段", TimelineSegments.Count);
                
                // 选择输出文件夹
                var outputFolder = _fileDialogService.SelectFolder();
                if (string.IsNullOrEmpty(outputFolder))
                {
                    _logger.Information("用户取消选择输出文件夹");
                    StatusMessage = "未选择输出文件夹";
                    return;
                }
                
                _logger.Information("选择输出文件夹: {OutputFolder}", outputFolder);
                
                StatusMessage = "正在导出视频片段...";
                
                var progress = new Progress<int>(percent =>
                {
                    StatusMessage = $"导出进度: {percent}%";
                    _logger.Debug("导出进度: {Progress}%", percent);
                });
                
                var exportSettings = new ExportSettings
                {
                    Format = SelectedExportFormat,
                    OutputQuality = int.Parse(SelectedExportQuality),
                    FrameRate = int.Parse(SelectedFrameRate),
                    //OutputPath = outputFolder
                };
                
                _logger.Information("导出设置 - 格式: {Format}, 质量: {Quality}, 帧率: {FrameRate}", 
                    exportSettings.Format, exportSettings.OutputQuality, exportSettings.FrameRate);
                
                var successCount = 0;
                foreach (var segment in TimelineSegments)
                {
                    var outputFileName = $"{segment.Name}.{SelectedExportFormat.ToLower()}";
                    var outputPath = Path.Combine(outputFolder, outputFileName);
                    
                    _logger.Information("开始导出片段 {SegmentName} 到 {OutputPath}", segment.Name, outputPath);
                    
                    var success = await _videoService.ExportSegmentAsync(
                        VideoInfo.FilePath,
                        outputPath,
                        segment.StartTime,
                        segment.Duration,
                        exportSettings,
                        progress
                    );
                    
                    if (success)
                    {
                        successCount++;
                        _logger.Information("片段 {SegmentName} 导出成功", segment.Name);
                        
                        // 添加到最近导出列表
                        var recentExport = new RecentExport
                        {
                            FilePath = outputPath,
                            ExportTime = DateTime.Now,
                            Format = SelectedExportFormat,
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
                        
                        _logger.Debug("已添加到最近导出列表，当前列表大小: {RecentExportsCount}", RecentExports.Count);
                    }
                    else
                    {
                        _logger.Error("片段 {SegmentName} 导出失败", segment.Name);
                    }
                }
                
                _logger.Information("导出流程完成 - 成功: {SuccessCount}/{TotalCount}", successCount, TimelineSegments.Count);
                StatusMessage = $"导出完成: {successCount}/{TimelineSegments.Count} 个片段";
                
                // 保存最近导出列表
                SaveRecentExports();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "导出过程中发生异常");
                StatusMessage = $"导出失败: {ex.Message}";
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
                    OutputQuality = int.Parse(SelectedExportQuality),
                    FrameRate = int.Parse(SelectedFrameRate),
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

        [RelayCommand]
        private void SelectSegment(TimelineSegment segment)
        {
            _logger.Information("选择片段: {SegmentName}", segment.Name);
            
            // 清除之前的选择
            foreach (var seg in TimelineSegments)
            {
                seg.IsSelected = false;
            }
            
            // 设置新的选择
            segment.IsSelected = true;
            SelectedSegment = segment;
            
            // 跳转到片段开始时间
            CurrentTime = segment.StartTime;
            
            UpdateCommandStates();
            StatusMessage = $"已选择片段: {segment.Name}";
        }

        [RelayCommand]
        private void PlaySegment(TimelineSegment segment)
        {
            _logger.Information("播放片段: {SegmentName}", segment.Name);
            
            // 选择并播放片段
            SelectSegment(segment);
            
            // 开始播放
            if (!IsPlaying)
            {
                PlayPause();
            }
            
            StatusMessage = $"播放片段: {segment.Name}";
        }
        
        // 播放头位置改变事件处理
        public void OnPlayheadPositionChanged(TimeSpan newTime)
        {
            CurrentTime = newTime;
            
            // 如果正在播放，先暂停
            if (IsPlaying)
            {
                IsPlaying = false;
                PlayPauseIcon = "Play";
            }
        }
        
        // 切分点请求事件处理
        public void OnSplitPointRequested(TimeSpan splitTime)
        {
            // 在当前播放头位置添加切分点
            SplitVideoAtCurrentPosition();
        }
        
        // 在当前播放头位置切分视频
        private void SplitVideoAtCurrentPosition()
        {
            if (VideoInfo == null) return;
            
            var splitTime = CurrentTime;
            
            // 创建新的时间轴片段
            var newSegment = new TimelineSegment
            {
                Name = $"片段 {TimelineSegments.Count + 1}",
                StartTime = splitTime,
                EndTime = VideoInfo.Duration,
                CreatedDate = DateTime.Now
            };
            
            // 添加到时间轴
            TimelineSegments.Add(newSegment);
            
            // 更新项目信息
            UpdateProjectInfo();
            
            // 记录撤销操作
            var undoAction = new Action(() =>
            {
                TimelineSegments.Remove(newSegment);
                UpdateProjectInfo();
            });
            _undoStack.Push(undoAction);
            _redoStack.Clear();
            
            UpdateCommandStates();
        }

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
            _logger.Debug("当前时间更新: {CurrentTime}", value);
            
            // 更新时间显示
            UpdateTimeDisplay();
            UpdatePreciseTimeDisplay();
        }

        partial void OnVolumeChanged(double value)
        {
            // TODO: 实现音量控制
        }

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
        
        private void UpdateProjectInfo()
        {
            if (VideoInfo != null)
            {
                var oldSegmentCount = ProjectInfo.SegmentCount;
                var oldOutputDuration = ProjectInfo.OutputDuration;
                
                ProjectInfo.SegmentCount = TimelineSegments.Count;
                ProjectInfo.OutputDuration = TimeSpan.FromSeconds(TimelineSegments.Sum(s => s.Duration.TotalSeconds));
                
                // 估算文件大小（基于视频质量和时长）
                var totalSeconds = ProjectInfo.OutputDuration.TotalSeconds;
                var estimatedSizeMB = totalSeconds * 2; // 简单估算：每秒2MB
                ProjectInfo.EstimatedSize = $"{estimatedSizeMB:F1} MB";
                
                _logger.Debug("项目信息更新 - 片段数: {OldCount} -> {NewCount}, 输出时长: {OldDuration} -> {NewDuration}, 预估大小: {EstimatedSize}", 
                    oldSegmentCount, ProjectInfo.SegmentCount, oldOutputDuration, ProjectInfo.OutputDuration, ProjectInfo.EstimatedSize);
            }
        }
        
        private void UpdateCommandStates()
        {
            var oldCanUndo = CanUndo;
            var oldCanRedo = CanRedo;
            var oldCanDelete = CanDelete;
            var oldCanCopy = CanCopy;
            
            CanUndo = _undoStack.Count > 0;
            CanRedo = _redoStack.Count > 0;
            CanDelete = TimelineSegments.Any(s => s.IsSelected);
            CanCopy = TimelineSegments.Any(s => s.IsSelected);
            
            _logger.Debug("命令状态更新 - 撤销: {OldUndo} -> {NewUndo}, 重做: {OldRedo} -> {NewRedo}, 删除: {OldDelete} -> {NewDelete}, 复制: {OldCopy} -> {NewCopy}", 
                oldCanUndo, CanUndo, oldCanRedo, CanRedo, oldCanDelete, CanDelete, oldCanCopy, CanCopy);
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
    }
} 