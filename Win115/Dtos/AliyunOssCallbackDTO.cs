using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Win115.Dtos
{
    public class AliyunOssCallbackDTO
    {
        [JsonProperty("callbackUrl")]
        public string? CallbackUrl { get; set; }

        [JsonProperty("callbackBody")]
        public string? CallbackBody { get; set; }
    }
}
