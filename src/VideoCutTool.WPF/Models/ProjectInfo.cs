namespace VideoCutTool.WPF.Models
{
    public class ProjectInfo
    {
        public string Name { get; set; } = "未命名项目";
        public TimeSpan TotalDuration { get; set; }
        public int SegmentCount { get; set; }
        public TimeSpan OutputDuration { get; set; }
        public string EstimatedSize { get; set; } = "0 MB";
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime LastModifiedDate { get; set; } = DateTime.Now;
        public string ProjectPath { get; set; } = string.Empty;
    }
} 