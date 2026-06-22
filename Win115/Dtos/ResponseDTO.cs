using System.Text.Json.Serialization;

namespace Win115.Dtos
{
    public class ResponseDTO
    {
        [JsonPropertyName("state")]
        public int State { get; set; }

        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }

    public class ResponseDTO<T>
    {
        [JsonPropertyName("state")]
        public int State { get; set; }

        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("errno")]
        public int ErrNo { get; set; }

        [JsonPropertyName("data")]
        public T? Data { get; set; }
    }
}
