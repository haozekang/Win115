using System.Text.Json.Serialization;

namespace Win115.Dtos
{
    public class OpenUserInfoDTO
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        [JsonPropertyName("user_id")]
        public long? UserId { get; set; }

        /// <summary>
        /// 用户名称
        /// </summary>
        [JsonPropertyName("user_name")]
        public string? UserName { get; set; }

        /// <summary>
        /// 小尺寸用户头像
        /// </summary>
        [JsonPropertyName("user_face_s")]
        public string? UserFaceS { get; set; }

        /// <summary>
        /// 中尺寸用户头像
        /// </summary>
        [JsonPropertyName("user_face_m")]
        public string? UserFaceM { get; set; }

        /// <summary>
        /// 大尺寸用户头像
        /// </summary>
        [JsonPropertyName("user_face_l")]
        public string? UserFaceL { get; set; }

        /// <summary>
        /// 用户空间信息
        /// </summary>
        [JsonPropertyName("rt_space_info")]
        public RtSpaceInfoDTO? RtSpaceInfo { get; set; }

        /// <summary>
        /// 用户vip等级信息
        /// </summary>
        [JsonPropertyName("vip_info")]
        public VipInfoDTO? VipInfo { get; set; }
    }

    public class VipInfoDTO
    {
        /// <summary>
        /// vip等级名称；原石会员、尝鲜VIP、体验VIP、月费VIP、年费VIP、年费VIP高级版、年费VIP特级版、超级VIP、长期VIP；
        /// </summary>
        [JsonPropertyName("level_name")]
        public string? LevelName { get; set; }

        /// <summary>
        /// 过期时间戳
        /// </summary>
        [JsonPropertyName("expire")]
        public long? Expire { get; set; }
    }

    public class RtSpaceInfoDTO
    {
        /// <summary>
        /// 用户总空间
        /// </summary>
        [JsonPropertyName("all_total")]
        public SizeDTO? AllTotal { get; set; }

        /// <summary>
        /// 用户剩余空间
        /// </summary>
        [JsonPropertyName("all_remain")]
        public SizeDTO? AllRemain { get; set; }

        /// <summary>
        /// 用户已使用空间
        /// </summary>
        [JsonPropertyName("all_use")]
        public SizeDTO? AllUse { get; set; }
    }

    public class SizeDTO
    {
        /// <summary>
        /// 空间大小(字节)
        /// </summary>
        [JsonPropertyName("size")]
        public long? Size { get; set; }

        /// <summary>
        /// 空间大小(格式化)
        /// </summary>
        [JsonPropertyName("size_format")]
        public string? SizeFormat { get; set; }
    }
}
