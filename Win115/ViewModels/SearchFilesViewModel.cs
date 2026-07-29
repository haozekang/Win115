using Autofac;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tanovo.ExtensionMethods;
using Win115.Dtos;
using Win115.Enums;
using Win115.Helpers;
using Win115.Models;
using Win115.Properties;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Win115.ViewModels
{
    public partial class SearchFilesViewModel : ObservableRecipient
    {
        private DownloadListViewModel _downloadListViewModel { get; set; }
        private MyFilesViewModel _myFilesViewModel { get; set; }

        [ObservableProperty]
        public partial UserInfoModel User { get; set; }

        [ObservableProperty]
        public partial SystemInfoModel System { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanDo))]
        public partial bool IsBusy { get; set; } = false;

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

        public SearchFilesViewModel(UserInfoModel user, SystemInfoModel system, DownloadListViewModel downloadListViewModel, MyFilesViewModel myFilesViewModel)
        {
            User = user;
            System = system;
            _downloadListViewModel = downloadListViewModel;
            _myFilesViewModel = myFilesViewModel;
            FileItems = new();
            SelectedFileItems = new();

            Messenger.Register<ObservableRecipient, ValueChangedMessage<WeakMessengerTypes>, string>(this, nameof(MainViewModel), (r, msgType) =>
            {
                switch (msgType.Value)
                {
                    case WeakMessengerTypes.SignOut:
                        ClearData();
                        break;
                }
            });
        }

        /// <summary>
        /// 登出后，清理
        /// </summary>
        public async void ClearData()
        {
            HasSelectedItems = false;
            IsCheckAll = false;
            SelectedFileItems.Clear();
            FileItems.Clear();
            Total = 0;
            IsBusy = false;
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
                var dto = JsonSerializer.Deserialize<OpenUfileSearchDTO>(res.Content);
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
                    if (FileItems.Any(x => x.Id == f.FileId))
                    {
                        continue;
                    }
                    FileItems.Add(new MyFileItemModel
                    {
                        ParentId = f.ParentId,
                        Id = f.FileId,
                        Name = f.FileName,
                        FileType = f.FileCategory,
                        FileExtension = f.ICO,
                        FileState = f.AreaId,
                        Sha1 = f.Sha1,
                        Size = f.FileSize,
                        //CreateTime = f.UserPtime,
                        //UpdateTime = f.UserUtime,
                        PickCode = f.PickCode,
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
            bool add = await AddDownloadTasksAsync(SelectedFileItems, System.DownloadDirPath!);
            if (add)
            {
                await App.JumpPage(MenuKeys.DownloadList);
            }
        }

        [RelayCommand]
        public async Task DownloadRightMenu(object? item)
        {
            if (!User.IsLogin || (SelectedFileItems.IsBlank() && item is null)) return;
            if (System.DownloadDirPath.IsBlank())
            {
                await App.ShowMessageBar("请先在设置中设定下载默认目录！", "错误", InfoBarSeverity.Error, autoClose: TimeSpan.FromSeconds(5));
                return;
            }

            var items = SelectedFileItems.IsNotBlank()
                ? SelectedFileItems.ToList()
                : item is MyFileItemModel file ? [file] : [];
            if (await AddDownloadTasksAsync(items, System.DownloadDirPath!))
            {
                await App.JumpPage(MenuKeys.DownloadList);
            }
        }

        [RelayCommand]
        public async Task DownloadItem(object? item)
        {
            if (!User.IsLogin || item is not MyFileItemModel file)
            {
                return;
            }
            if (System.DownloadDirPath.IsBlank())
            {
                await App.ShowMessageBar("请先在设置中设定下载默认目录！", "错误", InfoBarSeverity.Error, autoClose: TimeSpan.FromSeconds(5));
                return;
            }

            if (await AddDownloadTasksAsync([file], System.DownloadDirPath!))
            {
                await App.JumpPage(MenuKeys.DownloadList);
            }
        }

        /// <summary>
        /// 在“我的文件”中打开目录，或定位文件。
        /// </summary>
        [RelayCommand]
        public async Task JumpTo(object? item)
        {
            if (!User.IsLogin || item is not MyFileItemModel file)
            {
                return;
            }

            await App.JumpPage(MenuKeys.MyFiles);
            await _myFilesViewModel.OpenSearchResultAsync(file);
        }

        /// <summary>
        /// 显示搜索结果详情。
        /// </summary>
        [RelayCommand]
        public async Task ShowDetail(object? item)
        {
            if (!User.IsLogin || item is not MyFileItemModel file)
            {
                return;
            }

            await _myFilesViewModel.ShowDetailForItemAsync(file);
        }

        private async Task<bool> AddDownloadTasksAsync(IEnumerable<MyFileItemModel> items, string targetDirectory)
        {
            var added = false;
            IsBusy = true;
            try
            {
                foreach (var item in items.ToList())
                {
                    if (item.FileType == "0")
                    {
                        added |= await AddFolderDownloadTasksAsync(item, targetDirectory);
                    }
                    else if (item.PickCode.IsNotBlank() && item.Name.IsNotBlank())
                    {
                        await _downloadListViewModel.AddTask(item.PickCode!, SanitizePathSegment(item.Name!), item.Size, targetDirectory);
                        added = true;
                    }
                }
            }
            catch (Exception ex)
            {
                await LogHelper.Error(ex);
                await App.ShowMessageBar("获取文件夹内容失败，请稍后重试。", "错误", InfoBarSeverity.Error, autoClose: TimeSpan.FromSeconds(5));
            }
            finally
            {
                IsBusy = false;
            }
            return added;
        }

        private async Task<bool> AddFolderDownloadTasksAsync(MyFileItemModel folder, string targetDirectory)
        {
            if (folder.Id.IsBlank() || folder.Name.IsBlank()) return false;

            const int pageSize = 1150;
            var rootDirectory = Path.Combine(targetDirectory, SanitizePathSegment(folder.Name!));
            Directory.CreateDirectory(rootDirectory);
            var parentTaskId = await _downloadListViewModel.BeginFolderTaskAsync(folder.Name!, rootDirectory, folder.Id!);
            var pendingFolders = new Queue<(string Id, string Directory)>();
            pendingFolders.Enqueue((folder.Id!, rootDirectory));
            var totalFiles = 0;
            long totalSize = 0;

            try
            {
                while (pendingFolders.Count > 0)
                {
                    var current = pendingFolders.Dequeue();
                    long offset = 0;
                    long totalCount;
                    FDataDTO[] entries;
                    do
                    {
                        var dto = await GetFolderPageAsync(current.Id, offset, pageSize);
                        entries = dto.Data ?? [];
                        totalCount = dto.Count ?? entries.LongLength;
                        foreach (var entry in entries)
                        {
                            if (entry.FN.IsBlank()) continue;
                            var safeName = SanitizePathSegment(entry.FN!);
                            if (entry.FC == "0" && entry.FId.IsNotBlank())
                            {
                                var childDirectory = Path.Combine(current.Directory, safeName);
                                Directory.CreateDirectory(childDirectory);
                                pendingFolders.Enqueue((entry.FId!, childDirectory));
                            }
                            else if (entry.PC.IsNotBlank())
                            {
                                await _downloadListViewModel.AddTask(entry.PC!, safeName, entry.FS, current.Directory, parentTaskId: parentTaskId);
                                totalFiles++;
                                totalSize += entry.FS ?? 0;
                            }
                        }
                        offset += entries.LongLength;
                    }
                    while (offset < totalCount && entries.Length > 0);
                }

                _downloadListViewModel.CompleteFolderCollection(parentTaskId, totalFiles, totalSize);
                return true;
            }
            catch
            {
                _downloadListViewModel.MarkFolderCollectionFailed(parentTaskId);
                throw;
            }
        }

        private static async Task<OpenUfileFilesDTO> GetFolderPageAsync(string folderId, long offset, int limit)
        {
            var request = new RestRequest(ApiResource.OpenUfileFiles);
            request.AddQueryParameter("cid", folderId);
            request.AddQueryParameter("limit", limit);
            request.AddQueryParameter("offset", offset);
            request.AddQueryParameter("show_dir", 1);
            var response = await App.ProApiClient.GetAsync(request);
            if (!response.IsSuccessful || response.Content.IsBlank())
            {
                throw new InvalidOperationException("Failed to retrieve the folder contents.");
            }

            var dto = JsonSerializer.Deserialize<OpenUfileFilesDTO>(response.Content);
            if (dto is null || !dto.State)
            {
                throw new InvalidOperationException(dto?.Message ?? "Failed to retrieve the folder contents.");
            }
            return dto;
        }

        private static string SanitizePathSegment(string value)
        {
            var invalidCharacters = Path.GetInvalidFileNameChars();
            var sanitized = new string(value.Select(character => invalidCharacters.Contains(character) ? '_' : character).ToArray()).Trim();
            sanitized = sanitized.TrimEnd('.');
            return sanitized is "" or "." or ".." ? "_" : sanitized;
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
                    var dto = JsonSerializer.Deserialize<ProResponseDTO<string[]?>>(res.Content);
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
