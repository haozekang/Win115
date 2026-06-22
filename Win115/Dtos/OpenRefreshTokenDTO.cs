using System.Text.Json.Serialization;

namespace Win115.Dtos
{
    public class OpenRefreshTokenDTO
    {
        /// <summary>
        /// 用于访问资源接口的凭证
        /// </summary>
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        /// <summary>
        /// 用于刷新 access_token，有效期1年
        /// </summary>
        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        /// <summary>
        /// access_token 有效期，单位秒
        /// </summary>
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
