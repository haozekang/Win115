using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text;

namespace Win115.Dtos
{
    public class OpenRbListDTO
    {
        /// <summary>
        /// 偏移量
        /// </summary>
        [JsonProperty("offset")]
        public long? Offset { get; set; }

        /// <summary>
        /// 分页量
        /// </summary>
        [JsonProperty("limit")]
        public long? Limit { get; set; }

        /// <summary>
        /// 分页量
        /// </summary>
        [JsonProperty("count")]
        public string? Count { get; set; }

        /// <summary>
        /// 是否设置回收站密码
        /// </summary>
        [JsonProperty("rb_pass")]
        public long? RbPass { get; set; }

        /// <summary>
        /// 动态的回收站文件列表，Key = 回收站ID
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, OpenRbListDataItemDTO> Items { get; set; } = new();

        /// <summary>
        /// 处理那些不固定的数字字段
        /// </summary>
        [JsonExtensionData]
        private IDictionary<string, JToken>? ExtensionData { get; set; }

        /// <summary>
        /// 在反序列化完成后，将动态字段提取到 Items 中
        /// </summary>
        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            if (ExtensionData == null)
                return;

            foreach (var kv in ExtensionData)
            {
                // 跳过固定字段
                if (kv.Key is "offset" or "limit" or "count" or "rb_pass")
                {
                    continue;
                }

                // 只处理对象类型（即文件信息）
                if (kv.Value.Type == JTokenType.Object)
                {
                    var item = kv.Value.ToObject<OpenRbListDataItemDTO>();
                    if (item != null)
                    {
                        Items[kv.Key] = item;
                    }
                }
            }
        }
    }


    public class OpenRbListDataItemDTO
    {
        /// <summary>
        /// 文件(夹)回收站ID
        /// </summary>
        [JsonProperty("id")]
        public string? Id { get; set; }

        /// <summary>
        /// 文件(夹)名称
        /// </summary>
        [JsonProperty("file_name")]
        public string? FileName { get; set; }

        /// <summary>
        /// 类型（1：文件，2：目录
        /// </summary>
        [JsonProperty("type")]
        public string? Type { get; set; }

        /// <summary>
        /// 文件大小
        /// </summary>
        [JsonProperty("file_size")]
        public string? FileSize { get; set; }

        /// <summary>
        /// 删除日期
        /// </summary>
        [JsonProperty("dtime")]
        public string? DeleteTime { get; set; }

        /// <summary>
        /// 缩略图地址
        /// </summary>
        [JsonProperty("thumb_url")]
        public string? ThumbUrl { get; set; }

        /// <summary>
        /// 还原状态，-1 表示还原中，0 表示正常状态
        /// </summary>
        [JsonProperty("status")]
        public string? Status { get; set; }

        /// <summary>
        /// 原文件的父目录id
        /// </summary>
        [JsonProperty("cid")]
        public string? ParentId { get; set; }

        /// <summary>
        /// 原文件的父目录名称
        /// </summary>
        [JsonProperty("parent_name")]
        public string? ParentName { get; set; }

        /// <summary>
        /// 文件提取码
        /// </summary>
        [JsonProperty("pick_code")]
        public string? PickCode { get; set; }
    }
}
