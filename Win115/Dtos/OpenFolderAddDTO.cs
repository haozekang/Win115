using System.Text.Json.Serialization;

namespace Win115.Dtos
{
    public class OpenFolderAddDTO
    {
        /// <summary>
        /// 新建的文件夹名称
        /// </summary>
        [JsonPropertyName("file_name")]
        public string? FileName { get; set; }

        /// <summary>
        /// 新建的文件夹ID
        /// </summary>
        [JsonPropertyName("file_id")]
        public string? FileId { get; set; }
    }
}
