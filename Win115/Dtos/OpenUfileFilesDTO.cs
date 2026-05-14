using Newtonsoft.Json;
using System.ComponentModel;

namespace Win115.Dtos
{
    public record OpenUfileFilesDTO
    {
        /// <summary>
        /// 排序
        /// </summary>
        [JsonProperty("order"), DefaultValue("")]
        public string? Order { get; set; }

        [JsonProperty("fields"), DefaultValue("")]
        public string? Fields { get; set; }

        [JsonProperty("stdir"), DefaultValue(0)]
        public long? STDir { get; set; }

        [JsonProperty("cur"), DefaultValue(0)]
        public long? Cur { get; set; }

        [JsonProperty("path"), DefaultValue(null)]
        public FPathDTO[]? Paths { get; set; }

        [JsonProperty("suffix"), DefaultValue("")]
        public string? Suffix { get; set; }

        [JsonProperty("type"), DefaultValue(0)]
        public int? Type { get; set; }

        [JsonProperty("star"), DefaultValue(0)]
        public int? Star { get; set; }

        [JsonProperty("record_open_time"), DefaultValue("")]
        public string? RecordOpenTime { get; set; }

        [JsonProperty("hide_data"), DefaultValue("")]
        public string? HideData { get; set; }

        [JsonProperty("sys_dir"), DefaultValue("")]
        public string? SysDir { get; set; }

        [JsonProperty("max_size"), DefaultValue(0)]
        public long? MaxSize { get; set; }

        [JsonProperty("min_size"), DefaultValue(0)]
        public long? MinSize { get; set; }

        [JsonProperty("is_asc"), DefaultValue(0)]
        public int? IsAsc { get; set; }

        [JsonProperty("cid"), DefaultValue(0)]
        public long? CId { get; set; }

        [JsonProperty("aid"), DefaultValue("")]
        public string? AId { get; set; }

        [JsonProperty("limit"), DefaultValue(0)]
        public long? Limit { get; set; }

        [JsonProperty("offset"), DefaultValue(0)]
        public long? Offset { get; set; }

        [JsonProperty("sys_count"), DefaultValue(0)]
        public long? SysCount { get; set; }

        [JsonProperty("count"), DefaultValue(0)]
        public long? Count { get; set; }

        [JsonProperty("data"), DefaultValue(null)]
        public FDataDTO[]? Data { get; set; }

        [JsonProperty("state"), DefaultValue(false)]
        public bool State { get; set; }

        [JsonProperty("code"), DefaultValue(0)]
        public int Code { get; set; }

        [JsonProperty("message"), DefaultValue("")]
        public string? Message { get; set; }
    }

    public record FDataDTO
    {
        /// <summary>
        /// 文件ID
        /// </summary>
        [JsonProperty("fid"), DefaultValue("")]
        public string? FId { get; set; }

        /// <summary>
        /// 文件的状态，aid 的别名。1 正常，7 删除(回收站)，120 彻底删除
        /// </summary>
        [JsonProperty("aid"), DefaultValue("")]
        public string? AId { get; set; }

        /// <summary>
        /// 父目录ID
        /// </summary>
        [JsonProperty("pid"), DefaultValue("")]
        public string? PId { get; set; }

        /// <summary>
        /// 文件分类。0 文件夹，1 文件
        /// </summary>
        [JsonProperty("fc"), DefaultValue("")]
        public string? FC { get; set; }

        /// <summary>
        /// 文件(夹)名称
        /// </summary>
        [JsonProperty("fn"), DefaultValue("")]
        public string? FN { get; set; }

        /// <summary>
        /// 文件夹封面
        /// </summary>
        [JsonProperty("fco"), DefaultValue("")]
        public string? FCO { get; set; }

        /// <summary>
        /// 是否星标，1：星标
        /// </summary>
        [JsonProperty("ism"), DefaultValue("")]
        public string? IsM { get; set; }

        /// <summary>
        /// 是否加密；1：加密
        /// </summary>
        [JsonProperty("isp"), DefaultValue(0)]
        public int? IsP { get; set; }

        /// <summary>
        /// 文件提取码
        /// </summary>
        [JsonProperty("pc"), DefaultValue("")]
        public string? PC { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        [JsonProperty("upt"), DefaultValue(null)]
        public long? UpT { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        [JsonProperty("uet"), DefaultValue(null)]
        public long? UeT { get; set; }

        /// <summary>
        /// 上传时间
        /// </summary>
        [JsonProperty("uppt"), DefaultValue(null)]
        public long? UppT { get; set; }

        [JsonProperty("cm"), DefaultValue(null)]
        public long? CM { get; set; }

        /// <summary>
        /// 文件备注
        /// </summary>
        [JsonProperty("fdesc"), DefaultValue("")]
        public string? FDesc { get; set; }

        /// <summary>
        /// 文件备注
        /// </summary>
        [JsonProperty("ispl"), DefaultValue(0)]
        public int? IsPl { get; set; }

        /// <summary>
        /// 文件标签
        /// </summary>
        [JsonProperty("fl"), DefaultValue(null)]
        public FLabelDTO[]? FL { get; set; }

        /// <summary>
        /// sha1值
        /// </summary>
        [JsonProperty("sha1"), DefaultValue("")]
        public string? Sha1 { get; set; }

        /// <summary>
        /// 文件大小
        /// </summary>
        [JsonProperty("fs"), DefaultValue(0)]
        public long? FS { get; set; }

        /// <summary>
        /// 文件状态 0/2 未上传完成，1 已上传完成
        /// </summary>
        [JsonProperty("fta"), DefaultValue(null)]
        public string? FTA { get; set; }

        /// <summary>
        /// 文件后缀名
        /// </summary>
        [JsonProperty("ico"), DefaultValue("")]
        public string? ICO { get; set; }

        /// <summary>
        /// 音频长度
        /// </summary>
        [JsonProperty("fatr"), DefaultValue("")]
        public string? FATR { get; set; }

        /// <summary>
        /// 是否为视频
        /// </summary>
        [JsonProperty("isv"), DefaultValue(0)]
        public int? IsV { get; set; }

        /// <summary>
        /// 视频清晰度；1:标清 2:高清 3:超清 4:1080P 5:4k;100:原画
        /// </summary>
        [JsonProperty("def"), DefaultValue(0)]
        public int? Def { get; set; }

        /// <summary>
        /// 视频清晰度；1:标清 2:高清 3:超清 4:1080P 5:4k;100:原画
        /// </summary>
        [JsonProperty("def2"), DefaultValue(0)]
        public int? Def2 { get; set; }

        /// <summary>
        /// 音视频时长
        /// </summary>
        [JsonProperty("play_long"), DefaultValue(null)]
        public long? PlayLong { get; set; }

        [JsonProperty("v_img"), DefaultValue(null)]
        public string? VImg { get; set; }

        /// <summary>
        /// 图片缩略图
        /// </summary>
        [JsonProperty("thumb"), DefaultValue(null)]
        public string? Thumb { get; set; }

        /// <summary>
        /// 原图地址
        /// </summary>
        [JsonProperty("uo"), DefaultValue(null)]
        public string? UO { get; set; }
    }

    public record FLabelDTO
    {
        /// <summary>
        /// 文件标签id
        /// </summary>
        [JsonProperty("id"), DefaultValue("")]
        public string? Id { get; set; }

        /// <summary>
        /// 文件标签名称
        /// </summary>
        [JsonProperty("name"), DefaultValue("")]
        public string? Name { get; set; }

        /// <summary>
        /// 文件标签排序
        /// </summary>
        [JsonProperty("sort"), DefaultValue("")]
        public string? Sort { get; set; }

        /// <summary>
        /// 文件标签颜色
        /// </summary>
        [JsonProperty("color"), DefaultValue("")]
        public string? Color { get; set; }

        /// <summary>
        /// 文件标签类型；0：最近使用；1：非最近使用；2：为默认标签
        /// </summary>
        [JsonProperty("is_default"), DefaultValue(null)]
        public int? IsDefault { get; set; }

        /// <summary>
        /// 文件标签更新时间
        /// </summary>
        [JsonProperty("update_time"), DefaultValue(null)]
        public long? UpdateTime { get; set; }

        /// <summary>
        /// 文件标签创建时间
        /// </summary>
        [JsonProperty("create_time"), DefaultValue(null)]
        public long? CreateTime { get; set; }
    }

    public record FPathDTO
    {
        [JsonProperty("name"), DefaultValue("")]
        public string? Name { get; set; }

        [JsonProperty("aid"), DefaultValue("")]
        public string? AId { get; set; }

        [JsonProperty("cid"), DefaultValue("")]
        public string? CId { get; set; }

        [JsonProperty("pid"), DefaultValue("")]
        public string? PId { get; set; }

        [JsonProperty("isp"), DefaultValue("")]
        public string? IsP { get; set; }

        [JsonProperty("p_cid"), DefaultValue("")]
        public string? PCId { get; set; }

        [JsonProperty("fv"), DefaultValue("")]
        public string? FV { get; set; }
    }
}
