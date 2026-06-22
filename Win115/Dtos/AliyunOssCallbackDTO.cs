using System.Text.Json.Serialization;

namespace Win115.Dtos
{
    public class AliyunOssCallbackDTO
    {
        [JsonPropertyName("callbackUrl")]
        public string? CallbackUrl { get; set; }

        [JsonPropertyName("callbackBody")]
        public string? CallbackBody { get; set; }
    }
}
