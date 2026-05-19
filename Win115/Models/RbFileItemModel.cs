using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteDB;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Org.BouncyCastle.Tsp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Win115.Entities;
using Win115.Helpers;
using Win115.Properties;
using Win115.ViewModels;

namespace Win115.Models
{
    public partial class RbFileItemModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ThumbImg))]
        public partial string? ThumbUrl { get; set; } = string.Empty;

        public ImageSource? ThumbImg => string.IsNullOrEmpty(ThumbUrl) ? null : new BitmapImage(new Uri(ThumbUrl, UriKind.RelativeOrAbsolute));

        [ObservableProperty]
        public partial string? Id { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string? FileName { get; set; } = string.Empty;

        /// <summary>
        /// 类型（1：文件，2：目录
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TypeText))]
        public partial int? Type { get; set; } = 0;

        public string? TypeText => Type switch
        {
            1 => "文件",
            2 => "目录",
            _ => "-"
        };

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FileSizeText))]
        public partial long? FileSize { get; set; } = 0;

        public string? FileSizeText => FileSize > 0 ? StringHelper.FormatFileSize(FileSize) : "-";

        /// <summary>
        /// 删除日期
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DeleteTimeText))]
        public partial DateTime? DeleteTime { get; set; }

        public string? DeleteTimeText => DeleteTime.HasValue ? DeleteTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : "-";

        /// <summary>
        /// 还原状态，-1 表示还原中，0 表示正常状态
        /// </summary>
        [ObservableProperty]
        public partial string? Status { get; set; } = string.Empty;

        /// <summary>
        /// 原文件的父目录id
        /// </summary>
        [ObservableProperty]
        public partial string? ParentId { get; set; } = string.Empty;

        /// <summary>
        /// 原文件的父目录名称
        /// </summary>
        [ObservableProperty]
        public partial string? ParentName { get; set; } = string.Empty;

        /// <summary>
        /// 文件提取码
        /// </summary>
        [ObservableProperty]
        public partial string? PickCode { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool ShowRecycleTip { get; set; } = false;

        [ObservableProperty]
        public partial bool ShowDeleteTip { get; set; } = false;

        [RelayCommand]
        private async Task Recycle()
        {
        }

        [RelayCommand]
        private async Task Delete()
        {
        }
    }
}
