namespace VideoCutTool.WPF.Models
{
    public class TimelineSegment
    {
        public string Name { get; set; } = string.Empty;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public string ThumbnailPath { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
} 