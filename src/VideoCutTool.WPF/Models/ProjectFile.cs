using System.Text.Json.Serialization;

namespace VideoCutTool.WPF.Models
{
    public class ProjectFile
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0.0";
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("createdDate")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        [JsonPropertyName("modifiedDate")]
        public DateTime ModifiedDate { get; set; } = DateTime.Now;
        
        [JsonPropertyName("videoInfo")]
        public VideoInfo? VideoInfo { get; set; }
        
        [JsonPropertyName("videoSource")]
        public string VideoSource { get; set; } = string.Empty;
        
        [JsonPropertyName("timelineSegments")]
        public List<TimelineSegment> TimelineSegments { get; set; } = new();
        
        [JsonPropertyName("exportSettings")]
        public ExportSettings ExportSettings { get; set; } = new();
        
        [JsonPropertyName("currentTime")]
        public TimeSpan CurrentTime { get; set; }
        
        [JsonPropertyName("volume")]
        public double Volume { get; set; } = 50.0;
    }
} 