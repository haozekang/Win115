using Newtonsoft.Json;
using System.ComponentModel;

namespace Win115.Dtos
{
    public record OpenOfflineGetQuotaInfo
    {
        /// <summary>
        /// 用户总配额数量
        /// </summary>
        [JsonProperty("count"), DefaultValue(0)]
        public long? Count { get; set; }

        /// <summary>
        /// 用户总剩余配额数量
        /// </summary>
        [JsonProperty("surplus"), DefaultValue(0)]
        public long? Surplus { get; set; }
    }
}
