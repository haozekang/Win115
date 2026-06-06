using Newtonsoft.Json;
using System.ComponentModel;

namespace Win115.Dtos
{
    public class GetQeCodeStatusDTO
    {
        [JsonProperty("msg")]
        public string? Message { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("version")]
        public string? Version { get; set; }
    }
}
