using System.Text.Json.Serialization;

namespace Win115.Dtos
{
    public class OpenUfileUpdateDTO
    {
        [JsonPropertyName("file_name")]
        public string? FileName { get; set; }

        [JsonPropertyName("star")]
        public string? Star { get; set; }
    }
}
