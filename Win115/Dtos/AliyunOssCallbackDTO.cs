using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Win115.Dtos
{
    public record AliyunOssCallbackDTO
    {
        [JsonProperty("callbackUrl"), DefaultValue("")]
        public string? CallbackUrl { get; set; }

        [JsonProperty("callbackBody"), DefaultValue("")]
        public string? CallbackBody { get; set; }
    }
}
