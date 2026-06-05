using Newtonsoft.Json;
using System.ComponentModel;

namespace Win115.Dtos
{
    public record OpenUfileUpdateDTO
    {
        [JsonProperty("file_name")]
        public string? FileName { get; set; }

        [JsonProperty("star")]
        public string? Star { get; set; }
    }
}
