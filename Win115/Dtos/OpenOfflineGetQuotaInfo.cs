using System.Text.Json.Serialization;

namespace Win115.Dtos
{
    public class OpenOfflineGetQuotaInfo
    {
        /// <summary>
        /// 用户总配额数量
        /// </summary>
        [JsonPropertyName("count")]
        public long? Count { get; set; }

        /// <summary>
        /// 用户总剩余配额数量
        /// </summary>
        [JsonPropertyName("surplus")]
        public long? Surplus { get; set; }
    }
}
