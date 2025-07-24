using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using VideoCutTool.WPF.Models;
using Serilog;

namespace VideoCutTool.WPF.ViewModels
{
    public partial class AdvancedSettingsViewModel : ObservableObject
    {
        private readonly ILogger _logger;
        private readonly ExportSettings _originalSettings;

        public AdvancedSettingsViewModel(ExportSettings settings)
        {
            _logger = Log.ForContext<AdvancedSettingsViewModel>();
            _originalSettings = settings;
            
            // 初始化设置
            LoadSettings(settings);
            
            // 初始化选项列表
            InitializeOptions();
        }

        #region 选项列表

        [ObservableProperty]
        private ObservableCollection<string> _videoCodecs = new() { "h264", "h265", "vp9", "av1" };

        [ObservableProperty]
        private ObservableCollection<string> _audioCodecs = new() { "aac", "mp3", "opus", "flac" };

        [ObservableProperty]
        private ObservableCollection<string> _audioBitrates = new() { "64 kbps", "128 kbps", "192 kbps", "256 kbps", "320 kbps" };

        [ObservableProperty]
        private ObservableCollection<string> _audioSampleRates = new() { "22050 Hz", "44100 Hz", "48000 Hz", "96000 Hz" };

        [ObservableProperty]
        private ObservableCollection<string> _hardwareAccelerationOptions = new() { "无", "NVIDIA NVENC", "Intel QuickSync", "AMD VCE" };

        #endregion

        #region 设置属性

        [ObservableProperty]
        private string _selectedVideoCodec = "h264";

        [ObservableProperty]
        private string _selectedAudioCodec = "aac";

        [ObservableProperty]
        private string _selectedAudioBitrate = "128 kbps";

        [ObservableProperty]
        private string _selectedAudioSampleRate = "48000 Hz";

        [ObservableProperty]
        private string _selectedHardwareAcceleration = "无";

        [ObservableProperty]
        private int _bitrate = 5000;

        [ObservableProperty]
        private int _keyframeInterval = 2;

        [ObservableProperty]
        private bool _audioEnabled = true;

        [ObservableProperty]
        private bool _useGPUAcceleration = false;

        [ObservableProperty]
        private int _threadCount = Environment.ProcessorCount;

        #endregion

        private void InitializeOptions()
        {
            _logger.Debug("初始化高级设置选项");
        }

        private void LoadSettings(ExportSettings settings)
        {
            _logger.Debug("加载高级设置");
            
            SelectedVideoCodec = settings.VideoCodec;
            SelectedAudioCodec = settings.AudioCodec;
            Bitrate = settings.Bitrate;
            AudioEnabled = settings.AudioEnabled;
        }

        public void ResetToDefaults()
        {
            _logger.Information("重置高级设置为默认值");
            
            SelectedVideoCodec = "h264";
            SelectedAudioCodec = "aac";
            SelectedAudioBitrate = "128 kbps";
            SelectedAudioSampleRate = "48000 Hz";
            SelectedHardwareAcceleration = "无";
            Bitrate = 5000;
            KeyframeInterval = 2;
            AudioEnabled = true;
            UseGPUAcceleration = false;
            ThreadCount = Environment.ProcessorCount;
        }

        public void SaveSettings()
        {
            _logger.Information("保存高级设置");
            
            // 更新原始设置对象
            _originalSettings.VideoCodec = SelectedVideoCodec;
            _originalSettings.AudioCodec = SelectedAudioCodec;
            _originalSettings.Bitrate = Bitrate;
            _originalSettings.AudioEnabled = AudioEnabled;
        }

        public ExportSettings GetUpdatedSettings()
        {
            return _originalSettings;
        }
    }
} 