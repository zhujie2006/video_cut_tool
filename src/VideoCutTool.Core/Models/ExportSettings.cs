using System.Text.Json.Serialization;

namespace VideoCutTool.Core.Models
{
    /// <summary>
    /// 导出设置模型
    /// </summary>
    public class ExportSettings
    {
        /// <summary>
        /// 输出格式
        /// </summary>
        [JsonPropertyName("format")]
        public string Format { get; set; } = "MP4";

        /// <summary>
        /// 视频编码器
        /// </summary>
        [JsonPropertyName("videoCodec")]
        public string VideoCodec { get; set; } = "libx264";

        /// <summary>
        /// 音频编码器
        /// </summary>
        [JsonPropertyName("audioCodec")]
        public string AudioCodec { get; set; } = "aac";

        /// <summary>
        /// 分辨率宽度
        /// </summary>
        [JsonPropertyName("width")]
        public int Width { get; set; } = 1920;

        /// <summary>
        /// 分辨率高度
        /// </summary>
        [JsonPropertyName("height")]
        public int Height { get; set; } = 1080;

        /// <summary>
        /// 帧率
        /// </summary>
        [JsonPropertyName("frameRate")]
        public int FrameRate { get; set; } = 30;

        /// <summary>
        /// 视频比特率（kbps）
        /// </summary>
        [JsonPropertyName("videoBitrate")]
        public int VideoBitrate { get; set; } = 5000;

        /// <summary>
        /// 音频比特率（kbps）
        /// </summary>
        [JsonPropertyName("audioBitrate")]
        public int AudioBitrate { get; set; } = 128;

        /// <summary>
        /// 音频采样率
        /// </summary>
        [JsonPropertyName("audioSampleRate")]
        public int AudioSampleRate { get; set; } = 44100;

        /// <summary>
        /// 音频通道数
        /// </summary>
        [JsonPropertyName("audioChannels")]
        public int AudioChannels { get; set; } = 2;

        /// <summary>
        /// 是否使用硬件加速
        /// </summary>
        [JsonPropertyName("useHardwareAcceleration")]
        public bool UseHardwareAcceleration { get; set; } = true;

        /// <summary>
        /// 硬件加速类型
        /// </summary>
        [JsonPropertyName("hardwareAccelerationType")]
        public string HardwareAccelerationType { get; set; } = "auto";

        /// <summary>
        /// 质量预设
        /// </summary>
        [JsonPropertyName("qualityPreset")]
        public string QualityPreset { get; set; } = "medium";

        /// <summary>
        /// 是否保持原始分辨率
        /// </summary>
        [JsonPropertyName("maintainOriginalResolution")]
        public bool MaintainOriginalResolution { get; set; } = true;

        /// <summary>
        /// 是否保持原始帧率
        /// </summary>
        [JsonPropertyName("maintainOriginalFrameRate")]
        public bool MaintainOriginalFrameRate { get; set; } = true;

        /// <summary>
        /// 输出质量（0-100）
        /// </summary>
        [JsonPropertyName("outputQuality")]
        public int OutputQuality { get; set; } = 80;
    }
}