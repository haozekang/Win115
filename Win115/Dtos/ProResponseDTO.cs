using Newtonsoft.Json;
using System.ComponentModel;

namespace Win115.Dtos
{
    public class ProResponseDTO<T>
    {
        [JsonProperty("state")]
        public bool State { get; set; }

        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("message")]
        public string? Message { get; set; }

        [JsonProperty("data")]
        public T? Data { get; set; }
    }

    public class ProResponseDTO
    {
        [JsonProperty("state")]
        public bool State { get; set; }

        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("message")]
        public string? Message { get; set; }
    }
}
