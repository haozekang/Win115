using System.Text.Json.Serialization;

namespace Win115.Dtos
{
    public class OpenUploadGetTokenDTO
    {
        /// <summary>
        /// 上传域名
        /// </summary>
        [JsonPropertyName("endpoint")]
        public string? Endpoint { get; set; }

        /// <summary>
        /// 上传凭证-密钥
        /// </summary>
        [JsonPropertyName("AccessKeySecret")]
        public string? AccessKeySecret { get; set; }

        /// <summary>
        /// 上传凭证-token
        /// </summary>
        [JsonPropertyName("SecurityToken")]
        public string? SecurityToken { get; set; }

        /// <summary>
        /// 上传凭证-过期日期
        /// </summary>
        [JsonPropertyName("Expiration")]
        public string? Expiration { get; set; }

        /// <summary>
        /// 上传凭证-ID
        /// </summary>
        [JsonPropertyName("AccessKeyId")]
        public string? AccessKeyId { get; set; }
    }
}
