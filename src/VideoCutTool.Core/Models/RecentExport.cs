using System.Text.Json.Serialization;

namespace VideoCutTool.Core.Models
{
    /// <summary>
    /// 最近导出记录模型
    /// </summary>
    public class RecentExport
    {
        /// <summary>
        /// 导出文件路径
        /// </summary>
        [JsonPropertyName("filePath")]
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// 导出时间
        /// </summary>
        [JsonPropertyName("exportTime")]
        public DateTime ExportTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 导出格式
        /// </summary>
        [JsonPropertyName("format")]
        public string Format { get; set; } = string.Empty;

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        [JsonPropertyName("fileSize")]
        public long FileSize { get; set; }

        /// <summary>
        /// 导出设置
        /// </summary>
        [JsonPropertyName("exportSettings")]
        public ExportSettings ExportSettings { get; set; } = new();

        /// <summary>
        /// 导出状态
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = "成功";

        /// <summary>
        /// 错误信息（如果有）
        /// </summary>
        [JsonPropertyName("errorMessage")]
        public string ErrorMessage { get; set; } = string.Empty;
    }
}