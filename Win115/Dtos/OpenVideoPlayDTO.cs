using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Win115.Dtos
{
    public class OpenVideoPlayDTO
    {
        /// <summary>
        /// 文件id
        /// </summary>
        [JsonPropertyName("file_id")]
        public string? FileId { get; set; }

        /// <summary>
        /// 文件父目录id
        /// </summary>
        [JsonPropertyName("parent_id")]
        public string? ParentId { get; set; }

        /// <summary>
        /// 文件名称
        /// </summary>
        [JsonPropertyName("file_name")]
        public string? FileName { get; set; }

        /// <summary>
        /// 文件大小
        /// </summary>
        [JsonPropertyName("file_size")]
        public string? FileSize { get; set; }

        /// <summary>
        /// 文件哈希值
        /// </summary>
        [JsonPropertyName("file_sha1")]
        public string? FileSha1 { get; set; }

        /// <summary>
        /// 文件类型
        /// </summary>
        [JsonPropertyName("file_type")]
        public string? FileType { get; set; }

        /// <summary>
        /// 文件是否加密隐藏；0：否；1：是
        /// </summary>
        [JsonPropertyName("is_private")]
        public string? IsPrivate { get; set; }

        /// <summary>
        /// 视频文件时长
        /// </summary>
        [JsonPropertyName("play_long")]
        public string? PlayLong { get; set; }

        /// <summary>
        /// 视频文件记忆选中的清晰度；1:标清 2:高清 3:超清 4:1080P 5:4k;100:原画
        /// </summary>
        [JsonPropertyName("user_def")]
        public int? UserDef { get; set; }

        /// <summary>
        /// 记忆视频旋转角度；0, 90, 180, 270
        /// </summary>
        [JsonPropertyName("user_rotate")]
        public int? UserRotate { get; set; }

        /// <summary>
        /// 视频翻转方向：0：不翻转；1：水平翻转；2：垂直翻转
        /// </summary>
        [JsonPropertyName("user_turn")]
        public int? UserTurn { get; set; }

        /// <summary>
        /// 视频多音轨列表
        /// </summary>
        [JsonPropertyName("multitrack_list")]
        public List<MultitrackItemDTO>? MultitrackList { get; set; }

        /// <summary>
        /// 视频所有用可切换的清晰度列表;1:标清 2:高清 3:超清 4:1080P 5:4k;100:原画
        /// </summary>
        [JsonPropertyName("definition_list_new")]
        public Dictionary<string, string>? DefinitionListNew { get; set; }

        /// <summary>
        /// 视频各清晰度的播放地址信息
        /// </summary>
        [JsonPropertyName("video_url")]
        public List<VideoUrlItemDTO>? VideoUrl { get; set; }


        public class VideoUrlItemDTO
        {
            /// <summary>
            /// 播放地址
            /// </summary>
            [JsonPropertyName("url")]
            public string? Url { get; set; }

            /// <summary>
            /// 视频高度
            /// </summary>
            [JsonPropertyName("height")]
            public int? Height { get; set; }

            /// <summary>
            /// 视频宽度
            /// </summary>
            [JsonPropertyName("width")]
            public int? Width { get; set; }

            /// <summary>
            /// 视频清晰度
            /// </summary>
            [JsonPropertyName("definition")]
            public int? Definition { get; set; }

            /// <summary>
            /// 视频清晰度名称
            /// </summary>
            [JsonPropertyName("title")]
            public string? Title { get; set; }

            /// <summary>
            /// 视频清晰度(新)
            /// </summary>
            [JsonPropertyName("definition_n")]
            public int? DefinitionN { get; set; }
        }


        public class MultitrackItemDTO
        {
            /// <summary>
            /// 音轨标题
            /// </summary>
            [JsonPropertyName("title")]
            public string? Title { get; set; }

            /// <summary>
            /// 音轨是否上次选中；1：选中
            /// </summary>
            [JsonPropertyName("is_selected")]
            public string? IsSelected { get; set; }
        }
    }
}
