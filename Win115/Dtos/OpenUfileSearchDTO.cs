using Newtonsoft.Json;
using System.ComponentModel;

namespace Win115.Dtos
{
    public record OpenUfileSearchDTO
    {
        [JsonProperty("count"), DefaultValue(0)]
        public long? Count { get; set; }

        [JsonProperty("limit"), DefaultValue(0)]
        public long? Limit { get; set; }

        [JsonProperty("offset"), DefaultValue(0)]
        public long? Offset { get; set; }

        [JsonProperty("data"), DefaultValue(null)]
        public FSearchDataDTO[]? Data { get; set; }

        [JsonProperty("state"), DefaultValue(false)]
        public bool State { get; set; }

        [JsonProperty("code"), DefaultValue(0)]
        public int Code { get; set; }

        [JsonProperty("message"), DefaultValue("")]
        public string? Message { get; set; }
    }

    public record FSearchDataDTO
    {
        /// <summary>
        /// 文件ID
        /// </summary>
        [JsonProperty("file_id"), DefaultValue("")]
        public string? FileId { get; set; }

        /// <summary>
        /// 用户ID
        /// </summary>
        [JsonProperty("user_id"), DefaultValue("")]
        public string? UserId { get; set; }

        /// <summary>
        /// sha1值
        /// </summary>
        [JsonProperty("sha1"), DefaultValue("")]
        public string? Sha1 { get; set; }

        /// <summary>
        /// 文件名称
        /// </summary>
        [JsonProperty("file_name"), DefaultValue("")]
        public string? FileName { get; set; }

        /// <summary>
        /// 文件大小
        /// </summary>
        [JsonProperty("file_size"), DefaultValue(0)]
        public long? FileSize { get; set; }

        /// <summary>
        /// 上传时间
        /// </summary>
        [JsonProperty("user_ptime"), DefaultValue("")]
        public string? UserPtime { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        [JsonProperty("user_utime"), DefaultValue("")]
        public string? UserUtime { get; set; }

        /// <summary>
        /// 文件提取码
        /// </summary>
        [JsonProperty("pick_code"), DefaultValue("")]
        public string? PickCode { get; set; }

        /// <summary>
        /// 父目录ID
        /// </summary>
        [JsonProperty("parent_id"), DefaultValue("")]
        public string? ParentId { get; set; }

        /// <summary>
        /// 文件的状态，aid 的别名。1 正常，7 删除(回收站)，120 彻底删除
        /// </summary>
        [JsonProperty("area_id"), DefaultValue("")]
        public string? AreaId { get; set; }

        /// <summary>
        /// 文件是否隐藏。0 未隐藏，1 已隐藏
        /// </summary>
        [JsonProperty("is_private"), DefaultValue(0)]
        public int? IsPrivate { get; set; }

        /// <summary>
        /// 文件分类。0 文件夹，1 文件
        /// </summary>
        [JsonProperty("file_category"), DefaultValue("")]
        public string? FileCategory { get; set; }

        /// <summary>
        /// 文件后缀名
        /// </summary>
        [JsonProperty("ico"), DefaultValue("")]
        public string? ICO { get; set; }
    }
}
