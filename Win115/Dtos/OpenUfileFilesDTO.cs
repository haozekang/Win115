using Newtonsoft.Json;
using System.ComponentModel;

namespace Win115.Dtos
{
    public record OpenUfileFilesDTO
    {
        /// <summary>
        /// 排序
        /// </summary>
        [JsonProperty("order")]
        public string? Order { get; set; }

        [JsonProperty("fields")]
        public string? Fields { get; set; }

        [JsonProperty("stdir")]
        public long? STDir { get; set; }

        [JsonProperty("cur")]
        public long? Cur { get; set; }

        [JsonProperty("path")]
        public FPathDTO[]? Paths { get; set; }

        [JsonProperty("suffix")]
        public string? Suffix { get; set; }

        [JsonProperty("type")]
        public int? Type { get; set; }

        [JsonProperty("star")]
        public int? Star { get; set; }

        [JsonProperty("record_open_time")]
        public string? RecordOpenTime { get; set; }

        [JsonProperty("hide_data")]
        public string? HideData { get; set; }

        [JsonProperty("sys_dir")]
        public string? SysDir { get; set; }

        [JsonProperty("max_size")]
        public long? MaxSize { get; set; }

        [JsonProperty("min_size")]
        public long? MinSize { get; set; }

        [JsonProperty("is_asc")]
        public int? IsAsc { get; set; }

        [JsonProperty("cid")]
        public long? CId { get; set; }

        [JsonProperty("aid")]
        public string? AId { get; set; }

        [JsonProperty("limit")]
        public long? Limit { get; set; }

        [JsonProperty("offset")]
        public long? Offset { get; set; }

        [JsonProperty("sys_count")]
        public long? SysCount { get; set; }

        [JsonProperty("count")]
        public long? Count { get; set; }

        [JsonProperty("data")]
        public FDataDTO[]? Data { get; set; }

        [JsonProperty("state")]
        public bool State { get; set; }

        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("message")]
        public string? Message { get; set; }
    }

    public record FDataDTO
    {
        /// <summary>
        /// 文件ID
        /// </summary>
        [JsonProperty("fid")]
        public string? FId { get; set; }

        /// <summary>
        /// 文件的状态，aid 的别名。1 正常，7 删除(回收站)，120 彻底删除
        /// </summary>
        [JsonProperty("aid")]
        public string? AId { get; set; }

        /// <summary>
        /// 父目录ID
        /// </summary>
        [JsonProperty("pid")]
        public string? PId { get; set; }

        /// <summary>
        /// 文件分类。0 文件夹，1 文件
        /// </summary>
        [JsonProperty("fc")]
        public string? FC { get; set; }

        /// <summary>
        /// 文件(夹)名称
        /// </summary>
        [JsonProperty("fn")]
        public string? FN { get; set; }

        /// <summary>
        /// 文件夹封面
        /// </summary>
        [JsonProperty("fco")]
        public string? FCO { get; set; }

        /// <summary>
        /// 是否星标，1：星标
        /// </summary>
        [JsonProperty("ism")]
        public string? IsM { get; set; }

        /// <summary>
        /// 是否加密；1：加密
        /// </summary>
        [JsonProperty("isp")]
        public int? IsP { get; set; }

        /// <summary>
        /// 文件提取码
        /// </summary>
        [JsonProperty("pc")]
        public string? PC { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        [JsonProperty("upt")]
        public long? UpT { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        [JsonProperty("uet")]
        public long? UeT { get; set; }

        /// <summary>
        /// 上传时间
        /// </summary>
        [JsonProperty("uppt")]
        public long? UppT { get; set; }

        [JsonProperty("cm")]
        public long? CM { get; set; }

        /// <summary>
        /// 文件备注
        /// </summary>
        [JsonProperty("fdesc")]
        public string? FDesc { get; set; }

        /// <summary>
        /// 文件备注
        /// </summary>
        [JsonProperty("ispl")]
        public int? IsPl { get; set; }

        /// <summary>
        /// 文件标签
        /// </summary>
        [JsonProperty("fl")]
        public FLabelDTO[]? FL { get; set; }

        /// <summary>
        /// sha1值
        /// </summary>
        [JsonProperty("sha1")]
        public string? Sha1 { get; set; }

        /// <summary>
        /// 文件大小
        /// </summary>
        [JsonProperty("fs")]
        public long? FS { get; set; }

        /// <summary>
        /// 文件状态 0/2 未上传完成，1 已上传完成
        /// </summary>
        [JsonProperty("fta")]
        public string? FTA { get; set; }

        /// <summary>
        /// 文件后缀名
        /// </summary>
        [JsonProperty("ico")]
        public string? ICO { get; set; }

        /// <summary>
        /// 音频长度
        /// </summary>
        [JsonProperty("fatr")]
        public string? FATR { get; set; }

        /// <summary>
        /// 是否为视频
        /// </summary>
        [JsonProperty("isv")]
        public int? IsV { get; set; }

        /// <summary>
        /// 视频清晰度；1:标清 2:高清 3:超清 4:1080P 5:4k;100:原画
        /// </summary>
        [JsonProperty("def")]
        public int? Def { get; set; }

        /// <summary>
        /// 视频清晰度；1:标清 2:高清 3:超清 4:1080P 5:4k;100:原画
        /// </summary>
        [JsonProperty("def2")]
        public int? Def2 { get; set; }

        /// <summary>
        /// 音视频时长
        /// </summary>
        [JsonProperty("play_long")]
        public long? PlayLong { get; set; }

        [JsonProperty("v_img")]
        public string? VImg { get; set; }

        /// <summary>
        /// 图片缩略图
        /// </summary>
        [JsonProperty("thumb")]
        public string? Thumb { get; set; }

        /// <summary>
        /// 原图地址
        /// </summary>
        [JsonProperty("uo")]
        public string? UO { get; set; }
    }

    public record FLabelDTO
    {
        /// <summary>
        /// 文件标签id
        /// </summary>
        [JsonProperty("id")]
        public string? Id { get; set; }

        /// <summary>
        /// 文件标签名称
        /// </summary>
        [JsonProperty("name")]
        public string? Name { get; set; }

        /// <summary>
        /// 文件标签排序
        /// </summary>
        [JsonProperty("sort")]
        public string? Sort { get; set; }

        /// <summary>
        /// 文件标签颜色
        /// </summary>
        [JsonProperty("color")]
        public string? Color { get; set; }

        /// <summary>
        /// 文件标签类型；0：最近使用；1：非最近使用；2：为默认标签
        /// </summary>
        [JsonProperty("is_default")]
        public int? IsDefault { get; set; }

        /// <summary>
        /// 文件标签更新时间
        /// </summary>
        [JsonProperty("update_time")]
        public long? UpdateTime { get; set; }

        /// <summary>
        /// 文件标签创建时间
        /// </summary>
        [JsonProperty("create_time")]
        public long? CreateTime { get; set; }
    }

    public record FPathDTO
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("aid")]
        public string? AId { get; set; }

        [JsonProperty("cid")]
        public string? CId { get; set; }

        [JsonProperty("pid")]
        public string? PId { get; set; }

        [JsonProperty("isp")]
        public string? IsP { get; set; }

        [JsonProperty("p_cid")]
        public string? PCId { get; set; }

        [JsonProperty("fv")]
        public string? FV { get; set; }
    }
}
