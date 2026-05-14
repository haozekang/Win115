using Newtonsoft.Json;
using System.Collections.Generic;

namespace Win115.Dtos
{
    public record OpenUfileDownurlDTO
    {
        [JsonProperty("file_name")]
        public string? FileName { get; set; }

        [JsonProperty("file_size")]
        public string? FileSize { get; set; }

        [JsonProperty("pick_code")]
        public string? PickCode { get; set; }

        [JsonProperty("sha1")]
        public string? Sha1 { get; set; }

        [JsonProperty("url")]
        public UfileDownurlFileDataUrlDTO? Url { get; set; }
    }

    public record UfileDownurlFileDataUrlDTO
    {
        [JsonProperty("url")]
        public string? Url { get; set; }
    }
}
