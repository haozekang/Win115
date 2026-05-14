using Newtonsoft.Json;
using System.ComponentModel;

namespace Win115.Dtos
{
    public record GetQeCodeStatusDTO
    {
        [JsonProperty("msg"), DefaultValue("")]
        public string? Message { get; set; }

        [JsonProperty("status"), DefaultValue(0)]
        public int Status { get; set; }

        [JsonProperty("version"), DefaultValue("")]
        public string? Version { get; set; }
    }
}
