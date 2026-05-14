using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Win115.Dtos
{
    public record OpenRefreshTokenDTO
    {
        /// <summary>
        /// 用于访问资源接口的凭证
        /// </summary>
        [JsonProperty("access_token"), DefaultValue("")]
        public string? AccessToken { get; set; }

        /// <summary>
        /// 用于刷新 access_token，有效期1年
        /// </summary>
        [JsonProperty("refresh_token"), DefaultValue("")]
        public string? RefreshToken { get; set; }

        /// <summary>
        /// access_token 有效期，单位秒
        /// </summary>
        [JsonProperty("expires_in"), DefaultValue(0)]
        public int ExpiresIn { get; set; }
    }
}
