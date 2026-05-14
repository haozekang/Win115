using Autofac;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.X509;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Xml.XPath;
using Tanovo.ExtensionMethods;
using Win115.Dtos;
using Win115.Helpers;
using Win115.Models;
using Win115.Properties;
using Win115.Views;
using Windows.Foundation;

namespace Win115.ViewModels
{
    public partial class SearchFilesViewModel : ObservableRecipient
    {
        private DownloadListViewModel _downloadListViewModel { get; set; }

        [ObservableProperty]
        public partial UserInfoModel User { get; set; }

        [ObservableProperty]
        public partial SystemInfoModel System { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanDo))]
        public partial bool IsBusy { get; set; } = false;

        [ObservableProperty]
        public partial long Offset { get; set; } = 0;

        [ObservableProperty]
        public partial int Total { get; set; } = 0;

        [ObservableProperty]
        public partial string? SearchValue { get; set; } = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsViewAllView))]
        public partial Visibility ViewAllVisibility { get; set; } = Visibility.Collapsed;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsListView))]
        public partial Visibility ListVisibility { get; set; } = Visibility.Visible;

        public bool IsViewAllView => ViewAllVisibility == Visibility.Visible;

        public bool IsListView => ListVisibility == Visibility.Visible;

        public bool CanDo => !IsBusy && User.IsLogin;

        [ObservableProperty]
        public partial bool HasSelectedItems { get; set; } = false;

        [ObservableProperty]
        public partial bool? IsCheckAll { get; set; } = false;

        [ObservableProperty]
        public partial ObservableCollection<MyFileItemModel> FileItems { get; set; }

        [ObservableProperty]
        public partial ObservableCollection<MyFileItemModel> SelectedFileItems { get; set; }

        public SearchFilesViewModel(UserInfoModel user, SystemInfoModel system, DownloadListViewModel downloadListViewModel)
        {
            User = user;
            System = system;
            _downloadListViewModel = downloadListViewModel;
            FileItems = new();
            SelectedFileItems = new();
        }

        /// <summary>
        /// 刷新
        /// </summary>
        [RelayCommand]
        public async Task RefreshFiles()
        {
            if (!User.IsLogin)
            {
                return;
            }
            IsBusy = true;
            FileItems.Clear();
            var req = new RestRequest(ApiResource.OpenUfileSearch);
            req.AddQueryParameter("search_value", SearchValue);
            req.AddQueryParameter("limit", 1000);
            req.AddQueryParameter("offset", 0);
            req.AddQueryParameter("file_label", "1");

            try
            {
                var res = await App.ProApiClient.GetAsync(req);
                if (!res.IsSuccessful || res.Content.IsBlank())
                {
                    return;
                }
                var dto = JsonConvert.DeserializeObject<OpenUfileFilesDTO>(res.Content);
                if (dto is null)
                {
                    await App.ShowMessageBar("序列化失败！", "错误", InfoBarSeverity.Error);
                    return;
                }
                if (dto.State != true)
                {
                    await App.ShowMessageBar(dto.Message ?? "未知错误", "错误", InfoBarSeverity.Error, autoClose: TimeSpan.FromSeconds(5));
                    return;
                }
                if (dto.Data is null)
                {
                    return;
                }
                foreach (var f in dto.Data)
                {
                    if (FileItems.Any(x => x.Id == f.FId))
                    {
                        continue;
                    }
                    FileItems.Add(new MyFileItemModel
                    {
                        ParentId = f.PId,
                        Id = f.FId,
                        Name = f.FN,
                        FileType = f.FC,
                        FileCover = f.FCO,
                        FileExtension = f.ICO,
                        FileState = f.FTA,
                        Sha1 = f.Sha1,
                        IsVideo = f.IsV,
                        IsEncrypted = f.IsP,
                        Size = f.FS,
                        CreateTime = f.UppT?.TimeStampToDateTime(),
                        UpdateTime = f.UpT?.TimeStampToDateTime(),
                        PickCode = f.PC,
                        FileDesc = f.FDesc,
                        AudioLength = f.FATR,
                        VideoResolution = f.Def,
                        VideoResolution2 = f.Def2,
                        PlayLong = f.PlayLong,
                        VideoUrl = f.VImg,
                        ThumbUrl = f.Thumb,
                        OriginalUrl = f.UO,
                    });
                }
                Total = FileItems.Count;
            }
            catch (Exception ex)
            {
                await LogHelper.Error(ex);
            }
            finally
            {
                SelectedFileItems.Clear();
                HasSelectedItems = false;
                IsCheckAll = false;
                IsBusy = false;
            }
        }

        /// <summary>
        /// 下载所选
        /// </summary>
        [RelayCommand]
        public async Task DownloadSelected()
        {
            if (SelectedFileItems.IsBlank())
            {
                return;
            }
            if (System.DownloadDirPath.IsBlank())
            {
                await App.ShowMessageBar("请先在设置中设定下载默认目录！", "错误", InfoBarSeverity.Error, autoClose: TimeSpan.FromSeconds(5));
                return;
            }
            bool add = false;
            foreach (var item in SelectedFileItems)
            {
                // 目录暂不支持
                if (item.FileType == "0")
                {
                    continue;
                }
                add = true;
                await _downloadListViewModel.AddTask(item.PickCode!, item.Name!, item.Size, System.DownloadDirPath);
            }
            if (add)
            {
                await App.JumpPage(typeof(DownloadListPage));
            }
        }

        /// <summary>
        /// 复制到
        /// </summary>
        [RelayCommand]
        public async Task CopyTo()
        {
        }

        /// <summary>
        /// 移动到
        /// </summary>
        [RelayCommand]
        public async Task MoveTo()
        {
        }

        /// <summary>
        /// 删除所选
        /// </summary>
        [RelayCommand]
        public async Task DeleteSelected()
        {
            if (!User.IsLogin)
            {
                return;
            }
            if (SelectedFileItems.IsBlank())
            {
                return;
            }
            ContentDialog dialog = new ContentDialog();
            dialog.XamlRoot = App.XamlRoot;
            dialog.Title = "警告";
            dialog.Content = "确认执行删除操作吗？删除后可在回收站进行恢复。";
            dialog.PrimaryButtonText = "确认";
            dialog.SecondaryButtonText = "取消";
            dialog.DefaultButton = ContentDialogButton.Secondary;
            dialog.PrimaryButtonClick += DeleteSelected;
            await dialog.ShowAsync();
        }

        private async void DeleteSelected(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (SelectedFileItems.IsBlank())
            {
                return;
            }
            try
            {
                var pids = SelectedFileItems.GroupBy(x => x.ParentId).Select(x => x.Key);
                foreach (var pid in pids)
                {
                    var ids = string.Join(",", SelectedFileItems.Where(x => x.ParentId == pid).Select(x => x.Id).ToList());
                    var req = new RestRequest(ApiResource.OpenUfileDelete);
                    req.AddOrUpdateParameter("file_ids", ids);
                    req.AddOrUpdateParameter("parent_id", pid);
                    req.AlwaysMultipartFormData = true;
                    var res = await App.ProApiClient.PostAsync(req);
                    if (!res.IsSuccessful || res.Content.IsBlank())
                    {
                        continue;
                    }
                    var dto = JsonConvert.DeserializeObject<ProResponseDTO<string[]?>>(res.Content);
                    if (dto is null || !dto.State || dto.Data is null)
                    {
                        continue;
                    }
                }
                await RefreshFiles();
            }
            catch (Exception ex)
            {
                await LogHelper.Error(ex);
            }
        }

        /// <summary>
        /// 进入目录
        /// </summary>
        [RelayCommand]
        public async Task EnterFolder(object folder)
        {
            if (!User.IsLogin)
            {
                return;
            }
        }
    }
}
