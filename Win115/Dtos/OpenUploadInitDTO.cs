using Newtonsoft.Json;
using System.Collections.Generic;

namespace Win115.Dtos
{
    public record OpenUploadInitNoCallbackDTO
    {
        /// <summary>
        /// 上传任务唯一ID,用于续传
        /// </summary>
        [JsonProperty("pick_code")]
        public string? PickCode { get; set; }

        /// <summary>
        /// 上传状态；1：非秒传；2：秒传
        /// </summary>
        [JsonProperty("status")]
        public int? Status { get; set; }

        /// <summary>
        /// 上传状态；1：非秒传；2：秒传
        /// </summary>
        [JsonProperty("code")]
        public int? Code { get; set; }

        /// <summary>
        /// 本次计算的sha1标识（二次认证）
        /// </summary>
        [JsonProperty("sign_key")]
        public string? SignKey { get; set; }

        /// <summary>
        /// 本次计算本地文件sha1区间范围（二次认证）
        /// </summary>
        [JsonProperty("sign_check")]
        public string? SignCheck { get; set; }

        /// <summary>
        /// 秒传成功返回的新增文件ID
        /// </summary>
        [JsonProperty("file_id")]
        public string? FileId { get; set; }
    }

    public record OpenUploadInitDTO
    {
        /// <summary>
        /// 上传任务唯一ID,用于续传
        /// </summary>
        [JsonProperty("pick_code")]
        public string? PickCode { get; set; }

        /// <summary>
        /// 上传状态；1：非秒传；2：秒传
        /// </summary>
        [JsonProperty("status")]
        public int? Status { get; set; }

        /// <summary>
        /// 上传状态；1：非秒传；2：秒传
        /// </summary>
        [JsonProperty("code")]
        public int? Code { get; set; }

        /// <summary>
        /// 本次计算的sha1标识（二次认证）
        /// </summary>
        [JsonProperty("sign_key")]
        public string? SignKey { get; set; }

        /// <summary>
        /// 本次计算本地文件sha1区间范围（二次认证）
        /// </summary>
        [JsonProperty("sign_check")]
        public string? SignCheck { get; set; }

        /// <summary>
        /// 秒传成功返回的新增文件ID
        /// </summary>
        [JsonProperty("file_id")]
        public string? FileId { get; set; }

        /// <summary>
        /// 文件上传目标约定
        /// </summary>
        [JsonProperty("target")]
        public string? Target { get; set; }

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

        [JsonProperty("callback")]
        public OpenUploadInitCallbackDTO? Callback { get; set; }
    }

    public record OpenUploadInitCallbackDTO
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
