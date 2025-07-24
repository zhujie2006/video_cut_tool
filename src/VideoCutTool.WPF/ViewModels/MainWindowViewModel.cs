using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;
using VideoCutTool.WPF.Models;

namespace VideoCutTool.WPF.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
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

        // Commands
        [RelayCommand]
        private void ImportVideo()
        {
            StatusMessage = "导入视频功能待实现";
            // TODO: 实现视频导入功能
        }

        [RelayCommand]
        private void SplitVideo()
        {
            StatusMessage = "分割视频功能待实现";
            // TODO: 实现视频分割功能
        }

        [RelayCommand]
        private void DeleteSegment()
        {
            StatusMessage = "删除片段功能待实现";
            // TODO: 实现片段删除功能
        }

        [RelayCommand]
        private void CopySegment()
        {
            StatusMessage = "复制片段功能待实现";
            // TODO: 实现片段复制功能
        }

        [RelayCommand]
        private void Undo()
        {
            StatusMessage = "撤销功能待实现";
            // TODO: 实现撤销功能
        }

        [RelayCommand]
        private void Redo()
        {
            StatusMessage = "重做功能待实现";
            // TODO: 实现重做功能
        }

        [RelayCommand]
        private void PlayPause()
        {
            IsPlaying = !IsPlaying;
            PlayPauseIcon = IsPlaying ? "Pause" : "Play";
            StatusMessage = IsPlaying ? "播放中" : "已暂停";
            // TODO: 实现播放/暂停功能
        }

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
        private void SaveProject()
        {
            StatusMessage = "保存项目功能待实现";
            // TODO: 实现保存项目功能
        }

        [RelayCommand]
        private void Export()
        {
            StatusMessage = "导出功能待实现";
            // TODO: 实现导出功能
        }

        [RelayCommand]
        private void AdvancedSettings()
        {
            StatusMessage = "高级设置功能待实现";
            // TODO: 实现高级设置功能
        }

        partial void OnCurrentPositionChanged(double value)
        {
            CurrentTime = TimeSpan.FromSeconds(value);
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
    }
} 