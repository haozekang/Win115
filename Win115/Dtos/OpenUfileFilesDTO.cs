using System.Text.Json.Serialization;

namespace Win115.Dtos
{
    public class OpenUfileFilesDTO
    {
        /// <summary>
        /// 排序
        /// </summary>
        [JsonPropertyName("order")]
        public string? Order { get; set; }

        [JsonPropertyName("fields")]
        public string? Fields { get; set; }

        [JsonPropertyName("stdir")]
        public long? STDir { get; set; }

        [JsonPropertyName("cur")]
        public long? Cur { get; set; }

        [JsonPropertyName("path")]
        public FPathDTO[]? Paths { get; set; }

        [JsonPropertyName("suffix")]
        public string? Suffix { get; set; }

        [JsonPropertyName("type")]
        public int? Type { get; set; }

        [JsonPropertyName("star")]
        public int? Star { get; set; }

        [JsonPropertyName("record_open_time")]
        public string? RecordOpenTime { get; set; }

        [JsonPropertyName("hide_data")]
        public string? HideData { get; set; }

        [JsonPropertyName("sys_dir")]
        public string? SysDir { get; set; }

        [JsonPropertyName("max_size")]
        public long? MaxSize { get; set; }

        [JsonPropertyName("min_size")]
        public long? MinSize { get; set; }

        [JsonPropertyName("is_asc")]
        public int? IsAsc { get; set; }

        [JsonPropertyName("cid")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long? CId { get; set; }

        [JsonPropertyName("aid")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long? AId { get; set; }

        [JsonPropertyName("limit")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long? Limit { get; set; }

        [JsonPropertyName("offset")]
        public long? Offset { get; set; }

        [JsonPropertyName("sys_count")]
        public long? SysCount { get; set; }

        [JsonPropertyName("count")]
        public long? Count { get; set; }

        [JsonPropertyName("data")]
        public FDataDTO[]? Data { get; set; }

        [JsonPropertyName("state")]
        public bool State { get; set; }

        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }

    public class FDataDTO
    {
        /// <summary>
        /// 文件ID
        /// </summary>
        [JsonPropertyName("fid")]
        public string? FId { get; set; }

        /// <summary>
        /// 文件的状态，aid 的别名。1 正常，7 删除(回收站)，120 彻底删除
        /// </summary>
        [JsonPropertyName("aid")]
        public string? AId { get; set; }

        /// <summary>
        /// 父目录ID
        /// </summary>
        [JsonPropertyName("pid")]
        public string? PId { get; set; }

        /// <summary>
        /// 文件分类。0 文件夹，1 文件
        /// </summary>
        [JsonPropertyName("fc")]
        public string? FC { get; set; }

        /// <summary>
        /// 文件(夹)名称
        /// </summary>
        [JsonPropertyName("fn")]
        public string? FN { get; set; }

        /// <summary>
        /// 文件夹封面
        /// </summary>
        [JsonPropertyName("fco")]
        public string? FCO { get; set; }

        /// <summary>
        /// 是否星标，1：星标
        /// </summary>
        [JsonPropertyName("ism")]
        public string? IsM { get; set; }

        /// <summary>
        /// 是否加密；1：加密
        /// </summary>
        [JsonPropertyName("isp")]
        public int? IsP { get; set; }

        /// <summary>
        /// 文件提取码
        /// </summary>
        [JsonPropertyName("pc")]
        public string? PC { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        [JsonPropertyName("upt")]
        public long? UpT { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        [JsonPropertyName("uet")]
        public long? UeT { get; set; }

        /// <summary>
        /// 上传时间
        /// </summary>
        [JsonPropertyName("uppt")]
        public long? UppT { get; set; }

        [JsonPropertyName("cm")]
        public long? CM { get; set; }

        /// <summary>
        /// 文件备注
        /// </summary>
        [JsonPropertyName("fdesc")]
        public string? FDesc { get; set; }

        /// <summary>
        /// 文件备注
        /// </summary>
        [JsonPropertyName("ispl")]
        public int? IsPl { get; set; }

        /// <summary>
        /// 文件标签
        /// </summary>
        [JsonPropertyName("fl")]
        public FLabelDTO[]? FL { get; set; }

        /// <summary>
        /// sha1值
        /// </summary>
        [JsonPropertyName("sha1")]
        public string? Sha1 { get; set; }

        /// <summary>
        /// 文件大小
        /// </summary>
        [JsonPropertyName("fs")]
        public long? FS { get; set; }

        /// <summary>
        /// 文件状态 0/2 未上传完成，1 已上传完成
        /// </summary>
        [JsonPropertyName("fta")]
        public string? FTA { get; set; }

        /// <summary>
        /// 文件后缀名
        /// </summary>
        [JsonPropertyName("ico")]
        public string? ICO { get; set; }

        /// <summary>
        /// 音频长度
        /// </summary>
        [JsonPropertyName("fatr")]
        public string? FATR { get; set; }

        /// <summary>
        /// 是否为视频
        /// </summary>
        [JsonPropertyName("isv")]
        public int? IsV { get; set; }

        /// <summary>
        /// 视频清晰度；1:标清 2:高清 3:超清 4:1080P 5:4k;100:原画
        /// </summary>
        [JsonPropertyName("def")]
        public int? Def { get; set; }

        /// <summary>
        /// 视频清晰度；1:标清 2:高清 3:超清 4:1080P 5:4k;100:原画
        /// </summary>
        [JsonPropertyName("def2")]
        public int? Def2 { get; set; }

        /// <summary>
        /// 音视频时长
        /// </summary>
        [JsonPropertyName("play_long")]
        public long? PlayLong { get; set; }

        [JsonPropertyName("v_img")]
        public string? VImg { get; set; }

        /// <summary>
        /// 图片缩略图
        /// </summary>
        [JsonPropertyName("thumb")]
        public string? Thumb { get; set; }

        /// <summary>
        /// 原图地址
        /// </summary>
        [JsonPropertyName("uo")]
        public string? UO { get; set; }
    }

    public class FLabelDTO
    {
        /// <summary>
        /// 文件标签id
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// 文件标签名称
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// 文件标签排序
        /// </summary>
        [JsonPropertyName("sort")]
        public string? Sort { get; set; }

        /// <summary>
        /// 文件标签颜色
        /// </summary>
        [JsonPropertyName("color")]
        public string? Color { get; set; }

        /// <summary>
        /// 文件标签类型；0：最近使用；1：非最近使用；2：为默认标签
        /// </summary>
        [JsonPropertyName("is_default")]
        public int? IsDefault { get; set; }

        /// <summary>
        /// 文件标签更新时间
        /// </summary>
        [JsonPropertyName("update_time")]
        public long? UpdateTime { get; set; }

        /// <summary>
        /// 文件标签创建时间
        /// </summary>
        [JsonPropertyName("create_time")]
        public long? CreateTime { get; set; }
    }

    public class FPathDTO
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("aid")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long? AId { get; set; }

        [JsonPropertyName("cid")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long? CId { get; set; }

        [JsonPropertyName("pid")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long? PId { get; set; }

        [JsonPropertyName("isp")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long? IsP { get; set; }

        [JsonPropertyName("p_cid")]
        public string? PCId { get; set; }

        [JsonPropertyName("fv")]
        public string? FV { get; set; }
    }
}
