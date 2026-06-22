using System.Text.Json.Serialization;

namespace Win115.Dtos
{
    public class GetQeCodeStatusDTO
    {
        [JsonPropertyName("msg")]
        public string? Message { get; set; }

        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("version")]
        public string? Version { get; set; }
    }
}
