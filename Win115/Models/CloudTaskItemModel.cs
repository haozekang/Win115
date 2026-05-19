using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using LiteDB;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Newtonsoft.Json;
using Org.BouncyCastle.Tsp;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Tanovo.ExtensionMethods;
using Win115.Dtos;
using Win115.Entities;
using Win115.Helpers;
using Win115.Properties;
using Win115.ViewModels;
using Windows.ApplicationModel.DataTransfer;

namespace Win115.Models
{
    public partial class CloudTaskItemModel : ObservableObject
    {
        /// <summary>
        /// 任务sha1
        /// </summary>
        [ObservableProperty]
        public partial string? InfoHash { get; set; } = string.Empty;

        /// <summary>
        /// 任务添加时间
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AddTimeText))]
        public partial DateTime? AddTime { get; set; }

        public string? AddTimeText => AddTime.HasValue ? AddTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : "-";

        /// <summary>
        /// 任务下载进度
        /// </summary>
        [ObservableProperty]
        public partial long? PercentDone { get; set; } = 0;

        /// <summary>
        /// 任务总大小（字节）
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FileSizeText))]
        public partial long? FileSize { get; set; } = 0;

        public string? FileSizeText => FileSize > 0 ? StringHelper.FormatFileSize(FileSize) : "-";

        /// <summary>
        /// 任务名
        /// </summary>
        [ObservableProperty]
        public partial string? Name { get; set; } = string.Empty;

        /// <summary>
        /// 任务最后更新时间
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LastUpdateText))]
        public partial DateTime? LastUpdate { get; set; }

        public string? LastUpdateText => LastUpdate.HasValue ? LastUpdate.Value.ToString("yyyy-MM-dd HH:mm:ss") : "-";

        /// <summary>
        /// 任务源文件（夹）对应文件（夹）id
        /// </summary>
        [ObservableProperty]
        public partial string? FileId { get; set; } = string.Empty;

        /// <summary>
        /// 删除任务需删除源文件（夹）时，对应需传递的文件（夹）id
        /// </summary>
        [ObservableProperty]
        public partial string? DeleteFileId { get; set; } = string.Empty;

        /// <summary>
        /// 任务状态：-1下载失败；0分配中；1下载中；2下载成功
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StatusText))]
        public partial int? Status { get; set; } = null;
        public string? StatusText => Status switch
        {
            -1 => "下载失败",
            0 => "分配中",
            1 => "下载中",
            2 => "下载成功",
            _ => "-"
        };

        /// <summary>
        /// 链接任务url
        /// </summary>
        [ObservableProperty]
        public partial string? Url { get; set; } = string.Empty;

        /// <summary>
        /// 任务源文件所在父文件夹id
        /// </summary>
        [ObservableProperty]
        public partial string? WpPathId { get; set; } = string.Empty;

        /// <summary>
        /// 视频清晰度；1:标清 2:高清 3:超清 4:1080P 5:4k;100:原画
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Def2Text))]
        public partial int? Def2 { get; set; } = 0;
        public string? Def2Text => Status switch
        {
            1 => "标清",
            2 => "高清",
            3 => "超清",
            4 => "1080P",
            5 => "4k",
            100 => "原画",
            _ => "-"
        };

        /// <summary>
        /// 视频时长
        /// </summary>
        [ObservableProperty]
        public partial long? PlayLong { get; set; } = 0;

        /// <summary>
        /// 是否可申诉
        /// </summary>
        [ObservableProperty]
        public partial int? CanAppeal { get; set; } = 0;

        [ObservableProperty]
        public partial bool ShowDeleteTip { get; set; } = false;

        [RelayCommand]
        private async Task CopyUrl()
        {
            App.DispatcherQueue!.TryEnqueue(async() =>
            {
                DataPackage package = new();
                package.SetText(Url);
                Clipboard.SetContent(package);
                await App.ShowMessageBar($"复制成功", "信息", InfoBarSeverity.Success);
            });
        }

        [RelayCommand]
        private async Task Delete()
        {
            ContentDialog dialog = new ContentDialog();
            var content = new StackPanel();
            var checkBox = new CheckBox()
            {
                VerticalContentAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center,
                VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center,
                Content = "是否删除本地文件？"
            };
            dialog.Tag = checkBox;
            content.Children.Add(checkBox);
            dialog.XamlRoot = App.XamlRoot;
            dialog.Title = "警告";
            dialog.Content = content;
            dialog.PrimaryButtonText = "确认";
            dialog.SecondaryButtonText = "取消";
            dialog.DefaultButton = ContentDialogButton.Secondary;
            dialog.PrimaryButtonClick += Delete;
            await dialog.ShowAsync();
        }

        private async void Delete(ContentDialog dialog, ContentDialogButtonClickEventArgs args)
        {
            if (dialog.Tag is not CheckBox cmb)
            {
                return;
            }
            var req = new RestRequest(ApiResource.OpenOfflineDelTask);
            req.AddParameter("info_hash", InfoHash);
            if (cmb.IsChecked == true)
            {
                req.AddParameter("del_source_file", "1");
            }
            else
            {
                req.AddParameter("del_source_file", "0");
            }
            var res = await App.ProApiClient.PostAsync(req);
            if (!res.IsSuccessful || res.Content.IsBlank())
            {
                return;
            }
            var dto = JsonConvert.DeserializeObject<ProResponseDTO<object?>>(res.Content);
            if (dto is null)
            {
                return;
            }
            if (!dto.State)
            {
                App.DispatcherQueue!.TryEnqueue(async() => 
                {
                    await App.ShowMessageBar($"{dto.Message}", "错误", InfoBarSeverity.Error);
                });
                return;
            }
            var vm = App.Resolve<CloudDownloadViewModel>();
            vm.TaskItems.Remove(this);
        }
    }
}
