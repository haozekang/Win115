using Newtonsoft.Json;
using System.ComponentModel;

namespace Win115.Dtos
{
    public class OpenOfflineGetQuotaInfo
    {
        /// <summary>
        /// 用户总配额数量
        /// </summary>
        [JsonProperty("count")]
        public long? Count { get; set; }

        /// <summary>
        /// 用户总剩余配额数量
        /// </summary>
        [JsonProperty("surplus")]
        public long? Surplus { get; set; }
    }
}
