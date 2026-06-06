using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Win115.Dtos
{
    public class OpenAuthDeviceCodeDTO
    {
        [JsonProperty("uid")]
        public string? Uid { get; set; }

        [JsonProperty("time")]
        public int Time { get; set; }

        [JsonProperty("qrcode")]
        public string? QrCode { get; set; }

        [JsonProperty("sign")]
        public string? Sign { get; set; }
    }
}
