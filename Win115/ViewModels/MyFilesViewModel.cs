using Autofac;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Collections;
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
using Win115.Enums;
using Win115.Helpers;
using Win115.Models;
using Win115.Properties;
using Win115.Views;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Win115.ViewModels
{
    public partial class MyFilesViewModel : ObservableRecipient
    {
        public readonly SemaphoreSlim RefreshSemaphore = new(1, 1);
        private DownloadListViewModel _downloadListViewModel { get; set; }

        [ObservableProperty]
        public partial UserInfoModel User { get; set; }

        [ObservableProperty]
        public partial SystemInfoModel System { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanDo))]
        [NotifyPropertyChangedFor(nameof(CanParentDirectory))]
        public partial bool IsBusy { get; set; } = false;

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

        public bool CanParentDirectory => PathItems.Count > 1 && CanDo;

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
        public partial IncrementalLoadingCollection<MyFileIncrementalSource, MyFileItemModel> FileItems { get; set; }

        [ObservableProperty]
        public partial List<MyFileItemModel> SelectedFileItems { get; set; }

        [ObservableProperty]
        public partial List<SelectOptionItem> PathItems { get; set; } = new()
        {
            new SelectOptionItem(-1, "文件")
        };

        public MyFilesViewModel(UserInfoModel user, SystemInfoModel system, DownloadListViewModel downloadListViewModel)
        {
            User = user;
            System = system;
            _downloadListViewModel = downloadListViewModel;
            FileItems = new IncrementalLoadingCollection<MyFileIncrementalSource, MyFileItemModel>(new MyFileIncrementalSource(-1, SortDirection, SortField));
            SelectedFileItems = new();
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
            if (!User.IsLogin)
            {
                return;
            }
            if (RefreshSemaphore.CurrentCount == 0)
            {
                return;
            }
            await RefreshSemaphore.WaitAsync();
            try
            {
                IsBusy = true;
                await App.UpdatePathBar();
                FileItems = new IncrementalLoadingCollection<MyFileIncrementalSource, MyFileItemModel>(new MyFileIncrementalSource(PathItems.Last().Id, SortDirection, SortField));
                //if (IsViewAllView)
                //{
                //    await FileItems.RefreshAsync();
                //}
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
                RefreshSemaphore.Release();
            }
        }

        /// <summary>
        /// 新建目录
        /// </summary>
        [RelayCommand]
        public async Task NewFolder()
        {
            if (!User.IsLogin)
            {
                return;
            }
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
            if (!User.IsLogin)
            {
                return;
            }
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
                await App.JumpPage(MenuKeys.DownloadList);
            }
        }

        /// <summary>
        /// 右键触发下载所选（没有所选择下载所在的item）
        /// </summary>
        [RelayCommand]
        public async Task DownloadRightMenu(object? item)
        {
            if (!User.IsLogin)
            {
                return;
            }
            if (SelectedFileItems.IsBlank() && item is null)
            {
                return;
            }
            if (System.DownloadDirPath.IsBlank())
            {
                await App.ShowMessageBar("请先在设置中设定下载默认目录！", "错误", InfoBarSeverity.Error, autoClose: TimeSpan.FromSeconds(5));
                return;
            }
            bool add = false;
            if (SelectedFileItems.IsNotBlank())
            {
                foreach (var s in SelectedFileItems)
                {
                    // 目录暂不支持
                    if (s.FileType == "0")
                    {
                        continue;
                    }
                    add = true;
                    await _downloadListViewModel.AddTask(s.PickCode!, s.Name!, s.Size, System.DownloadDirPath);
                }
            }
            else if(item is MyFileItemModel _item)
            {
                if (_item.FileType == "0")
                {
                    await App.ShowMessageBar("尚未支持目录下载！", "错误", InfoBarSeverity.Error, autoClose: TimeSpan.FromSeconds(5));
                    return;
                }
                add = true;
                await _downloadListViewModel.AddTask(_item.PickCode!, _item.Name!, _item.Size, System.DownloadDirPath);
            }
            if (add)
            {
                await App.JumpPage(MenuKeys.DownloadList);
            }
        }

        /// <summary>
        /// 右键触发下载，另存为
        /// </summary>
        [RelayCommand]
        public async Task DownloadOtherDirRightMenu(object? item)
        {
            if (!User.IsLogin)
            {
                return;
            }
            if (SelectedFileItems.IsBlank() && item is null)
            {
                return;
            }
            FolderPicker picker = new();
            picker.FileTypeFilter.Add("*");
            InitializeWithWindow.Initialize(picker, App.WindowHandle);
            StorageFolder? folder = await picker.PickSingleFolderAsync();
            if (folder == null || folder.Path.IsBlank())
            {
                return;
            }
            bool add = false;
            if (SelectedFileItems.IsNotBlank())
            {
                foreach (var s in SelectedFileItems)
                {
                    // 目录暂不支持
                    if (s.FileType == "0")
                    {
                        continue;
                    }
                    add = true;
                    await _downloadListViewModel.AddTask(s.PickCode!, s.Name!, s.Size, folder.Path);
                }
            }
            else if(item is MyFileItemModel _item)
            {
                if (_item.FileType == "0")
                {
                    await App.ShowMessageBar("尚未支持目录下载！", "错误", InfoBarSeverity.Error, autoClose: TimeSpan.FromSeconds(5));
                    return;
                }
                add = true;
                await _downloadListViewModel.AddTask(_item.PickCode!, _item.Name!, _item.Size, folder.Path);
            }
            if (add)
            {
                await App.JumpPage(MenuKeys.DownloadList);
            }
        }

        /// <summary>
        /// 复制到
        /// </summary>
        [RelayCommand]
        public async Task CopyTo()
        {
            if (!User.IsLogin)
            {
                return;
            }
        }

        /// <summary>
        /// 移动到
        /// </summary>
        [RelayCommand]
        public async Task MoveTo()
        {
            if (!User.IsLogin)
            {
                return;
            }
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
            try
            {
                if (folder is MyFileItemModel { FileType: "0", Id: not null, Name: not null } item)
                {
                    PathItems.Add(new SelectOptionItem(item.Id, item.Name));
                    await RefreshFiles();
                }
            }
            catch
            {
            }
        }
    }
}
