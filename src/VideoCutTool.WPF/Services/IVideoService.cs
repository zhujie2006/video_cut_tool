using VideoCutTool.WPF.Models;

namespace VideoCutTool.WPF.Services
{
    public interface IVideoService
    {
        /// <summary>
        /// 获取视频信息
        /// </summary>
        Task<VideoInfo> GetVideoInfoAsync(string filePath);
        
        /// <summary>
        /// 分割视频
        /// </summary>
        Task<bool> SplitVideoAsync(string inputPath, string outputPath, TimeSpan startTime, TimeSpan duration);
        
        /// <summary>
        /// 导出视频片段
        /// </summary>
        Task<bool> ExportSegmentAsync(string inputPath, string outputPath, TimeSpan startTime, TimeSpan duration, ExportSettings settings, IProgress<int>? progress = null);
        
        /// <summary>
        /// 生成视频缩略图
        /// </summary>
        Task<string> GenerateThumbnailAsync(string videoPath, TimeSpan time, string outputPath);
    }
    
    public class ExportSettings
    {
        public string Format { get; set; } = "MP4";
        public string Quality { get; set; } = "高质量 (1080p)";
        public string FrameRate { get; set; } = "30 fps";
        public string OutputPath { get; set; } = string.Empty;
    }
} 