using Newtonsoft.Json;
using System.ComponentModel;

namespace Win115.Dtos
{
    public record OpenOfflineGetTaskListDTO
    {
        /// <summary>
        /// 当前第几页
        /// </summary>
        [JsonProperty("page"), DefaultValue(0)]
        public long? Page { get; set; }

        /// <summary>
        /// 总页数
        /// </summary>
        [JsonProperty("page_count"), DefaultValue(0)]
        public long? PageCount { get; set; }

        /// <summary>
        /// 总数量
        /// </summary>
        [JsonProperty("count"), DefaultValue(0)]
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
        [JsonProperty("info_hash"), DefaultValue("")]
        public string? InfoHash { get; set; }

        /// <summary>
        /// 任务添加时间戳
        /// </summary>
        [JsonProperty("add_time"), DefaultValue(0)]
        public long? AddTime { get; set; }

        /// <summary>
        /// 任务总大小（字节）
        /// </summary>
        [JsonProperty("percentDone"), DefaultValue(0)]
        public long? PercentDone { get; set; }

        /// <summary>
        /// 任务下载进度
        /// </summary>
        [JsonProperty("size"), DefaultValue(0)]
        public long? Size { get; set; }

        /// <summary>
        /// 任务名
        /// </summary>
        [JsonProperty("name"), DefaultValue("")]
        public string? Name { get; set; }

        /// <summary>
        /// 任务最后更新时间戳
        /// </summary>
        [JsonProperty("last_update"), DefaultValue(0)]
        public long? LastUpdate { get; set; }

        /// <summary>
        /// 任务源文件（夹）对应文件（夹）id
        /// </summary>
        [JsonProperty("file_id"), DefaultValue("")]
        public string? FileId { get; set; }

        /// <summary>
        /// 任务源文件（夹）对应文件（夹）id
        /// </summary>
        [JsonProperty("delete_file_id"), DefaultValue("")]
        public string? DeleteFileId { get; set; }

        /// <summary>
        /// 任务状态：-1下载失败；0分配中；1下载中；2下载成功
        /// </summary>
        [JsonProperty("status"), DefaultValue(0)]
        public int? Status { get; set; }

        /// <summary>
        /// 链接任务url
        /// </summary>
        [JsonProperty("url"), DefaultValue("")]
        public string? Url { get; set; }

        /// <summary>
        /// 任务源文件所在父文件夹id
        /// </summary>
        [JsonProperty("wp_path_id"), DefaultValue("")]
        public string? WpPathId { get; set; }

        /// <summary>
        /// 视频清晰度；1:标清 2:高清 3:超清 4:1080P 5:4k;100:原画
        /// </summary>
        [JsonProperty("def2"), DefaultValue(0)]
        public int? Def2 { get; set; }

        /// <summary>
        /// 视频时长
        /// </summary>
        [JsonProperty("play_long"), DefaultValue(0)]
        public long? PlayLong { get; set; }

        /// <summary>
        /// 是否可申诉
        /// </summary>
        [JsonProperty("can_appeal"), DefaultValue(0)]
        public int? CanAppeal { get; set; }
    }
}
