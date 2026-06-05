using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Win115.Dtos
{
    public record OpenFolderAddDTO
    {
        /// <summary>
        /// 新建的文件夹名称
        /// </summary>
        [JsonProperty("file_name")]
        public string? FileName { get; set; }

        /// <summary>
        /// 新建的文件夹ID
        /// </summary>
        [JsonProperty("file_id")]
        public string? FileId { get; set; }
    }
}
