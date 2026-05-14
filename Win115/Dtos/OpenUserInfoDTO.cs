using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Win115.Dtos
{
    public record OpenUserInfoDTO
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        [JsonProperty("user_id"), DefaultValue("")]
        public string? UserId { get; set; }

        /// <summary>
        /// 用户名称
        /// </summary>
        [JsonProperty("user_name"), DefaultValue("")]
        public string? UserName { get; set; }

        /// <summary>
        /// 小尺寸用户头像
        /// </summary>
        [JsonProperty("user_face_s"), DefaultValue("")]
        public string? UserFaceS { get; set; }

        /// <summary>
        /// 中尺寸用户头像
        /// </summary>
        [JsonProperty("user_face_m"), DefaultValue("")]
        public string? UserFaceM { get; set; }

        /// <summary>
        /// 大尺寸用户头像
        /// </summary>
        [JsonProperty("user_face_l"), DefaultValue("")]
        public string? UserFaceL { get; set; }

        /// <summary>
        /// 用户空间信息
        /// </summary>
        [JsonProperty("rt_space_info"), DefaultValue("")]
        public RtSpaceInfoDTO? RtSpaceInfo { get; set; }

        /// <summary>
        /// 用户vip等级信息
        /// </summary>
        [JsonProperty("vip_info"), DefaultValue("")]
        public VipInfoDTO? VipInfo { get; set; }
    }

    public record VipInfoDTO
    {
        /// <summary>
        /// vip等级名称；原石会员、尝鲜VIP、体验VIP、月费VIP、年费VIP、年费VIP高级版、年费VIP特级版、超级VIP、长期VIP；
        /// </summary>
        [JsonProperty("level_name"), DefaultValue("")]
        public string? LevelName { get; set; }

        /// <summary>
        /// 过期时间戳
        /// </summary>
        [JsonProperty("expire"), DefaultValue(0)]
        public long? Expire { get; set; }
    }

    public record RtSpaceInfoDTO
    {
        /// <summary>
        /// 用户总空间
        /// </summary>
        [JsonProperty("all_total"), DefaultValue(null)]
        public SizeDTO? AllTotal { get; set; }

        /// <summary>
        /// 用户剩余空间
        /// </summary>
        [JsonProperty("all_remain"), DefaultValue(null)]
        public SizeDTO? AllRemain { get; set; }

        /// <summary>
        /// 用户已使用空间
        /// </summary>
        [JsonProperty("all_use"), DefaultValue(null)]
        public SizeDTO? AllUse { get; set; }
    }

    public record SizeDTO
    {
        /// <summary>
        /// 空间大小(字节)
        /// </summary>
        [JsonProperty("size"), DefaultValue(0)]
        public long? Size { get; set; }

        /// <summary>
        /// 空间大小(格式化)
        /// </summary>
        [JsonProperty("size_format"), DefaultValue("")]
        public string? SizeFormat { get; set; }
    }
}
