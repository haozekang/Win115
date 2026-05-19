using Newtonsoft.Json;
using System.Collections.Generic;

namespace Win115.Dtos
{
    public record OpenUploadGetTokenDTO
    {
        /// <summary>
        /// 上传域名
        /// </summary>
        [JsonProperty("endpoint")]
        public string? Endpoint { get; set; }

        /// <summary>
        /// 上传凭证-密钥
        /// </summary>
        [JsonProperty("AccessKeySecret")]
        public string? AccessKeySecret { get; set; }

        /// <summary>
        /// 上传凭证-token
        /// </summary>
        [JsonProperty("SecurityToken")]
        public string? SecurityToken { get; set; }

        /// <summary>
        /// 上传凭证-过期日期
        /// </summary>
        [JsonProperty("Expiration")]
        public string? Expiration { get; set; }

        /// <summary>
        /// 上传凭证-ID
        /// </summary>
        [JsonProperty("AccessKeyId")]
        public string? AccessKeyId { get; set; }
    }
}
