using Newtonsoft.Json;
using System.ComponentModel;

namespace Win115.Dtos
{
    public record OpenUfileUpdateDTO
    {
        [JsonProperty("file_name"), DefaultValue("")]
        public string? FileName { get; set; }

        [JsonProperty("star"), DefaultValue("")]
        public string? Star { get; set; }
    }
}
