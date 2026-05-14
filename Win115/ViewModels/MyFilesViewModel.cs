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
using System.Threading;
using System.Threading.Tasks;
using Tanovo.ExtensionMethods;
using Win115.Dtos;
using Win115.Helpers;
using Win115.Models;
using Win115.Properties;
using Win115.Views;
using Windows.Foundation;

namespace Win115.ViewModels
{
    public partial class MyFilesViewModel : ObservableRecipient
    {
        public readonly SemaphoreSlim RefreshSemaphore = new(1, 1);
        private DownloadListViewModel _downloadListViewModel { get; set; }

        public List<SelectOptionItem> PageSizeItems { get; } = new List<SelectOptionItem>() 
        {
            new SelectOptionItem(20, $"20/页"),
            new SelectOptionItem(50, $"50/页"),
            new SelectOptionItem(100, $"100/页"),
            new SelectOptionItem(200, $"200/页"),
        };

        [ObservableProperty]
        public partial UserInfoModel User { get; set; }

        [ObservableProperty]
        public partial SystemInfoModel System { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanDo))]
        public partial bool IsBusy { get; set; } = false;

        [ObservableProperty]
        public partial long CurPageTotal { get; set; } = 0;

        [ObservableProperty]
        public partial SelectOptionItem SelectedPageSize { get; set; }

        [ObservableProperty]
        public partial long Offset { get; set; } = 0;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsViewAllView))]
        public partial Visibility ViewAllVisibility { get; set; } = Visibility.Collapsed;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsListView))]
        public partial Visibility ListVisibility { get; set; } = Visibility.Visible;

        public bool IsViewAllView => ViewAllVisibility == Visibility.Visible;

        public bool IsListView => ListVisibility == Visibility.Visible;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanPreviousPage))]
        [NotifyPropertyChangedFor(nameof(CanNextPage))]
        [NotifyPropertyChangedFor(nameof(PageIndexP))]
        [NotifyPropertyChangedFor(nameof(PageIndexN))]
        [NotifyPropertyChangedFor(nameof(PageIndexPVisibility))]
        [NotifyPropertyChangedFor(nameof(PageIndexNVisibility))]
        public partial long PageIndex { get; set; } = 1;

        public long PageIndexP => PageIndex - 1;

        public long PageIndexN => PageIndex + 1;

        public Visibility PageIndexPVisibility => PageIndex > 1 ? Visibility.Visible : Visibility.Collapsed;

        public Visibility PageIndexNVisibility => PageIndex < PageCount ? Visibility.Visible : Visibility.Collapsed;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanPreviousPage))]
        [NotifyPropertyChangedFor(nameof(CanNextPage))]
        [NotifyPropertyChangedFor(nameof(PageIndexP))]
        [NotifyPropertyChangedFor(nameof(PageIndexN))]
        [NotifyPropertyChangedFor(nameof(PageIndexPVisibility))]
        [NotifyPropertyChangedFor(nameof(PageIndexNVisibility))]
        [NotifyPropertyChangedFor(nameof(PageMax))]
        public partial long PageCount { get; set; } = 1;

        public double PageMax => PageCount;

        public bool CanDo => !IsBusy && User.IsLogin;

        [ObservableProperty]
        public partial bool CanPreviousPage { get; set; } = false;

        [ObservableProperty]
        public partial bool CanNextPage { get; set; } = false;

        [ObservableProperty]
        public partial bool CanParentDirectory { get; set; } = false;

        [ObservableProperty]
        public partial bool HasSelectedItems { get; set; } = false;

        [ObservableProperty]
        public partial bool? IsCheckAll { get; set; } = false;

        [ObservableProperty]
        public partial Visibility SortNameUpVisibility { get; set; } = Visibility.Visible;

        [ObservableProperty]
        public partial Visibility SortNameDownVisibility { get; set; } = Visibility.Collapsed;

        [ObservableProperty]
        public partial Visibility SortSizeUpVisibility { get; set; } = Visibility.Collapsed;

        [ObservableProperty]
        public partial Visibility SortSizeDownVisibility { get; set; } = Visibility.Collapsed;

        [ObservableProperty]
        public partial Visibility SortFileTypeUpVisibility { get; set; } = Visibility.Collapsed;

        [ObservableProperty]
        public partial Visibility SortFileTypeDownVisibility { get; set; } = Visibility.Collapsed;

        [ObservableProperty]
        public partial Visibility SortCreateTimeUpVisibility { get; set; } = Visibility.Collapsed;

        [ObservableProperty]
        public partial Visibility SortCreateTimeDownVisibility { get; set; } = Visibility.Collapsed;

        [ObservableProperty]
        public partial Visibility SortUpdateTimeUpVisibility { get; set; } = Visibility.Collapsed;

        [ObservableProperty]
        public partial Visibility SortUpdateTimeDownVisibility { get; set; } = Visibility.Collapsed;

        [ObservableProperty]
        public partial string SortDirection { get; set; } = "1";

        [ObservableProperty]
        public partial string SortField { get; set; } = "file_name";

        [ObservableProperty]
        public partial ObservableCollection<MyFileItemModel> FileItems { get; set; }

        [ObservableProperty]
        public partial ObservableCollection<MyFileItemModel> SelectedFileItems { get; set; }

        [ObservableProperty]
        public partial ObservableCollection<SelectOptionItem> PathItems { get; set; }

        public MyFilesViewModel(UserInfoModel user, SystemInfoModel system, DownloadListViewModel downloadListViewModel)
        {
            User = user;
            System = system;
            _downloadListViewModel = downloadListViewModel;
            FileItems = new();
            SelectedFileItems = new();
            PathItems = new()
            {
                new SelectOptionItem(-1, "文件")
            };
            SelectedPageSize = PageSizeItems.First();
        }

        /// <summary>
        /// 返回上级目录
        /// </summary>
        [RelayCommand]
        public async Task ParentDirectory()
        {
            if (!User.IsLogin)
            {
                return;
            }
            if (PathItems.Count <= 1)
            {
                return;
            }
            PathItems.RemoveAt(PathItems.Count - 1);
            await RefreshFiles();
        }

        /// <summary>
        /// 刷新
        /// </summary>
        [RelayCommand]
        public async Task RefreshFiles()
        {
            if (RefreshSemaphore.CurrentCount == 0)
            {
                return;
            }
            await RefreshSemaphore.WaitAsync();
            if (!User.IsLogin)
            {
                return;
            }
            try
            {
                IsBusy = true;
                FileItems.Clear();
                var req = new RestRequest(ApiResource.OpenUfileFiles);
                if (PathItems.Last().Id != -1)
                {
                    req.AddQueryParameter("cid", PathItems.Last().Id);
                }
                req.AddQueryParameter("limit", SelectedPageSize.Id);
                req.AddQueryParameter("offset", (PageIndex - 1) * SelectedPageSize.Id);
                req.AddQueryParameter("asc", SortDirection);
                req.AddQueryParameter("o", SortField);
                req.AddQueryParameter("custom_order", "1");
                req.AddQueryParameter("cur", 1);
                req.AddQueryParameter("show_dir", 1);

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
                var pageSize = SelectedPageSize.Id;
                PageCount = (long)Math.Ceiling((dto.Count ?? 0d) / pageSize);
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
                CurPageTotal = FileItems.Count;
            }
            catch (Exception ex)
            {
                await LogHelper.Error(ex);
            }
            finally
            {
                CanPreviousPage = PageIndex > 1 && PageCount > 1 && User.IsLogin;
                CanNextPage = PageIndex < PageCount && PageCount > 1 && User.IsLogin;
                CanParentDirectory = PathItems.Count > 1 && User.IsLogin;
                SelectedFileItems.Clear();
                HasSelectedItems = false;
                IsCheckAll = false;
                IsBusy = false;
                RefreshSemaphore.Release();
            }
        }

        /// <summary>
        /// 新建目录
        /// </summary>
        [RelayCommand]
        public async Task NewFolder()
        {
            using var scope = App.CreateScope();
            var vm = scope.Resolve<NewFolderViewModel>();
            NewFolderContentDialog dialog = new NewFolderContentDialog(vm);
            dialog.XamlRoot = App.XamlRoot;
            dialog.Title = "新建文件夹";
            dialog.PrimaryButtonText = "保存";
            dialog.SecondaryButtonText = "取消";
            dialog.DefaultButton = ContentDialogButton.Primary;
            dialog.PrimaryButtonClick += NewFolder;
            await dialog.ShowAsync();
        }

        private async void NewFolder(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (sender is not NewFolderContentDialog dialog || dialog.ViewModel is not NewFolderViewModel vm)
            {
                return;
            }
            if (vm.FileName.IsBlank())
            {
                args.Cancel = true;
                return;
            }
            var deferral = args.GetDeferral();
            try
            {
                var req = new RestRequest(ApiResource.OpenFolderAdd);
                if (PathItems.Count == 1)
                {
                    req.AddOrUpdateParameter("pid", "0");
                }
                req.AddOrUpdateParameter("file_name", vm.FileName);
                req.AlwaysMultipartFormData = true;
                var res = await App.ProApiClient.PostAsync(req);
                if (!res.IsSuccessful || res.Content.IsBlank())
                {
                    return;
                }
                var dto = JsonConvert.DeserializeObject<ProResponseDTO<OpenFolderAddDTO>>(res.Content);
                if (dto is null || !dto.State || dto.Data is null)
                {
                    return;
                }
                await RefreshFiles();
            }
            catch(Exception ex)
            {
                await LogHelper.Error(ex);
            }
            finally
            {
                deferral.Complete();
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
                var ids = string.Join(",", SelectedFileItems.Select(x => x.Id).ToList());
                var req = new RestRequest(ApiResource.OpenUfileDelete);
                req.AddOrUpdateParameter("file_ids", ids);
                req.AddOrUpdateParameter("parent_id", PathItems.Last().Id);
                req.AlwaysMultipartFormData = true;
                var res = await App.ProApiClient.PostAsync(req);
                if (!res.IsSuccessful || res.Content.IsBlank())
                {
                    return;
                }
                var dto = JsonConvert.DeserializeObject<ProResponseDTO<string[]?>>(res.Content);
                if (dto is null || !dto.State || dto.Data is null)
                {
                    return;
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
            if (folder is MyFileItemModel { FileType: "0", Id: not null, Name: not null } item )
            {
                PathItems.Add(new SelectOptionItem(item.Id, item.Name));
                await RefreshFiles();
            }
        }
    }
}
