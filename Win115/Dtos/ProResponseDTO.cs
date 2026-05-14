using Newtonsoft.Json;
using System.ComponentModel;

namespace Win115.Dtos
{
    public record ProResponseDTO<T>
    {
        [JsonProperty("state"), DefaultValue(false)]
        public bool State { get; set; }

        [JsonProperty("code"), DefaultValue(0)]
        public int Code { get; set; }

        [JsonProperty("message"), DefaultValue("")]
        public string? Message { get; set; }

        [JsonProperty("data"), DefaultValue(null)]
        public T? Data { get; set; }

    }
}
