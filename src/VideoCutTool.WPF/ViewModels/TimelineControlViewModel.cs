using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using VideoCutTool.Core.Interfaces;
using VideoCutTool.Core.Models;

namespace VideoCutTool.WPF.ViewModels
{
    public partial class TimelineControlViewModel : ObservableObject
    {
        private readonly IVideoService _videoService;
        private readonly ILogger _logger;
        /// <summary>
        /// 主界面的通知处理器
        /// </summary>
        public IMainPageNotifyHandler UINotifier { get; set; }

        /// <summary>
        /// 当前控件的通知处理器
        /// </summary>
        public ITimelineControlNotifyHandler ControlNotifier { get; set; }

        private readonly Stack<Action> _undoStack = new();
        private readonly Stack<Action> _redoStack = new();

        // 缩略图缓存
        private Dictionary<string, string> _thumbnailCache = new();
        
        public TimelineControlViewModel(IVideoService videoService)
        {
            _videoService = videoService;
            _logger = Log.ForContext<TimelineControlViewModel>();
            
            _logger.Information("TimelineControlViewModel 初始化完成");
        }

        #region 属性

        // 视频信息
        [ObservableProperty]
        private VideoInfo? _currentVideo;

        [ObservableProperty]
        private double _zoomLevel = 1.0; // 缩放倍数：1, 10, 20, 30, ..., 100

        // 新增属性
        [ObservableProperty]
        private bool _canUndo = false;

        [ObservableProperty]
        private bool _canRedo = false;

        [ObservableProperty]
        private bool _canDelete = false;

        [ObservableProperty]
        private bool _canClear = false;

        [ObservableProperty]
        private TimelineSegment? _selectedSegment;

        [ObservableProperty]
        private ObservableCollection<TimelineSegment> _timelineSegments = new();

        [ObservableProperty]
        private double _totalWidth = 1000.0;

        #region 计算属性

        public double PixelsPerSecond => SettingViewModel.PIXELS_PER_SECOND * _zoomLevel; // 4.6 × 缩放倍数

        public double ThumbnailTimeInterval => SettingViewModel.THUMBNAIL_WIDTH / PixelsPerSecond;

        #endregion

        #endregion

        #region 公共方法

        public async Task LoadProjectAsync(ProjectInfo projectInfo)
        {
            if (projectInfo == null)
            {
                _logger.Warning("尝试加载项目但ProjectInfo为null");
                return;
            }
            
            _logger.Information($"开始加载项目: {projectInfo.Name}");
            
            try
            {
                // 设置视频信息
                CurrentVideo = projectInfo.VideoInfo;
                
                // 设置时间轴片段
                TimelineSegments = new ObservableCollection<TimelineSegment>(projectInfo.TimelineSegments);

                // 更新状态消息
                UINotifier.NotifyStatusMessage($"已加载项目: {projectInfo.Name}");

                UpdateContentAsync();
                _logger.Information("项目加载完成");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "加载项目时发生错误");
            }
        }

        public async Task UpdateContentAsync()
        {
            if (CurrentVideo == null)
            {
                _logger.Warning("尝试更新时间轴内容但CurrentVideo为null");
                return;
            }
            
            _logger.Debug($"开始更新时间轴内容，缩放级别: {ZoomLevel}");
            
            try
            {

                // 如果没有片段，创建完整视频片段
                if (TimelineSegments.Count == 0 && CurrentVideo != null)
                {
                    CreateFullVideoSegment();
                }
                // 重新计算时间轴宽度
                var duration = CurrentVideo.Duration.TotalSeconds;
                TotalWidth = duration * PixelsPerSecond * ZoomLevel;
                
                _logger.Debug($"更新后时间轴宽度: {TotalWidth}px, 像素/秒: {PixelsPerSecond}");

                // 触发内容更新
                ControlNotifier.RefreshControlContent();

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
            
            UINotifier.SetCurrentTime(time);
        }
        
        public async Task<string?> GetThumbnailPathAsync(double time)
        {
            if (CurrentVideo == null)
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
                var thumbnailPath = await _videoService.GenerateThumbnailAsync(CurrentVideo.FilePath, time);
                
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

        /// <summary>
        /// 更新命令状态
        /// </summary>
        public void UpdateCommandStates()
        {
            var oldCanUndo = CanUndo;
            var oldCanRedo = CanRedo;
            var oldCanDelete = CanDelete;
            var oldCanClear = CanClear;

            CanUndo = _undoStack.Count > 0;
            CanRedo = _redoStack.Count > 0;
            CanDelete = SelectedSegment != null; // 检查是否有选中的片段
            CanClear = TimelineSegments.Count > 1; // 检查是否有多个片段（除了完整视频片段）

            _logger.Debug("命令状态更新 - 撤销: {OldUndo} -> {NewUndo}, 重做: {OldRedo} -> {NewRedo}, 删除: {OldDelete} -> {NewDelete}, 清空: {OldClear} -> {NewClear}",
                oldCanUndo, CanUndo, oldCanRedo, CanRedo, oldCanDelete, CanDelete, oldCanClear, CanClear);
        }

        private void CreateFullVideoSegment()
        {
            if (CurrentVideo == null) return;

            var fullSegment = new TimelineSegment
            {
                Name = CurrentVideo.Name,
                StartTime = TimeSpan.Zero,
                EndTime = CurrentVideo.Duration,
                CreatedDate = DateTime.Now,
                IsDeleted = false
            };

            TimelineSegments.Add(fullSegment);
            _logger.Information($"创建完整视频片段: {fullSegment.Name} (0:00 - {CurrentVideo.Duration:mm\\:ss})");
        }

        public void ClearAllSegments()
        {
            // 清空所有片段
            TimelineSegments.Clear();
            
            // 重新创建完整视频片段
            CreateFullVideoSegment();
            
            _logger.Information("清空所有片段并恢复完整视频片段");
            UINotifier.NotifyStatusMessage("已清空所有片段并恢复完整视频");
            UpdateCommandStates();
            ControlNotifier.RefreshSegmentAndSplitPoints();
        }

        #endregion

        #region 私有方法

        private string GetThumbnailCacheKey(double time)
        {
            var videoFileName = System.IO.Path.GetFileNameWithoutExtension(CurrentVideo?.FilePath ?? "");
            var timeKey = time.ToString("F1").Replace(".", "_");
            return $"{videoFileName}_{timeKey}s";
        }

        #endregion

        #region 命令

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

                UINotifier.NotifyStatusMessage("已撤销操作");
            }
            else
            {
                _logger.Debug("没有可撤销的操作");
                UINotifier.NotifyStatusMessage("没有可撤销的操作");
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

                // TODO: 更新状态消息到主界面
                UINotifier?.NotifyStatusMessage("已重做操作");
            }
            else
            {
                _logger.Debug("没有可重做的操作");
                // TODO: 更新状态消息到主界面
                UINotifier?.NotifyStatusMessage("没有可重做的操作");
            }
        }

        
        public void AddSegmentAtTime(TimeSpan time)
        {
            try
            {
                if (CurrentVideo == null)
                {
                    _logger.Warning("尝试切分视频但未导入视频");
                    UINotifier.NotifyStatusMessage("请先导入视频");
                    return;
                }

                // 检查时间是否在有效范围内
                if (time <= TimeSpan.Zero || time >= CurrentVideo.Duration)
                {
                    _logger.Warning($"切分时间超出范围: {time:mm\\:ss\\.f}");
                    UINotifier.NotifyStatusMessage("切分时间超出视频范围");
                    return;
                }

                // 查找包含当前时间的片段
                var existingSegment = TimelineSegments.FirstOrDefault(s => 
                    s.StartTime <= time && 
                    s.EndTime > time);

                if (existingSegment == null)
                {
                    _logger.Warning($"未找到包含时间 {time:mm\\:ss\\.f} 的片段");
                    UINotifier.NotifyStatusMessage("未找到包含该时间的片段");
                    return;
                }

                // 切分片段：删除原片段，创建两个新片段
                var originalStartTime = existingSegment.StartTime;
                var originalEndTime = existingSegment.EndTime;
                var originalName = existingSegment.Name;

                // 删除原片段
                TimelineSegments.Remove(existingSegment);

                // 创建第一个片段（从原开始时间到切分时间）
                var firstSegment = new TimelineSegment
                {
                    Name = $"{CurrentVideo.Name}_{originalStartTime:hh\\:mm\\:ss\\.ff}",
                    StartTime = originalStartTime,
                    EndTime = time,
                    CreatedDate = DateTime.Now,
                    IsDeleted = false
                };

                // 创建第二个片段（从切分时间到原结束时间）
                var secondSegment = new TimelineSegment
                {
                    Name = $"{CurrentVideo.Name}_{time:hh\\:mm\\:ss\\.ff}",
                    StartTime = time,
                    EndTime = originalEndTime,
                    CreatedDate = DateTime.Now,
                    IsDeleted = false
                };

                TimelineSegments.Add(firstSegment);
                TimelineSegments.Add(secondSegment);
                UINotifier.UpdateProjectInfo();

                _logger.Information($"切分片段成功: {originalName} -> {firstSegment.Name} + {secondSegment.Name}");
                UINotifier.NotifyStatusMessage($"已切分片段: {firstSegment.Name} + {secondSegment.Name}");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "切分片段时发生异常");
                UINotifier.NotifyStatusMessage($"切分片段失败: {ex.Message}");
            }
        }

        public void RemoveSegmentAtTime(TimeSpan curTime)
        {
            var segmentToRemove = TimelineSegments.FirstOrDefault(s => 
                !s.IsDeleted && 
                s.StartTime <= curTime && 
                s.EndTime > curTime);

            if (segmentToRemove != null)
            {
                // 直接删除片段（硬删除）
                TimelineSegments.Remove(segmentToRemove);
                _logger.Information($"删除片段: {segmentToRemove.Name}");
                UINotifier.NotifyStatusMessage($"已删除片段: {segmentToRemove.Name}");
                UINotifier.UpdateProjectInfo();
            }
            else
            {
                _logger.Warning($"未找到要删除的片段: {curTime:mm\\:ss\\.f}");
                UINotifier.NotifyStatusMessage("未找到指定的片段");
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

            UpdateCommandStates();
            UINotifier.NotifyStatusMessage($"已选择片段: {segment.Name}");
        }

        [RelayCommand]
        private void DeleteSegment()
        {
            _logger.Information("开始删除片段流程");

            var selectedSegment = TimelineSegments.FirstOrDefault(s => s.IsSelected && !s.IsDeleted);
            if (selectedSegment == null)
            {
                _logger.Warning("尝试删除片段但未选择任何片段");
                UINotifier.NotifyStatusMessage("请先选择要删除的片段");
                return;
            }

            _logger.Information("准备删除片段 - 名称: {SegmentName}, 开始时间: {StartTime}, 结束时间: {EndTime}",
                selectedSegment.Name, selectedSegment.StartTime, selectedSegment.EndTime);

            var segmentIndex = TimelineSegments.IndexOf(selectedSegment);
            var segmentToRestore = selectedSegment;

            // 保存撤销操作
            _undoStack.Push(() =>
            {
                TimelineSegments.Insert(segmentIndex, segmentToRestore);
                UINotifier.UpdateProjectInfo();
                _logger.Debug("撤销操作：恢复片段 {SegmentName} 到位置 {SegmentIndex}", segmentToRestore.Name, segmentIndex);
            });

            // 直接删除片段
            TimelineSegments.Remove(selectedSegment);
            UINotifier.UpdateProjectInfo();

            _logger.Information("片段删除成功");
            UINotifier.NotifyStatusMessage($"已删除片段: {selectedSegment.Name}");

            // 刷新时间轴、缩略图、音频、视频片段
            ControlNotifier.RefreshControlContent();
        }

        [RelayCommand]
        private void CopySegment()
        {
            _logger.Information("开始复制片段流程");

            var selectedSegment = TimelineSegments.FirstOrDefault(s => s.IsSelected);
            if (selectedSegment == null)
            {
                _logger.Warning("尝试复制片段但未选择任何片段");
                UINotifier.NotifyStatusMessage("请先选择要复制的片段");
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
                    UINotifier.UpdateProjectInfo();
                    _logger.Debug("撤销操作：删除复制的片段 {SegmentIndex}", segmentIndex);
                }
            });

            UINotifier.UpdateProjectInfo();
            _logger.Information("片段复制成功，当前片段数量: {SegmentCount}", TimelineSegments.Count);
            UINotifier.NotifyStatusMessage($"已复制片段: {copiedSegment.Name}");
        }

        [RelayCommand]
        private void ClearSegment()
        {
            _logger.Information("开始清空所有片段流程");

            var activeSegments = TimelineSegments.Where(s => !s.IsDeleted).ToList();
            if (activeSegments.Count == 0)
            {
                _logger.Warning("尝试清空片段但当前没有活跃片段");
                UINotifier.NotifyStatusMessage("当前没有活跃片段");
                return;
            }

            // 保存撤销操作
            var segmentsToRestore = activeSegments.ToList();
            _undoStack.Push(() =>
            {
                // 清空当前片段
                TimelineSegments.Clear();
                // 恢复之前的片段
                foreach (var segment in segmentsToRestore)
                {
                    TimelineSegments.Add(segment);
                }
                _logger.Debug("撤销操作：恢复 {Count} 个片段", segmentsToRestore.Count);
                UpdateCommandStates();
                UINotifier.UpdateProjectInfo();
                ControlNotifier.RefreshSegmentAndSplitPoints();
            });

            ClearAllSegments();
            ControlNotifier.RefreshSegmentAndSplitPoints();

            _logger.Information("清空所有片段成功");
            UINotifier.NotifyStatusMessage("已清空所有片段并恢复完整视频");
        }

        [RelayCommand]
        private void SplitVideo()
        {
            _logger.Information("开始创建新片段流程");

            if (CurrentVideo == null)
            {
                _logger.Warning("尝试创建片段但未导入视频");
                UINotifier.NotifyStatusMessage("请先导入视频");
                return;
            }

            var curTime = UINotifier.GetCurrentTime();
            // 在当前位置添加新片段
            AddSegmentAtTime(curTime);

            // 保存撤销操作
            var segmentTime = curTime;
            _undoStack.Push(() =>
            {
                // TODO: 恢复原来的片段，删除新增的两个片段
                _logger.Debug("撤销操作：移除片段 {SegmentTime}", segmentTime);
                UpdateCommandStates();
                ControlNotifier.RefreshSegmentAndSplitPoints();
            });

            UpdateCommandStates();
            ControlNotifier.RefreshSegmentAndSplitPoints();
        }


        #endregion
    }
} 