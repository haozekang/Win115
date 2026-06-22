using System.Text.Json.Serialization;

namespace Win115.Dtos
{
    public class OpenUfileSearchDTO
    {
        [JsonPropertyName("count")]
        public long? Count { get; set; }

        [JsonPropertyName("limit")]
        public long? Limit { get; set; }

        [JsonPropertyName("offset")]
        public long? Offset { get; set; }

        [JsonPropertyName("data")]
        public FSearchDataDTO[]? Data { get; set; }

        [JsonPropertyName("state")]
        public bool State { get; set; }

        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }

    public class FSearchDataDTO
    {
        /// <summary>
        /// 文件ID
        /// </summary>
        [JsonPropertyName("file_id")]
        public string? FileId { get; set; }

        /// <summary>
        /// 用户ID
        /// </summary>
        [JsonPropertyName("user_id")]
        public string? UserId { get; set; }

        /// <summary>
        /// sha1值
        /// </summary>
        [JsonPropertyName("sha1")]
        public string? Sha1 { get; set; }

        /// <summary>
        /// 文件名称
        /// </summary>
        [JsonPropertyName("file_name")]
        public string? FileName { get; set; }

        /// <summary>
        /// 文件大小
        /// </summary>
        [JsonPropertyName("file_size")]
        public long? FileSize { get; set; }

        /// <summary>
        /// 上传时间
        /// </summary>
        [JsonPropertyName("user_ptime")]
        public string? UserPtime { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        [JsonPropertyName("user_utime")]
        public string? UserUtime { get; set; }

        /// <summary>
        /// 文件提取码
        /// </summary>
        [JsonPropertyName("pick_code")]
        public string? PickCode { get; set; }

        /// <summary>
        /// 父目录ID
        /// </summary>
        [JsonPropertyName("parent_id")]
        public string? ParentId { get; set; }

        /// <summary>
        /// 文件的状态，aid 的别名。1 正常，7 删除(回收站)，120 彻底删除
        /// </summary>
        [JsonPropertyName("area_id")]
        public string? AreaId { get; set; }

        /// <summary>
        /// 文件是否隐藏。0 未隐藏，1 已隐藏
        /// </summary>
        [JsonPropertyName("is_private")]
        public int? IsPrivate { get; set; }

        /// <summary>
        /// 文件分类。0 文件夹，1 文件
        /// </summary>
        [JsonPropertyName("file_category")]
        public string? FileCategory { get; set; }

        /// <summary>
        /// 文件后缀名
        /// </summary>
        [JsonPropertyName("ico")]
        public string? ICO { get; set; }
    }
}
