using System.Text.Json.Serialization;

namespace Win115.Dtos
{
    public class OpenFolderGetInfoDTO
    {
        /// <summary>
        /// 包含文件总数量
        /// </summary>
        [JsonPropertyName("count")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long? Count { get; set; }

        /// <summary>
        /// 文件(夹)总大小
        /// </summary>
        [JsonPropertyName("size")]
        public string? Size { get; set; }

        /// <summary>
        /// 文件(夹)总大小(字节单位)
        /// </summary>
        [JsonPropertyName("size_byte")]
        public long? SizeByte { get; set; }

        /// <summary>
        /// 包含文件夹总数量
        /// </summary>
        [JsonPropertyName("folder_count")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long? FolderCount { get; set; }

        /// <summary>
        /// 视频时长；-1：正在统计，其他数值为视频时长的数值(单位秒)
        /// </summary>
        [JsonPropertyName("play_long")]
        public long? PlayLong { get; set; }

        /// <summary>
        /// 是否开启展示视频时长
        /// </summary>
        [JsonPropertyName("show_play_long")]
        public long? ShowPlayLong { get; set; }

        /// <summary>
        /// 上传时间
        /// </summary>
        [JsonPropertyName("ptime")]
        public string? Ptime { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        [JsonPropertyName("utime")]
        public string? Utime { get; set; }

        /// <summary>
        /// 文件名
        /// </summary>
        [JsonPropertyName("file_name")]
        public string? FileName { get; set; }

        /// <summary>
        /// 文件提取码
        /// </summary>
        [JsonPropertyName("pick_code")]
        public string? PickCode { get; set; }

        /// <summary>
        /// SHA1值
        /// </summary>
        [JsonPropertyName("sha1")]
        public string? Sha1 { get; set; }

        /// <summary>
        /// 文件(夹)ID
        /// </summary>
        [JsonPropertyName("file_id")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long? FileId { get; set; }

        /// <summary>
        /// 是否星标
        /// </summary>
        [JsonPropertyName("is_mark")]
        public string? IsMark { get; set; }

        /// <summary>
        /// 文件(夹)最近打开时间
        /// </summary>
        [JsonPropertyName("open_time")]
        public long? OpenTime { get; set; }

        /// <summary>
        /// 文件属性；1：文件；0：文件夹
        /// </summary>
        [JsonPropertyName("file_category")]
        public string? FileCategory { get; set; }

        /// <summary>
        /// 文件(夹)所在的路径
        /// </summary>
        [JsonPropertyName("paths")]
        public OpenFolderGetInfoPathDTO[]? Paths { get; set; }
    }

    /// <summary>
    /// 路径信息子对象
    /// </summary>
    public class OpenFolderGetInfoPathDTO
    {
        /// <summary>
        /// 父目录ID
        /// </summary>
        [JsonPropertyName("file_id")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long? FileId { get; set; }

        /// <summary>
        /// 父目录名称
        /// </summary>
        [JsonPropertyName("file_name")]
        public string? FileName { get; set; }
    }
}
