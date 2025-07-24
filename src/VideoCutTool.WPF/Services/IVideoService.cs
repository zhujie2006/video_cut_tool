using VideoCutTool.WPF.Models;

namespace VideoCutTool.WPF.Services
{
    public interface IVideoService
    {
        Task<VideoInfo> GetVideoInfoAsync(string filePath);
        Task<string> SplitVideoAsync(string inputPath, double startTime, double endTime, string outputPath);
        Task<bool> ExportSegmentAsync(string inputPath, string outputPath, TimeSpan startTime, TimeSpan duration, ExportSettings settings, IProgress<int>? progress = null);
        Task<string> GenerateThumbnailAsync(string videoPath, double time);
        Task<List<double>> GenerateAudioWaveformAsync(string videoPath);
    }
} 