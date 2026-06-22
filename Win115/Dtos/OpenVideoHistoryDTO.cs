using System.Text.Json.Serialization;

namespace Win115.Dtos
{
    public class OpenVideoHistoryDTO
    {
        /// <summary>
        /// 记录添加时间
        /// </summary>
        [JsonPropertyName("add_time")]
        public long? AddTime { get; set; }

        /// <summary>
        /// 文件id
        /// </summary>
        [JsonPropertyName("file_id")]
        public string? FileId { get; set; }

        /// <summary>
        /// 文件名称
        /// </summary>
        [JsonPropertyName("file_name")]
        public string? FileName { get; set; }

        /// <summary>
        /// 文件哈希值
        /// </summary>
        [JsonPropertyName("hash")]
        public string? Hash { get; set; }

        /// <summary>
        /// 文件提取码
        /// </summary>
        [JsonPropertyName("pick_code")]
        public string? PickCode { get; set; }

        /// <summary>
        /// 记录的已播放时长
        /// </summary>
        [JsonPropertyName("time")]
        public string? Time { get; set; }
    }
}
