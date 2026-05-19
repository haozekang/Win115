using Newtonsoft.Json;
using System.Collections.Generic;

namespace Win115.Dtos
{
    public record OpenUploadResumeDTO
    {
        /// <summary>
        /// 上传任务唯一ID,用于续传
        /// </summary>
        [JsonProperty("pick_code")]
        public string? PickCode { get; set; }

        /// <summary>
        /// 文件上传目标约定
        /// </summary>
        [JsonProperty("target")]
        public string? Target { get; set; }

        /// <summary>
        /// 接口版本
        /// </summary>
        [JsonProperty("version")]
        public string? Version { get; set; }

        /// <summary>
        /// 上传的bucket
        /// </summary>
        [JsonProperty("bucket")]
        public string? Bucket { get; set; }

        /// <summary>
        /// OSS objectID
        /// </summary>
        [JsonProperty("object")]
        public string? Object { get; set; }

        /// <summary>
        /// OSS objectID
        /// </summary>
        [JsonProperty("callback")]
        public OpenUploadResumeCallbackDTO? Callback { get; set; }
    }

    public record OpenUploadResumeCallbackDTO
    {
        /// <summary>
        /// 上传完回调信息
        /// </summary>
        [JsonProperty("callback")]
        public string? Callback { get; set; }

        /// <summary>
        /// 上传完回调参数
        /// </summary>
        [JsonProperty("callback_var")]
        public string? CallbackVar { get; set; }
    }
}
