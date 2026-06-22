using System.Text.Json.Serialization;

namespace Win115.Dtos
{
    public class OpenUploadInitNoCallbackDTO
    {
        /// <summary>
        /// 上传任务唯一ID,用于续传
        /// </summary>
        [JsonPropertyName("pick_code")]
        public string? PickCode { get; set; }

        /// <summary>
        /// 上传状态；1：非秒传；2：秒传
        /// </summary>
        [JsonPropertyName("status")]
        public int? Status { get; set; }

        /// <summary>
        /// 上传状态；1：非秒传；2：秒传
        /// </summary>
        [JsonPropertyName("code")]
        public int? Code { get; set; }

        /// <summary>
        /// 本次计算的sha1标识（二次认证）
        /// </summary>
        [JsonPropertyName("sign_key")]
        public string? SignKey { get; set; }

        /// <summary>
        /// 本次计算本地文件sha1区间范围（二次认证）
        /// </summary>
        [JsonPropertyName("sign_check")]
        public string? SignCheck { get; set; }

        /// <summary>
        /// 秒传成功返回的新增文件ID
        /// </summary>
        [JsonPropertyName("file_id")]
        public string? FileId { get; set; }
    }

    public class OpenUploadInitDTO
    {
        /// <summary>
        /// 上传任务唯一ID,用于续传
        /// </summary>
        [JsonPropertyName("pick_code")]
        public string? PickCode { get; set; }

        /// <summary>
        /// 上传状态；1：非秒传；2：秒传
        /// </summary>
        [JsonPropertyName("status")]
        public int? Status { get; set; }

        /// <summary>
        /// 上传状态；1：非秒传；2：秒传
        /// </summary>
        [JsonPropertyName("code")]
        public int? Code { get; set; }

        /// <summary>
        /// 本次计算的sha1标识（二次认证）
        /// </summary>
        [JsonPropertyName("sign_key")]
        public string? SignKey { get; set; }

        /// <summary>
        /// 本次计算本地文件sha1区间范围（二次认证）
        /// </summary>
        [JsonPropertyName("sign_check")]
        public string? SignCheck { get; set; }

        /// <summary>
        /// 秒传成功返回的新增文件ID
        /// </summary>
        [JsonPropertyName("file_id")]
        public string? FileId { get; set; }

        /// <summary>
        /// 文件上传目标约定
        /// </summary>
        [JsonPropertyName("target")]
        public string? Target { get; set; }

        /// <summary>
        /// 上传的bucket
        /// </summary>
        [JsonPropertyName("bucket")]
        public string? Bucket { get; set; }

        /// <summary>
        /// OSS objectID
        /// </summary>
        [JsonPropertyName("object")]
        public string? Object { get; set; }

        [JsonPropertyName("callback")]
        public OpenUploadInitCallbackDTO? Callback { get; set; }
    }

    public class OpenUploadInitCallbackDTO
    {
        /// <summary>
        /// 上传完回调信息
        /// </summary>
        [JsonPropertyName("callback")]
        public string? Callback { get; set; }

        /// <summary>
        /// 上传完回调参数
        /// </summary>
        [JsonPropertyName("callback_var")]
        public string? CallbackVar { get; set; }
    }
}
