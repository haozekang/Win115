using System.Text.Json.Serialization;

namespace Win115.Dtos
{
    public class OpenUfileDownurlDTO
    {
        [JsonPropertyName("file_name")]
        public string? FileName { get; set; }

        [JsonPropertyName("file_size")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long? FileSize { get; set; }

        [JsonPropertyName("pick_code")]
        public string? PickCode { get; set; }

        [JsonPropertyName("sha1")]
        public string? Sha1 { get; set; }

        [JsonPropertyName("url")]
        public UfileDownurlFileDataUrlDTO? Url { get; set; }
    }

    public class UfileDownurlFileDataUrlDTO
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }
}
