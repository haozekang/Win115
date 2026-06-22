using System.Text.Json.Serialization;

namespace Win115.Dtos
{
    public class ProResponseDTO<T>
    {
        [JsonPropertyName("state")]
        public bool State { get; set; }

        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("data")]
        public T? Data { get; set; }
    }

    public class ProResponseDTO
    {
        [JsonPropertyName("state")]
        public bool State { get; set; }

        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}
