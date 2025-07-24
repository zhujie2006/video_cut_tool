using VideoCutTool.Core.Models;

namespace VideoCutTool.Core.Interfaces
{
    /// <summary>
    /// 视频处理服务接口
    /// </summary>
    public interface IVideoService
    {
        /// <summary>
        /// 获取视频信息
        /// </summary>
        /// <param name="filePath">视频文件路径</param>
        /// <returns>视频信息</returns>
        Task<VideoInfo> GetVideoInfoAsync(string filePath);

        /// <summary>
        /// 分割视频
        /// </summary>
        /// <param name="inputPath">输入文件路径</param>
        /// <param name="startTime">开始时间（秒）</param>
        /// <param name="endTime">结束时间（秒）</param>
        /// <param name="outputPath">输出文件路径</param>
        /// <returns>输出文件路径</returns>
        Task<string> SplitVideoAsync(string inputPath, double startTime, double endTime, string outputPath);

        /// <summary>
        /// 导出视频片段
        /// </summary>
        /// <param name="inputPath">输入文件路径</param>
        /// <param name="outputPath">输出文件路径</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="duration">持续时间</param>
        /// <param name="settings">导出设置</param>
        /// <param name="progress">进度报告</param>
        /// <returns>是否成功</returns>
        Task<bool> ExportSegmentAsync(string inputPath, string outputPath, TimeSpan startTime, TimeSpan duration, ExportSettings settings, IProgress<int>? progress = null);

        /// <summary>
        /// 生成视频缩略图
        /// </summary>
        /// <param name="videoPath">视频文件路径</param>
        /// <param name="time">时间点（秒）</param>
        /// <returns>缩略图文件路径</returns>
        Task<string> GenerateThumbnailAsync(string videoPath, double time);

        /// <summary>
        /// 生成音频波形数据
        /// </summary>
        /// <param name="videoPath">视频文件路径</param>
        /// <returns>音频波形数据</returns>
        Task<List<double>> GenerateAudioWaveformAsync(string videoPath);
    }
}