namespace VideoCutTool.WPF.Models
{
    public class VideoInfo
    {
        public string Name { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public string Resolution { get; set; } = string.Empty;
        public string FrameRate { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string Format { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
} 