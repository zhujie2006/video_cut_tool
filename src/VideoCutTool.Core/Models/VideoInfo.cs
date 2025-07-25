using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace VideoCutTool.Core.Models
{
    /// <summary>
    /// 视频信息模型
    /// </summary>
    public partial class VideoInfo : ObservableObject
    {
        /// <summary>
        /// 视频名称
        /// </summary>
        [JsonPropertyName("name")]
        [ObservableProperty]
        private string _name = string.Empty;
        
        /// <summary>
        /// 文件路径
        /// </summary>
        [JsonPropertyName("filePath")]
        [ObservableProperty]
        private string _filePath = string.Empty;
        
        /// <summary>
        /// 视频时长
        /// </summary>
        [JsonPropertyName("duration")]
        [ObservableProperty]
        private TimeSpan _duration = TimeSpan.Zero;

        /// <summary>
        /// 视频宽度
        /// </summary>
        [JsonPropertyName("width")]
        [ObservableProperty]
        private float _width = 0.0f;

        /// <summary>
        /// 视频高度
        /// </summary>
        [JsonPropertyName("height")]
        [ObservableProperty]
        private float _height = 0.0f;

        /// <summary>
        /// 分辨率
        /// </summary>
        [JsonPropertyName("resolution")]
        [ObservableProperty]
        private string _resolution = string.Empty;
        
        /// <summary>
        /// 帧率
        /// </summary>
        [JsonPropertyName("frameRate")]
        [ObservableProperty]
        private string _frameRate = string.Empty;

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        [JsonPropertyName("fileSize")]
        [ObservableProperty]
        private long _fileSize = 0;
        
        /// <summary>
        /// 文件格式
        /// </summary>
        [JsonPropertyName("format")]
        [ObservableProperty]
        private string _format = string.Empty;
        
        /// <summary>
        /// 创建日期
        /// </summary>
        [JsonPropertyName("createdDate")]
        [ObservableProperty]
        private DateTime _createdDate = DateTime.Now;

        /// <summary>
        /// 修改日期
        /// </summary>
        [JsonPropertyName("modifiedDate")]
        [ObservableProperty]
        private DateTime _modifiedDate = DateTime.Now;

        /// <summary>
        /// 视频编码器
        /// </summary>
        [JsonPropertyName("videoCodec")]
        [ObservableProperty]
        private string _videoCodec = string.Empty;

        /// <summary>
        /// 音频编码器
        /// </summary>
        [JsonPropertyName("audioCodec")]
        [ObservableProperty]
        private string _audioCodec = string.Empty;

        /// <summary>
        /// 比特率
        /// </summary>
        [JsonPropertyName("bitrate")]
        [ObservableProperty]
        private long _bitrate = 0;

        /// <summary>
        /// 音频采样率
        /// </summary>
        [JsonPropertyName("audioSampleRate")]
        [ObservableProperty]
        private int _audioSampleRate = 0;

        /// <summary>
        /// 音频通道数
        /// </summary>
        [JsonPropertyName("audioChannels")]
        [ObservableProperty]
        private int _audioChannels = 0;
    }
}