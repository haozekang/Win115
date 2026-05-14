using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Win115.Dtos
{
    public record OpenAuthDeviceCodeDTO
    {
        [JsonProperty("uid"), DefaultValue("")]
        public string? Uid { get; set; }

        [JsonProperty("time"), DefaultValue(0)]
        public int Time { get; set; }

        [JsonProperty("qrcode"), DefaultValue("")]
        public string? QrCode { get; set; }

        [JsonProperty("sign"), DefaultValue("")]
        public string? Sign { get; set; }
    }
}
