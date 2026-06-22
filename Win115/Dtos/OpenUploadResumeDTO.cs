using System.Text.Json.Serialization;

namespace Win115.Dtos
{
    public class OpenUploadResumeDTO
    {
        /// <summary>
        /// 上传任务唯一ID,用于续传
        /// </summary>
        [JsonPropertyName("pick_code")]
        public string? PickCode { get; set; }

        /// <summary>
        /// 文件上传目标约定
        /// </summary>
        [JsonPropertyName("target")]
        public string? Target { get; set; }

        /// <summary>
        /// 接口版本
        /// </summary>
        [JsonPropertyName("version")]
        public string? Version { get; set; }

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

        /// <summary>
        /// OSS objectID
        /// </summary>
        [JsonPropertyName("callback")]
        public OpenUploadResumeCallbackDTO? Callback { get; set; }
    }

    public class OpenUploadResumeCallbackDTO
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
