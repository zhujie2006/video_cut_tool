namespace VideoCutTool.WPF.Models
{
    public class RecentExport
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string ExportTime { get; set; } = string.Empty;
        public DateTime ExportDate { get; set; }
        public long FileSize { get; set; }
    }
} 