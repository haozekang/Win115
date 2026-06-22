using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using RestSharp;
using System;
using System.Threading.Tasks;
using Tanovo.ExtensionMethods;
using Win115.Dtos;
using Win115.Helpers;
using Win115.Properties;
using Win115.ViewModels;
using JsonSerializer = System.Text.Json.JsonSerializer;

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
            var req = new RestRequest(ApiResource.OpenRbRevert);
            req.AddParameter("tid", Id);
            var res = await App.ProApiClient.PostAsync(req);
            if (!res.IsSuccessful || res.Content.IsBlank())
            {
                return;
            }
            var dto = JsonSerializer.Deserialize<ProResponseDTO>(res.Content);
            if (dto is null)
            {
                return;
            }
            if (!dto.State)
            {
                await App.ShowMessageBar($"{dto.Message}", "错误", InfoBarSeverity.Error);
                return;
            }
            await App.ShowMessageBar($"文件/文件夹还原成功！", "成功", InfoBarSeverity.Success);
            var vm = App.Resolve<BackStationViewModel>();
            await vm.RecycleSelectedCommand.ExecuteAsync(null);
        }

        [RelayCommand]
        private async Task Delete()
        {
            var req = new RestRequest(ApiResource.OpenRbDel);
            req.AddParameter("tid", Id);
            var res = await App.ProApiClient.PostAsync(req);
            if (!res.IsSuccessful || res.Content.IsBlank())
            {
                return;
            }
            var dto = JsonSerializer.Deserialize<ProResponseDTO>(res.Content);
            if (dto is null)
            {
                return;
            }
            if (!dto.State)
            {
                await App.ShowMessageBar($"{dto.Message}", "错误", InfoBarSeverity.Error);
                return;
            }
            await App.ShowMessageBar($"删除成功！", "成功", InfoBarSeverity.Success);
            var vm = App.Resolve<BackStationViewModel>();
            await vm.RecycleSelectedCommand.ExecuteAsync(null);
        }
    }
}
