using System.Text.Json.Serialization;

namespace VideoCutTool.Core.Models
{
    /// <summary>
    /// 视频信息模型
    /// </summary>
    public class VideoInfo
    {
        /// <summary>
        /// 视频名称
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// 文件路径
        /// </summary>
        [JsonPropertyName("filePath")]
        public string FilePath { get; set; } = string.Empty;
        
        /// <summary>
        /// 视频时长
        /// </summary>
        [JsonPropertyName("duration")]
        public TimeSpan Duration { get; set; }
        
        /// <summary>
        /// 分辨率
        /// </summary>
        [JsonPropertyName("resolution")]
        public string Resolution { get; set; } = string.Empty;
        
        /// <summary>
        /// 帧率
        /// </summary>
        [JsonPropertyName("frameRate")]
        public string FrameRate { get; set; } = string.Empty;
        
        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        [JsonPropertyName("fileSize")]
        public long FileSize { get; set; }
        
        /// <summary>
        /// 文件格式
        /// </summary>
        [JsonPropertyName("format")]
        public string Format { get; set; } = string.Empty;
        
        /// <summary>
        /// 创建日期
        /// </summary>
        [JsonPropertyName("createdDate")]
        public DateTime CreatedDate { get; set; }
        
        /// <summary>
        /// 修改日期
        /// </summary>
        [JsonPropertyName("modifiedDate")]
        public DateTime ModifiedDate { get; set; }

        /// <summary>
        /// 视频编码器
        /// </summary>
        [JsonPropertyName("videoCodec")]
        public string VideoCodec { get; set; } = string.Empty;

        /// <summary>
        /// 音频编码器
        /// </summary>
        [JsonPropertyName("audioCodec")]
        public string AudioCodec { get; set; } = string.Empty;

        /// <summary>
        /// 比特率
        /// </summary>
        [JsonPropertyName("bitrate")]
        public long Bitrate { get; set; }

        /// <summary>
        /// 音频采样率
        /// </summary>
        [JsonPropertyName("audioSampleRate")]
        public int AudioSampleRate { get; set; }

        /// <summary>
        /// 音频通道数
        /// </summary>
        [JsonPropertyName("audioChannels")]
        public int AudioChannels { get; set; }
    }
}