using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VideoCutTool.Core.Models
{
    /// <summary>
    /// 项目信息模型
    /// </summary>
    public partial class ProjectInfo : ObservableObject
    {
        /// <summary>
        /// 项目名称
        /// </summary>
        [JsonPropertyName("name")]
        [ObservableProperty]
        private string _name = "新项目";

        /// <summary>
        /// 项目文件路径
        /// </summary>
        [JsonPropertyName("projectFilePath")]
        [ObservableProperty]
        private string _projectFilePath = string.Empty;

        /// <summary>
        /// 时长
        /// </summary>
        [JsonPropertyName("duration")]
        [ObservableProperty]
        private TimeSpan _duration = TimeSpan.Zero;

        /// <summary>
        /// 输出时长
        /// </summary>
        [ObservableProperty]
        private TimeSpan _outputDuration = TimeSpan.Zero;

        /// <summary>
        /// 预估大小
        /// </summary>
        [JsonPropertyName("estimatedSize")]
        [ObservableProperty]
        private string _estimatedSize = "";

        /// <summary>
        /// 视频信息
        /// </summary>
        [JsonPropertyName("videoInfo")]
        [ObservableProperty]
        private VideoInfo? _videoInfo;

        /// <summary>
        /// 音量
        /// </summary>
        [JsonPropertyName("volume")]
        [ObservableProperty]
        private double _volume = 50.0; // 音量，范围0.0到100.0

        /// <summary>
        /// 时间轴片段列表
        /// </summary>
        [JsonPropertyName("timelineSegments")]
        [ObservableProperty]
        private List<TimelineSegment> _timelineSegments = new();

        /// <summary>
        /// 切分点列表
        /// </summary>
        [JsonPropertyName("timelineSegments")]
        [ObservableProperty]
        private List<TimeSpan> _splitPoints = new();

        /// <summary>
        /// 缩略图缓存路径
        /// </summary>
        [JsonPropertyName("thumbnailCachePath")]
        [ObservableProperty]
        private string _thumbnailCachePath = string.Empty;

        /// <summary>
        /// 导出设置
        /// </summary>
        [JsonPropertyName("exportSettings")]
        [ObservableProperty]
        private ExportSettings _exportSettings = new();

        /// <summary>
        /// 创建日期
        /// </summary>
        [JsonPropertyName("createdDate")]
        [ObservableProperty]
        private DateTime _createdDate = DateTime.Now;

        /// <summary>
        /// 最后修改日期
        /// </summary>
        [JsonPropertyName("lastModifiedDate")]
        [ObservableProperty]
        private DateTime _lastModifiedDate = DateTime.Now;
    }
}