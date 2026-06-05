using Newtonsoft.Json;
using System.ComponentModel;

namespace Win115.Dtos
{
    public record ResponseDTO
    {
        [JsonProperty("state")]
        public int State { get; set; }

        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("message")]
        public string? Message { get; set; }
    }

    public record ResponseDTO<T>
    {
        [JsonProperty("state")]
        public int State { get; set; }

        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("message")]
        public string? Message { get; set; }

        [JsonProperty("error")]
        public string? Error { get; set; }

        [JsonProperty("errno")]
        public int ErrNo { get; set; }

        [JsonProperty("data")]
        public T? Data { get; set; }
    }
}
