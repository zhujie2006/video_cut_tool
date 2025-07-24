using System.Text.Json.Serialization;

namespace VideoCutTool.WPF.Models
{
    public class ExportSettings
    {
        [JsonPropertyName("format")]
        public string Format { get; set; } = "MP4";
        
        [JsonPropertyName("quality")]
        public string Quality { get; set; } = "高质量 (1080p)";
        
        [JsonPropertyName("frameRate")]
        public string FrameRate { get; set; } = "30 fps";
        
        [JsonPropertyName("outputPath")]
        public string OutputPath { get; set; } = string.Empty;
        
        [JsonPropertyName("bitrate")]
        public int Bitrate { get; set; } = 5000; // kbps
        
        [JsonPropertyName("audioEnabled")]
        public bool AudioEnabled { get; set; } = true;
        
        [JsonPropertyName("videoCodec")]
        public string VideoCodec { get; set; } = "h264";
        
        [JsonPropertyName("audioCodec")]
        public string AudioCodec { get; set; } = "aac";
    }
} 