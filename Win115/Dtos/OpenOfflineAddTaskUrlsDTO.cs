using Newtonsoft.Json;
using System.ComponentModel;

namespace Win115.Dtos
{
    public record OpenOfflineAddTaskUrlsDTO
    {
        /// <summary>
        /// 链接任务添加状态，成功true；失败false
        /// </summary>
        [JsonProperty("state"), DefaultValue(false)]
        public bool? State { get; set; }

        /// <summary>
        /// 链接任务状态码，成功返回0
        /// </summary>
        [JsonProperty("code"), DefaultValue(0)]
        public long? Code { get; set; }

        /// <summary>
        /// 链接任务状态描述，成功返回空字符串
        /// </summary>
        [JsonProperty("message"), DefaultValue("")]
        public string? Message { get; set; }

        /// <summary>
        /// 链接任务sha1，只有任务成功的时候才会返回
        /// </summary>
        [JsonProperty("info_hash"), DefaultValue("")]
        public string? InfoHash { get; set; }

        /// <summary>
        /// 链接任务url
        /// </summary>
        [JsonProperty("url"), DefaultValue("")]
        public string? Url { get; set; }
    }
}
