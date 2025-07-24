using System.Text.Json.Serialization;

namespace VideoCutTool.Core.Models
{
    /// <summary>
    /// 项目文件模型
    /// </summary>
    public class ProjectFile
    {
        /// <summary>
        /// 项目信息
        /// </summary>
        [JsonPropertyName("projectInfo")]
        public ProjectInfo ProjectInfo { get; set; } = new();

        /// <summary>
        /// 视频信息
        /// </summary>
        [JsonPropertyName("videoInfo")]
        public VideoInfo? VideoInfo { get; set; }

        public TimeSpan CurrentTime { get; set; }

        public double Volume { get; set; } = 100.0; // 音量，范围0.0到100.0

        /// <summary>
        /// 时间轴片段列表
        /// </summary>
        [JsonPropertyName("timelineSegments")]
        public List<TimelineSegment> TimelineSegments { get; set; } = new();

        /// <summary>
        /// 导出设置
        /// </summary>
        [JsonPropertyName("exportSettings")]
        public ExportSettings ExportSettings { get; set; } = new();

        /// <summary>
        /// 最近导出记录
        /// </summary>
        [JsonPropertyName("recentExports")]
        public List<RecentExport> RecentExports { get; set; } = new();

        /// <summary>
        /// 项目文件版本
        /// </summary>
        [JsonPropertyName("fileVersion")]
        public string FileVersion { get; set; } = "1.0.0";

        /// <summary>
        /// 创建时间
        /// </summary>
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 最后保存时间
        /// </summary>
        [JsonPropertyName("lastSavedAt")]
        public DateTime LastSavedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 项目文件路径
        /// </summary>
        [JsonPropertyName("projectFilePath")]
        public string ProjectFilePath { get; set; } = string.Empty;

        /// <summary>
        /// 缩略图缓存路径
        /// </summary>
        [JsonPropertyName("thumbnailCachePath")]
        public string ThumbnailCachePath { get; set; } = string.Empty;

        /// <summary>
        /// 音频波形缓存路径
        /// </summary>
        [JsonPropertyName("waveformCachePath")]
        public string WaveformCachePath { get; set; } = string.Empty;
    }
}