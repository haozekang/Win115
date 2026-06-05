using Newtonsoft.Json;
using System.ComponentModel;

namespace Win115.Dtos
{
    public record OpenOfflineGetTaskListDTO
    {
        /// <summary>
        /// 当前第几页
        /// </summary>
        [JsonProperty("page")]
        public long? Page { get; set; }

        /// <summary>
        /// 总页数
        /// </summary>
        [JsonProperty("page_count")]
        public long? PageCount { get; set; }

        /// <summary>
        /// 总数量
        /// </summary>
        [JsonProperty("count")]
        public long? Count { get; set; }

        /// <summary>
        /// 云下载任务列表
        /// </summary>
        [JsonProperty("tasks")]
        public OpenOfflineTaskItemDTO[]? Tasks { get; set; }
    }


    public record OpenOfflineTaskItemDTO
    {
        /// <summary>
        /// 任务sha1
        /// </summary>
        [JsonProperty("info_hash")]
        public string? InfoHash { get; set; }

        /// <summary>
        /// 任务添加时间戳
        /// </summary>
        [JsonProperty("add_time")]
        public long? AddTime { get; set; }

        /// <summary>
        /// 任务总大小（字节）
        /// </summary>
        [JsonProperty("percentDone")]
        public long? PercentDone { get; set; }

        /// <summary>
        /// 任务下载进度
        /// </summary>
        [JsonProperty("size")]
        public long? Size { get; set; }

        /// <summary>
        /// 任务名
        /// </summary>
        [JsonProperty("name")]
        public string? Name { get; set; }

        /// <summary>
        /// 任务最后更新时间戳
        /// </summary>
        [JsonProperty("last_update")]
        public long? LastUpdate { get; set; }

        /// <summary>
        /// 任务源文件（夹）对应文件（夹）id
        /// </summary>
        [JsonProperty("file_id")]
        public string? FileId { get; set; }

        /// <summary>
        /// 任务源文件（夹）对应文件（夹）id
        /// </summary>
        [JsonProperty("delete_file_id")]
        public string? DeleteFileId { get; set; }

        /// <summary>
        /// 任务状态：-1下载失败；0分配中；1下载中；2下载成功
        /// </summary>
        [JsonProperty("status")]
        public int? Status { get; set; }

        /// <summary>
        /// 链接任务url
        /// </summary>
        [JsonProperty("url")]
        public string? Url { get; set; }

        /// <summary>
        /// 任务源文件所在父文件夹id
        /// </summary>
        [JsonProperty("wp_path_id")]
        public string? WpPathId { get; set; }

        /// <summary>
        /// 视频清晰度；1:标清 2:高清 3:超清 4:1080P 5:4k;100:原画
        /// </summary>
        [JsonProperty("def2")]
        public int? Def2 { get; set; }

        /// <summary>
        /// 视频时长
        /// </summary>
        [JsonProperty("play_long")]
        public long? PlayLong { get; set; }

        /// <summary>
        /// 是否可申诉
        /// </summary>
        [JsonProperty("can_appeal")]
        public int? CanAppeal { get; set; }
    }
}
