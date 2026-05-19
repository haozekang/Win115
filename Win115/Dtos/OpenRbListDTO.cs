using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text;

namespace Win115.Dtos
{
    public record OpenRbListDTO
    {
        /// <summary>
        /// 偏移量
        /// </summary>
        [JsonProperty("offset"), DefaultValue(0)]
        public long? Offset { get; set; }

        /// <summary>
        /// 分页量
        /// </summary>
        [JsonProperty("limit"), DefaultValue(0)]
        public long? Limit { get; set; }

        /// <summary>
        /// 分页量
        /// </summary>
        [JsonProperty("count"), DefaultValue("")]
        public string? Count { get; set; }

        /// <summary>
        /// 是否设置回收站密码
        /// </summary>
        [JsonProperty("rb_pass"), DefaultValue(0)]
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


    public record OpenRbListDataItemDTO
    {
        /// <summary>
        /// 文件(夹)回收站ID
        /// </summary>
        [JsonProperty("id"), DefaultValue("")]
        public string? Id { get; set; }

        /// <summary>
        /// 文件(夹)名称
        /// </summary>
        [JsonProperty("file_name"), DefaultValue("")]
        public string? FileName { get; set; }

        /// <summary>
        /// 类型（1：文件，2：目录
        /// </summary>
        [JsonProperty("type"), DefaultValue("")]
        public string? Type { get; set; }

        /// <summary>
        /// 文件大小
        /// </summary>
        [JsonProperty("file_size"), DefaultValue("")]
        public string? FileSize { get; set; }

        /// <summary>
        /// 删除日期
        /// </summary>
        [JsonProperty("dtime"), DefaultValue("")]
        public string? DeleteTime { get; set; }

        /// <summary>
        /// 缩略图地址
        /// </summary>
        [JsonProperty("thumb_url"), DefaultValue("")]
        public string? ThumbUrl { get; set; }

        /// <summary>
        /// 还原状态，-1 表示还原中，0 表示正常状态
        /// </summary>
        [JsonProperty("status"), DefaultValue("")]
        public string? Status { get; set; }

        /// <summary>
        /// 原文件的父目录id
        /// </summary>
        [JsonProperty("cid"), DefaultValue("")]
        public string? ParentId { get; set; }

        /// <summary>
        /// 原文件的父目录名称
        /// </summary>
        [JsonProperty("parent_name"), DefaultValue("")]
        public string? ParentName { get; set; }

        /// <summary>
        /// 文件提取码
        /// </summary>
        [JsonProperty("pick_code"), DefaultValue("")]
        public string? PickCode { get; set; }
    }
}
