using Newtonsoft.Json;
using System.ComponentModel;

namespace Win115.Dtos
{
    public class OpenUfileSearchDTO
    {
        [JsonProperty("count")]
        public long? Count { get; set; }

        [JsonProperty("limit")]
        public long? Limit { get; set; }

        [JsonProperty("offset")]
        public long? Offset { get; set; }

        [JsonProperty("data")]
        public FSearchDataDTO[]? Data { get; set; }

        [JsonProperty("state")]
        public bool State { get; set; }

        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("message")]
        public string? Message { get; set; }
    }

    public class FSearchDataDTO
    {
        /// <summary>
        /// 文件ID
        /// </summary>
        [JsonProperty("file_id")]
        public string? FileId { get; set; }

        /// <summary>
        /// 用户ID
        /// </summary>
        [JsonProperty("user_id")]
        public string? UserId { get; set; }

        /// <summary>
        /// sha1值
        /// </summary>
        [JsonProperty("sha1")]
        public string? Sha1 { get; set; }

        /// <summary>
        /// 文件名称
        /// </summary>
        [JsonProperty("file_name")]
        public string? FileName { get; set; }

        /// <summary>
        /// 文件大小
        /// </summary>
        [JsonProperty("file_size")]
        public long? FileSize { get; set; }

        /// <summary>
        /// 上传时间
        /// </summary>
        [JsonProperty("user_ptime")]
        public string? UserPtime { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        [JsonProperty("user_utime")]
        public string? UserUtime { get; set; }

        /// <summary>
        /// 文件提取码
        /// </summary>
        [JsonProperty("pick_code")]
        public string? PickCode { get; set; }

        /// <summary>
        /// 父目录ID
        /// </summary>
        [JsonProperty("parent_id")]
        public string? ParentId { get; set; }

        /// <summary>
        /// 文件的状态，aid 的别名。1 正常，7 删除(回收站)，120 彻底删除
        /// </summary>
        [JsonProperty("area_id")]
        public string? AreaId { get; set; }

        /// <summary>
        /// 文件是否隐藏。0 未隐藏，1 已隐藏
        /// </summary>
        [JsonProperty("is_private")]
        public int? IsPrivate { get; set; }

        /// <summary>
        /// 文件分类。0 文件夹，1 文件
        /// </summary>
        [JsonProperty("file_category")]
        public string? FileCategory { get; set; }

        /// <summary>
        /// 文件后缀名
        /// </summary>
        [JsonProperty("ico")]
        public string? ICO { get; set; }
    }
}
