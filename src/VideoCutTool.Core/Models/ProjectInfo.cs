using System.Text.Json.Serialization;

namespace VideoCutTool.Core.Models
{
    /// <summary>
    /// 项目信息模型
    /// </summary>
    public class ProjectInfo
    {
        /// <summary>
        /// 项目名称
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = "新项目";

        /// <summary>
        /// 时长
        /// </summary>
        [JsonPropertyName("duration")]
        public TimeSpan Duration { get; set; } = TimeSpan.Zero;

        public TimeSpan OutputDuration { get; set; } = TimeSpan.Zero;

        public string EstimatedSize = "";

        public int SegmentCount { get; set; } = 0;

        /// <summary>
        /// 项目描述
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 创建日期
        /// </summary>
        [JsonPropertyName("createdDate")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// 最后修改日期
        /// </summary>
        [JsonPropertyName("lastModifiedDate")]
        public DateTime LastModifiedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// 项目版本
        /// </summary>
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// 作者
        /// </summary>
        [JsonPropertyName("author")]
        public string Author { get; set; } = string.Empty;

        /// <summary>
        /// 标签
        /// </summary>
        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// 项目设置
        /// </summary>
        [JsonPropertyName("settings")]
        public Dictionary<string, object> Settings { get; set; } = new();
    }
}