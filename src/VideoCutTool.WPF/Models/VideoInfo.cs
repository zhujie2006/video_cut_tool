using System.Text.Json.Serialization;

namespace VideoCutTool.WPF.Models
{
    public class VideoInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("filePath")]
        public string FilePath { get; set; } = string.Empty;
        
        [JsonPropertyName("duration")]
        public TimeSpan Duration { get; set; }
        
        [JsonPropertyName("resolution")]
        public string Resolution { get; set; } = string.Empty;
        
        [JsonPropertyName("frameRate")]
        public string FrameRate { get; set; } = string.Empty;
        
        [JsonPropertyName("fileSize")]
        public long FileSize { get; set; }
        
        [JsonPropertyName("format")]
        public string Format { get; set; } = string.Empty;
        
        [JsonPropertyName("createdDate")]
        public DateTime CreatedDate { get; set; }
        
        [JsonPropertyName("modifiedDate")]
        public DateTime ModifiedDate { get; set; }
    }
} 