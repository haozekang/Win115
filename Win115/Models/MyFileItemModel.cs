using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Org.BouncyCastle.Tsp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Win115.Helpers;

namespace Win115.Models
{
    public partial class MyFileItemModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ThumbImg))]
        public partial string? ThumbUrl { get; set; } = string.Empty;

        public ImageSource? ThumbImg => string.IsNullOrEmpty(ThumbUrl) ? null : new BitmapImage(new Uri(ThumbUrl, UriKind.RelativeOrAbsolute));

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(OriginalImg))]
        public partial string? OriginalUrl { get; set; } = string.Empty;

        public ImageSource? OriginalImg => string.IsNullOrEmpty(OriginalUrl) ? null : new BitmapImage(new Uri(OriginalUrl, UriKind.RelativeOrAbsolute));

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(VideoImg))]
        public partial string? VideoUrl { get; set; } = string.Empty;

        public ImageSource? VideoImg => string.IsNullOrEmpty(VideoUrl) ? null : new BitmapImage(new Uri(VideoUrl, UriKind.RelativeOrAbsolute));

        [ObservableProperty]
        public partial string? Id { get; set; } = string.Empty;

        /// <summary>
        /// 父Id
        /// </summary>
        [ObservableProperty]
        public partial string? ParentId { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string? Name { get; set; } = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SizeText))]
        public partial long? Size { get; set; } = 0;

        public string? SizeText => Size > 0 ? StringHelper.FormatFileSize(Size) : "-";

        /// <summary>
        /// 文件夹封面
        /// </summary>
        [ObservableProperty]
        public partial string? FileCover { get; set; } = string.Empty;

        /// <summary>
        /// 文件提取码
        /// </summary>
        [ObservableProperty]
        public partial string? PickCode { get; set; } = string.Empty;

        /// <summary>
        /// 文件备注
        /// </summary>
        [ObservableProperty]
        public partial string? FileDesc { get; set; } = string.Empty;

        /// <summary>
        /// sha1值
        /// </summary>
        [ObservableProperty]
        public partial string? Sha1 { get; set; } = string.Empty;

        /// <summary>
        /// 文件状态 0/2 未上传完成，1 已上传完成
        /// </summary>
        [ObservableProperty]
        public partial string? FileState { get; set; } = string.Empty;

        /// <summary>
        /// 文件后缀名
        /// </summary>
        [ObservableProperty]
        public partial string? FileExtension { get; set; } = string.Empty;

        /// <summary>
        /// 音频长度
        /// </summary>
        [ObservableProperty]
        public partial string? AudioLength { get; set; } = string.Empty;

        /// <summary>
        /// 是否为视频
        /// </summary>
        [ObservableProperty]
        public partial int? IsVideo { get; set; }

        /// <summary>
        /// 是否为加密
        /// </summary>
        [ObservableProperty]
        public partial int? IsEncrypted { get; set; }

        /// <summary>
        /// 视频清晰度；1:标清 2:高清 3:超清 4:1080P 5:4k;100:原画
        /// </summary>
        [ObservableProperty]
        public partial int? VideoResolution { get; set; }

        /// <summary>
        /// 视频清晰度；1:标清 2:高清 3:超清 4:1080P 5:4k;100:原画
        /// </summary>
        [ObservableProperty]
        public partial int? VideoResolution2 { get; set; }

        /// <summary>
        /// 音视频时长
        /// </summary>
        [ObservableProperty]
        public partial long? PlayLong { get; set; }

        /// <summary>
        /// 一级筛选大分类，1：文档，2：图片，3：音乐，4：视频，5：压缩包，6：应用
        /// </summary>
        [ObservableProperty]
        public partial string? Type { get; set; } = string.Empty;

        /// <summary>
        /// 文件分类。0 文件夹，1 文件
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FileTypeText))]
        [NotifyPropertyChangedFor(nameof(FileTypeIcon))]
        public partial string? FileType { get; set; } = string.Empty;

        public string FileTypeText => FileType switch
        {
            "0" => "文件夹",
            "1" => "文件",
            _ => "-"
        };

        public string FileTypeIcon => FileType switch 
        {
            "0" => "\uE8B7",
            "1" => FileExtension?.ToLower() switch 
            {
                "msi" or
                "jar" or
                "exe" => "\uE977",
                "iso" => "\uE958",
                "txt" => "\uE8D2",
                "mp3" => "\uE8D6",
                "mp4" => "\uE8B2",
                "png" or
                "jpg" or
                "jpeg" => "\uE91B",
                "zip" or
                "rar" or
                "tar" or
                "gz" or
                "rpm" or
                "7z" => "\uF012",
                "cs" or
                "json" => "\uE943",
                "java" => "\uE943",
                "js" => "\uE943",
                "gif" => "\uF4A9",
                "pdf" => "\uEA90",
                _ => "\uE9CE"
            },
            _ => "\uE9CE"
        };

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CreateTimeText))]
        public partial DateTime? CreateTime { get; set; } = null;

        public string? CreateTimeText => CreateTime.HasValue ? CreateTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : "-";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(UpdateTimeText))]
        public partial DateTime? UpdateTime { get; set; } = null;

        public string? UpdateTimeText => UpdateTime.HasValue ? UpdateTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : "-";
    }
}
