using System.Text.Json.Serialization;

namespace Win115.Dtos
{
    public class OpenAuthDeviceCodeDTO
    {
        [JsonPropertyName("uid")]
        public string? Uid { get; set; }

        [JsonPropertyName("time")]
        public int Time { get; set; }

        [JsonPropertyName("qrcode")]
        public string? QrCode { get; set; }

        [JsonPropertyName("sign")]
        public string? Sign { get; set; }
    }
}
