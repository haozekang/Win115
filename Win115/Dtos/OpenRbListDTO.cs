using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Win115.Dtos
{
    [JsonConverter(typeof(OpenRbListDTOConverter))]
    public class OpenRbListDTO
    {
        /// <summary>
        /// 偏移量
        /// </summary>
        [JsonPropertyName("offset")]
        public long? Offset { get; set; }

        /// <summary>
        /// 分页量
        /// </summary>
        [JsonPropertyName("limit")]
        public long? Limit { get; set; }

        /// <summary>
        /// 分页量
        /// </summary>
        [JsonPropertyName("count")]
        public string? Count { get; set; }

        /// <summary>
        /// 是否设置回收站密码
        /// </summary>
        [JsonPropertyName("rb_pass")]
        public long? RbPass { get; set; }

        /// <summary>
        /// 动态的回收站文件列表，Key = 回收站ID
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, OpenRbListDataItemDTO> Items { get; set; } = new();
    }


    public class OpenRbListDataItemDTO
    {
        /// <summary>
        /// 文件(夹)回收站ID
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// 文件(夹)名称
        /// </summary>
        [JsonPropertyName("file_name")]
        public string? FileName { get; set; }

        /// <summary>
        /// 类型（1：文件，2：目录
        /// </summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>
        /// 文件大小
        /// </summary>
        [JsonPropertyName("file_size")]
        public string? FileSize { get; set; }

        /// <summary>
        /// 删除日期
        /// </summary>
        [JsonPropertyName("dtime")]
        public string? DeleteTime { get; set; }

        /// <summary>
        /// 缩略图地址
        /// </summary>
        [JsonPropertyName("thumb_url")]
        public string? ThumbUrl { get; set; }

        /// <summary>
        /// 还原状态，-1 表示还原中，0 表示正常状态
        /// </summary>
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        /// <summary>
        /// 原文件的父目录id
        /// </summary>
        [JsonPropertyName("cid")]
        public string? ParentId { get; set; }

        /// <summary>
        /// 原文件的父目录名称
        /// </summary>
        [JsonPropertyName("parent_name")]
        public string? ParentName { get; set; }

        /// <summary>
        /// 文件提取码
        /// </summary>
        [JsonPropertyName("pick_code")]
        public string? PickCode { get; set; }
    }
}
