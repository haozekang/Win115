using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Collections;
using LiteDB;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.XPath;
using Tanovo.ExtensionMethods;
using Win115.Dtos;
using Win115.Models;
using Win115.Properties;

namespace Win115.ViewModels
{
    public partial class BackStationViewModel : ObservableRecipient
    {
        private LiteDatabase _db;

        [ObservableProperty]
        public partial UserInfoModel User { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanClearAll))]
        public partial IncrementalLoadingCollection<RbFileIncrementalSource, RbFileItemModel> FileItems { get; set; }

        [ObservableProperty]
        public partial List<RbFileItemModel> SelectedFileItems { get; set; }

        [ObservableProperty]
        public partial bool? IsCheckAll { get; set; } = false;

        [ObservableProperty]
        public partial bool HasSelectedItems { get; set; } = false;

        [ObservableProperty]
        public partial bool CanClearAll { get; set; } = false;

        public BackStationViewModel(UserInfoModel user, LiteDatabase db)
        {
            User = user;
            _db = db;
            FileItems = new IncrementalLoadingCollection<RbFileIncrementalSource, RbFileItemModel>(new RbFileIncrementalSource());
            SelectedFileItems = new();
        }

        /// <summary>
        /// 登出后，清理
        /// </summary>
        [RelayCommand]
        public async Task ClearData()
        {
            App.DispatcherQueue?.TryEnqueue(() =>
            {
                SelectedFileItems.Clear();
                FileItems.Clear();
                HasSelectedItems = false;
                IsCheckAll = false;
                CanClearAll = false;
            });
        }

        [RelayCommand]
        private async Task RefreshFiles()
        {
            IsCheckAll = false;
            await FileItems.RefreshAsync();
        }

        [RelayCommand]
        private async Task RecycleSelected()
        {
            if (SelectedFileItems.IsBlank())
            {
                await App.ShowMessageBar("请选择需要还原的对象！", "警告", InfoBarSeverity.Warning);
                return;
            }
            ContentDialog dialog = new ContentDialog();
            dialog.XamlRoot = App.XamlRoot;
            dialog.Title = "信息";
            dialog.Content = "确认执行还原操作吗？";
            dialog.PrimaryButtonText = "确认";
            dialog.SecondaryButtonText = "取消";
            dialog.DefaultButton = ContentDialogButton.Secondary;
            dialog.PrimaryButtonClick += RecycleSelected;
            await dialog.ShowAsync();
        }

        private async void RecycleSelected(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var req = new RestRequest(ApiResource.OpenRbRevert);
            req.AddParameter("tid", string.Join(",", SelectedFileItems.Select(x => x.Id)));
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
                await App.ShowMessageBar($"{dto.Message}", "错误", InfoBarSeverity.Error);
                return;
            }
            await App.ShowMessageBar($"文件/文件夹还原成功！", "成功", InfoBarSeverity.Success);
            await RefreshFiles();
        }

        [RelayCommand]
        private async Task DeleteSelected()
        {
            if (SelectedFileItems.IsBlank())
            {
                await App.ShowMessageBar("请选择需要删除的对象！", "警告", InfoBarSeverity.Warning);
                return;
            }
            ContentDialog dialog = new ContentDialog();
            dialog.XamlRoot = App.XamlRoot;
            dialog.Title = "信息";
            dialog.Content = "确认执行删除操作吗？删除后不可恢复！";
            dialog.PrimaryButtonText = "确认";
            dialog.SecondaryButtonText = "取消";
            dialog.DefaultButton = ContentDialogButton.Secondary;
            dialog.PrimaryButtonClick += DeleteSelected;
            await dialog.ShowAsync();
        }

        private async void DeleteSelected(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var req = new RestRequest(ApiResource.OpenRbDel);
            req.AddParameter("tid", string.Join(",", SelectedFileItems.Select(x => x.Id)));
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
                await App.ShowMessageBar($"{dto.Message}", "错误", InfoBarSeverity.Error);
                return;
            }
            await App.ShowMessageBar($"删除成功！", "成功", InfoBarSeverity.Success);
            await RefreshFiles();
        }

        [RelayCommand]
        private async Task ClearAll()
        {
            if (FileItems.IsBlank())
            {
                await App.ShowMessageBar("无可清空的内容！", "警告", InfoBarSeverity.Warning);
                return;
            }
            ContentDialog dialog = new ContentDialog();
            dialog.XamlRoot = App.XamlRoot;
            dialog.Title = "信息";
            dialog.Content = "确认执行清空操作吗（最多1150个）？该操作后不可恢复！";
            dialog.PrimaryButtonText = "确认";
            dialog.SecondaryButtonText = "取消";
            dialog.DefaultButton = ContentDialogButton.Secondary;
            dialog.PrimaryButtonClick += ClearAll;
            await dialog.ShowAsync();
        }

        private async void ClearAll(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var req = new RestRequest(ApiResource.OpenRbDel);
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
                await App.ShowMessageBar($"{dto.Message}", "错误", InfoBarSeverity.Error);
                return;
            }
            await App.ShowMessageBar($"清空成功！", "成功", InfoBarSeverity.Success);
            await RefreshFiles();
        }
    }
}
