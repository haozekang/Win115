using Newtonsoft.Json;
using System.ComponentModel;

namespace Win115.Dtos
{
    public record OpenFolderGetInfoDTO
    {
        /// <summary>
        /// 包含文件总数量
        /// </summary>
        [JsonProperty("count"), DefaultValue("")]
        public string? Count { get; set; }

        /// <summary>
        /// 文件(夹)总大小
        /// </summary>
        [JsonProperty("size"), DefaultValue("")]
        public string? Size { get; set; }

        /// <summary>
        /// 文件(夹)总大小(字节单位)
        /// </summary>
        [JsonProperty("size_byte"), DefaultValue(0)]
        public long? SizeByte { get; set; }

        /// <summary>
        /// 包含文件夹总数量
        /// </summary>
        [JsonProperty("folder_count"), DefaultValue("")]
        public string? FolderCount { get; set; }

        /// <summary>
        /// 视频时长；-1：正在统计，其他数值为视频时长的数值(单位秒)
        /// </summary>
        [JsonProperty("play_long"), DefaultValue(-1)]
        public long? PlayLong { get; set; }

        /// <summary>
        /// 是否开启展示视频时长
        /// </summary>
        [JsonProperty("show_play_long"), DefaultValue(0)]
        public long? ShowPlayLong { get; set; }

        /// <summary>
        /// 上传时间
        /// </summary>
        [JsonProperty("ptime"), DefaultValue("")]
        public string? Ptime { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        [JsonProperty("utime"), DefaultValue("")]
        public string? Utime { get; set; }

        /// <summary>
        /// 文件名
        /// </summary>
        [JsonProperty("file_name"), DefaultValue("")]
        public string? FileName { get; set; }

        /// <summary>
        /// 文件提取码
        /// </summary>
        [JsonProperty("pick_code"), DefaultValue("")]
        public string? PickCode { get; set; }

        /// <summary>
        /// SHA1值
        /// </summary>
        [JsonProperty("sha1"), DefaultValue("")]
        public string? Sha1 { get; set; }

        /// <summary>
        /// 文件(夹)ID
        /// </summary>
        [JsonProperty("file_id"), DefaultValue("")]
        public string? FileId { get; set; }

        /// <summary>
        /// 是否星标
        /// </summary>
        [JsonProperty("is_mark"), DefaultValue("")]
        public string? IsMark { get; set; }

        /// <summary>
        /// 文件(夹)最近打开时间
        /// </summary>
        [JsonProperty("open_time"), DefaultValue(0)]
        public long? OpenTime { get; set; }

        /// <summary>
        /// 文件属性；1：文件；0：文件夹
        /// </summary>
        [JsonProperty("file_category"), DefaultValue("")]
        public string? FileCategory { get; set; }

        /// <summary>
        /// 文件(夹)所在的路径
        /// </summary>
        [JsonProperty("paths"), DefaultValue(null)]
        public OpenFolderGetInfoPathDTO[]? Paths { get; set; }
    }

    /// <summary>
    /// 路径信息子对象
    /// </summary>
    public record OpenFolderGetInfoPathDTO
    {
        /// <summary>
        /// 父目录ID
        /// </summary>
        [JsonProperty("file_id"), DefaultValue(0)]
        public long? FileId { get; set; }

        /// <summary>
        /// 父目录名称
        /// </summary>
        [JsonProperty("file_name"), DefaultValue("")]
        public string? FileName { get; set; }
    }
}
