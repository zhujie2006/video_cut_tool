using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace VideoCutTool.Core.Models
{
    /// <summary>
    /// 时间轴片段模型
    /// </summary>
    public partial class TimelineSegment : ObservableObject
    {
        /// <summary>
        /// 片段名称
        /// </summary>
        [ObservableProperty]
        [JsonPropertyName("name")]
        private string _name = string.Empty;
        
        /// <summary>
        /// 开始时间
        /// </summary>
        [ObservableProperty]
        [JsonPropertyName("startTime")]
        private TimeSpan _startTime;
        
        /// <summary>
        /// 结束时间
        /// </summary>
        [ObservableProperty]
        [JsonPropertyName("endTime")]
        private TimeSpan _endTime;
        
        /// <summary>
        /// 缩略图路径
        /// </summary>
        [ObservableProperty]
        [JsonPropertyName("thumbnailPath")]
        private string _thumbnailPath = string.Empty;
        
        /// <summary>
        /// 是否被选中
        /// </summary>
        [ObservableProperty]
        [JsonPropertyName("isSelected")]
        private bool _isSelected;
        
        /// <summary>
        /// 是否被删除
        /// </summary>
        [ObservableProperty]
        [JsonPropertyName("isDeleted")]
        private bool _isDeleted;
        
        /// <summary>
        /// 创建日期
        /// </summary>
        [ObservableProperty]
        [JsonPropertyName("createdDate")]
        private DateTime _createdDate = DateTime.Now;

        /// <summary>
        /// 片段ID
        /// </summary>
        [JsonPropertyName("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 片段持续时间
        /// </summary>
        [JsonPropertyName("duration")]
        public TimeSpan Duration => EndTime - StartTime;

        /// <summary>
        /// 片段描述
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 标签
        /// </summary>
        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();
    }
}