using Autofac;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.WinUI.Collections;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tanovo.ExtensionMethods;
using Win115.Dtos;
using Win115.Enums;
using Win115.Helpers;
using Win115.Models;
using Win115.Properties;
using Win115.Views;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using JsonSerializer = System.Text.Json.JsonSerializer;

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
        public partial IncrementalLoadingCollection<MyFileImageIncrementalSource, MyFileItemModel> ImageFileItems { get; set; }

        [ObservableProperty]
        public partial IncrementalLoadingCollection<MyFileMediaIncrementalSource, MyFileItemModel> MediaFileItems { get; set; }

        [ObservableProperty]
        public partial List<MyFileItemModel> SelectedFileItems { get; set; }

        [ObservableProperty]
        public partial List<SelectOptionItem> PathItems { get; set; } = new()
        {
            new SelectOptionItem(-1, "根目录")
        };

        public MyFilesViewModel(UserInfoModel user, SystemInfoModel system, DownloadListViewModel downloadListViewModel)
        {
            User = user;
            System = system;
            _downloadListViewModel = downloadListViewModel;
            FileItems = new IncrementalLoadingCollection<MyFileIncrementalSource, MyFileItemModel>(new MyFileIncrementalSource(-1, SortDirection, SortField), 100);
            ImageFileItems = new IncrementalLoadingCollection<MyFileImageIncrementalSource, MyFileItemModel>(new MyFileImageIncrementalSource(-1, SortDirection, SortField), 100);
            MediaFileItems = new IncrementalLoadingCollection<MyFileMediaIncrementalSource, MyFileItemModel>(new MyFileMediaIncrementalSource(-1, SortDirection, SortField), 100);
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
            PathItems = new()
            {
                new SelectOptionItem(-1, "根目录")
            };
            SelectedFileItems.Clear();
            FileItems.Clear();
            ImageFileItems.Clear();
            MediaFileItems.Clear();
            HasSelectedItems = false;
            IsCheckAll = false;
        }

        /// <summary>
        /// 显示文件/文件夹详情
        /// </summary>
        [RelayCommand]
        public async Task ShowDetail()
        {
            if (!User.IsLogin)
            {
                return;
            }
            if (SelectedFileItems.Count > 1)
            {
                return;
            }
            var item = SelectedFileItems.First();
            var req = new RestRequest(ApiResource.OpenFolderGetInfo);
            req.AddQueryParameter("file_id", item.Id);
            var res = await App.ProApiClient.GetAsync(req);
            if (!res.IsSuccessful || res.Content.IsBlank())
            {
                return;
            }
            var state = JsonSerializer.Deserialize<ProResponseDTO>(res.Content);
            if (state is null || !state.State)
            {
                return;
            }
            OpenFolderGetInfoDTO? info = null;
            try
            {
                var dto = JsonSerializer.Deserialize<ProResponseDTO<OpenFolderGetInfoDTO?>>(res.Content);
                info = dto?.Data;
            }
            catch (Exception e)
            {
                await LogHelper.Error(e);
            }
            if (info is null)
            {
                await App.ShowMessageBar("详情获取失败！", "错误", InfoBarSeverity.Error, autoClose: TimeSpan.FromSeconds(5));
                return;
            }
            var content = new StackPanel()
            {
                Orientation = Orientation.Vertical,
                Spacing = 8,
            };
            AddLabelAndTextToContent(content, "名称：", info.FileName);
            AddLabelAndTextToContent(content, "类型：", info.FileCategory == "1" ? "文件" : "文件夹");
            AddLabelAndTextToContent(content, "大小：", info.Size);
            if (info.Sha1.IsNotBlank())
            {
                AddLabelAndTextToContent(content, "SHA1：", info.Sha1);
            }
            AddLabelAndTextToContent(content, "包含：", $"{info.Count}个文件，{info.FolderCount}个文件夹");
            if (info.PlayLong is not null && info.PlayLong > 0)
            {
                AddLabelAndTimeLongToContent(content, "音视频时长：", info.PlayLong);
            }
            if (info.Ptime.IsNotBlank() && info.Ptime.IsNumber())
            {
                AddLabelAndTimeToContent(content, "创建时间：", info.Ptime.ToLong());
            }
            if (info.Utime.IsNotBlank() && info.Utime.IsNumber())
            {
                AddLabelAndTimeToContent(content, "修改时间：", info.Utime.ToLong());
            }
            if (info.OpenTime is not null && info.OpenTime > 0)
            {
                AddLabelAndTimeToContent(content, "上次打开时间：", info.OpenTime);
            }
            if (info.Paths is not null && info.Paths.Length > 0)
            {
                AddLabelAndTextToContent(content, "位置：", string.Join(" / ", info.Paths.Select(p => p.FileName)));
            }
            ContentDialog dialog = new ContentDialog();
            dialog.XamlRoot = App.XamlRoot;
            dialog.Title = $"详情";
            dialog.Content = content;
            dialog.PrimaryButtonText = "关闭";
            dialog.DefaultButton = ContentDialogButton.Primary;
            await dialog.ShowAsync();
        }

        private void AddLabelAndTextToContent(StackPanel content, string title, string? text)
        {
            if (text.IsBlank())
            {
                return;
            }
            var txt = new TextBlock()
            {
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
            };
            txt.Inlines.Add(new Run()
            {
                Text = title,
                FontWeight = FontWeights.Bold,
            });
            txt.Inlines.Add(new Run()
            {
                Text = text,
            });
            content.Children.Add(txt);
        }

        private void AddLabelAndTimeToContent(StackPanel content, string title, long? _long)
        {
            if (_long is null)
            {
                return;
            }
            var txt = new TextBlock()
            {
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
            };
            txt.Inlines.Add(new Run()
            {
                Text = title,
                FontWeight = FontWeights.Bold,
            });
            txt.Inlines.Add(new Run()
            {
                Text = _long.Value.TimeStampToDateTime()?.ToString("yyyy-MM-dd HH:mm:ss"),
            });
            content.Children.Add(txt);
        }

        private void AddLabelAndTimeLongToContent(StackPanel content, string title, long? _long)
        {
            if (_long is null)
            {
                return;
            }
            var txt = new TextBlock()
            {
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
            };
            txt.Inlines.Add(new Run()
            {
                Text = title,
                FontWeight = FontWeights.Bold,
            });
            txt.Inlines.Add(new Run()
            {
                Text = StringHelper.FormatTimeSpan(TimeSpan.FromSeconds((double)_long), 3),
            });
            content.Children.Add(txt);
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
                var id = PathItems.Last().Id;
                FileItems = new IncrementalLoadingCollection<MyFileIncrementalSource, MyFileItemModel>(new MyFileIncrementalSource(id, SortDirection, SortField), 100);
                ImageFileItems = new IncrementalLoadingCollection<MyFileImageIncrementalSource, MyFileItemModel>(new MyFileImageIncrementalSource(id, SortDirection, SortField), 100);
                MediaFileItems = new IncrementalLoadingCollection<MyFileMediaIncrementalSource, MyFileItemModel>(new MyFileMediaIncrementalSource(id, SortDirection, SortField), 100);
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
        /// 上传文件
        /// </summary>
        [RelayCommand]
        public async Task UploadFile()
        {
            var vm = App.Resolve<UploadListViewModel>();
            if (vm is null)
            {
                return;
            }
            FileOpenPicker picker = new();
            picker.FileTypeFilter.Add("*");
            InitializeWithWindow.Initialize(picker, App.WindowHandle);
            var files = await picker.PickMultipleFilesAsync();
            if (files is null || files.Count <= 0)
            {
                return;
            }
            await UploadFiles(files.Select(file => file.Path));
        }

        /// <summary>
        /// 选择本地文件夹并将其完整目录结构上传到当前目录。
        /// </summary>
        [RelayCommand]
        public async Task UploadFolder()
        {
            if (!User.IsLogin || PathItems.Count == 0)
            {
                return;
            }

            FolderPicker picker = new();
            picker.FileTypeFilter.Add("*");
            InitializeWithWindow.Initialize(picker, App.WindowHandle);
            var folder = await picker.PickSingleFolderAsync();
            if (folder is null)
            {
                return;
            }

            await UploadFolders([folder.Path]);
        }

        /// <summary>
        /// 将本地文件加入当前目录的上传队列。
        /// </summary>
        public async Task UploadFiles(IEnumerable<string> filePaths)
        {
            if (!User.IsLogin || PathItems.Count == 0)
            {
                return;
            }

            var vm = App.Resolve<UploadListViewModel>();
            if (vm is null)
            {
                return;
            }

            var added = false;
            foreach (var filePath in filePaths.Where(File.Exists))
            {
                await vm.AddTask(filePath, $"{PathItems.Last().Id}");
                added = true;
            }

            if (added)
            {
                await App.JumpPage(MenuKeys.UploadList);
            }
        }

        /// <summary>
        /// 将本地文件夹递归上传到当前目录，并保留原始目录结构。
        /// </summary>
        public async Task UploadFolders(IEnumerable<string> folderPaths)
        {
            if (!User.IsLogin || PathItems.Count == 0)
            {
                return;
            }

            var uploadViewModel = App.Resolve<UploadListViewModel>();
            var targetDirectoryId = PathItems.Count == 1 ? "0" : $"{PathItems.Last().Id}";
            var queuedFileCount = 0;
            var createdFolderCount = 0;

            IsBusy = true;
            try
            {
                foreach (var folderPath in folderPaths.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    var result = await UploadFolderTreeAsync(folderPath, targetDirectoryId, uploadViewModel);
                    queuedFileCount += result.QueuedFileCount;
                    createdFolderCount += result.CreatedFolderCount;
                }
            }
            catch (Exception ex)
            {
                await LogHelper.Error(ex);
                await App.ShowMessageBar(
                    $"文件夹上传任务添加失败：{ex.Message}",
                    "错误",
                    InfoBarSeverity.Error,
                    autoClose: TimeSpan.FromSeconds(8));
                return;
            }
            finally
            {
                IsBusy = false;
            }

            if (createdFolderCount == 0)
            {
                return;
            }

            if (queuedFileCount > 0)
            {
                await App.JumpPage(MenuKeys.UploadList);
            }
            else
            {
                await RefreshFiles();
                await App.ShowMessageBar("文件夹已创建。", "上传完成", autoClose: TimeSpan.FromSeconds(5));
            }
        }

        /// <summary>
        /// 根据拖放项类型分别上传文件和文件夹。
        /// </summary>
        public async Task UploadLocalItems(IEnumerable<string> filePaths, IEnumerable<string> folderPaths)
        {
            var files = filePaths.Where(File.Exists).ToList();
            var folders = folderPaths.Where(Directory.Exists).ToList();

            if (folders.Count > 0)
            {
                await UploadFolders(folders);
            }

            if (files.Count > 0)
            {
                await UploadFiles(files);
            }
        }

        private static async Task<(int QueuedFileCount, int CreatedFolderCount)> UploadFolderTreeAsync(
            string folderPath,
            string targetDirectoryId,
            UploadListViewModel uploadViewModel)
        {
            var root = new DirectoryInfo(folderPath);
            var rootDirectoryId = await CreateRemoteFolderAsync(root.Name, targetDirectoryId);
            var parentTaskId = await uploadViewModel.BeginFolderTaskAsync(root.Name, root.FullName, rootDirectoryId);
            var createdFolderCount = 1;
            var queuedFileCount = 0;
            long totalSize = 0;
            var pendingDirectories = new Queue<(DirectoryInfo LocalDirectory, string RemoteDirectoryId)>();
            pendingDirectories.Enqueue((root, rootDirectoryId));

            try
            {
                while (pendingDirectories.Count > 0)
                {
                    var current = pendingDirectories.Dequeue();
                    foreach (var directory in current.LocalDirectory.EnumerateDirectories())
                    {
                        var remoteDirectoryId = await CreateRemoteFolderAsync(directory.Name, current.RemoteDirectoryId);
                        createdFolderCount++;
                        pendingDirectories.Enqueue((directory, remoteDirectoryId));
                    }

                    foreach (var file in current.LocalDirectory.EnumerateFiles())
                    {
                        await uploadViewModel.AddTask(file.FullName, current.RemoteDirectoryId, parentTaskId);
                        queuedFileCount++;
                        totalSize += file.Length;
                    }
                }

                uploadViewModel.CompleteFolderCollection(parentTaskId, queuedFileCount, totalSize);
                return (queuedFileCount, createdFolderCount);
            }
            catch
            {
                uploadViewModel.MarkFolderCollectionFailed(parentTaskId);
                throw;
            }
        }

        private static async Task<string> CreateRemoteFolderAsync(string folderName, string parentDirectoryId)
        {
            var request = new RestRequest(ApiResource.OpenFolderAdd);
            request.AddOrUpdateParameter("pid", parentDirectoryId);
            request.AddOrUpdateParameter("file_name", folderName);
            request.AlwaysMultipartFormData = true;

            var response = await App.ProApiClient.PostAsync(request);
            if (!response.IsSuccessful || response.Content.IsBlank())
            {
                throw new InvalidOperationException($"无法创建远端文件夹“{folderName}”。");
            }

            var dto = JsonSerializer.Deserialize<ProResponseDTO<OpenFolderAddDTO>>(response.Content);
            if (dto is null || !dto.State || dto.Data?.FileId.IsBlank() != false)
            {
                throw new InvalidOperationException(dto?.Message ?? $"无法创建远端文件夹“{folderName}”。");
            }

            return dto.Data.FileId!;
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
                var parentDirectoryId = PathItems.Count == 1 ? "0" : $"{PathItems.Last().Id}";
                await CreateRemoteFolderAsync(vm.FileName, parentDirectoryId);
                await RefreshFiles();
            }
            catch (Exception ex)
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
            bool add = await AddDownloadTasksAsync(SelectedFileItems, System.DownloadDirPath);
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
            bool add;
            if (SelectedFileItems.IsNotBlank())
            {
                add = await AddDownloadTasksAsync(SelectedFileItems, System.DownloadDirPath);
            }
            else if (item is MyFileItemModel _item)
            {
                add = await AddDownloadTasksAsync([_item], System.DownloadDirPath);
            }
            else
            {
                add = false;
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
            bool add;
            if (SelectedFileItems.IsNotBlank())
            {
                add = await AddDownloadTasksAsync(SelectedFileItems, folder.Path);
            }
            else if (item is MyFileItemModel _item)
            {
                add = await AddDownloadTasksAsync([_item], folder.Path);
            }
            else
            {
                add = false;
            }
            if (add)
            {
                await App.JumpPage(MenuKeys.DownloadList);
            }
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
                        await _downloadListViewModel.AddTask(
                            item.PickCode!,
                            SanitizePathSegment(item.Name!),
                            item.Size,
                            targetDirectory);
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
            if (folder.Id.IsBlank() || folder.Name.IsBlank())
            {
                return false;
            }

            const int pageSize = 1150;
            var rootDirectory = Path.Combine(targetDirectory, SanitizePathSegment(folder.Name!));
            Directory.CreateDirectory(rootDirectory);
            var parentTaskId = await _downloadListViewModel.BeginFolderTaskAsync(folder.Name!, rootDirectory, folder.Id!);

            var pendingFolders = new Queue<(string Id, string Directory)>();
            pendingFolders.Enqueue((folder.Id!, rootDirectory));
            var added = false;
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
                            if (entry.FN.IsBlank())
                            {
                                continue;
                            }

                            var safeName = SanitizePathSegment(entry.FN!);
                            if (entry.FC == "0")
                            {
                                if (entry.FId.IsBlank()) continue;
                                var childDirectory = Path.Combine(current.Directory, safeName);
                                Directory.CreateDirectory(childDirectory);
                                pendingFolders.Enqueue((entry.FId!, childDirectory));
                            }
                            else if (entry.PC.IsNotBlank())
                            {
                                await _downloadListViewModel.AddTask(entry.PC!, safeName, entry.FS, current.Directory, parentTaskId: parentTaskId);
                                added = true;
                                totalFiles++;
                                totalSize += entry.FS ?? 0;
                            }
                        }

                        offset += entries.LongLength;
                    }
                    while (offset < totalCount && entries.Length > 0);
                }

                _downloadListViewModel.CompleteFolderCollection(parentTaskId, totalFiles, totalSize);
                return added;
            }
            catch
            {
                _downloadListViewModel.MarkFolderCollectionFailed(parentTaskId);
                throw;
            }
        }

        public async Task RestartFolderDownloadAsync(DownloadItemModel task)
        {
            if (task.TaskId is not > 0 || task.SourceFolderId.IsBlank() || task.SavePath.IsBlank())
            {
                return;
            }

            try
            {
                const int pageSize = 1150;
                var pendingFolders = new Queue<(string Id, string Directory)>();
                pendingFolders.Enqueue((task.SourceFolderId!, task.SavePath!));
                var totalFiles = 0;
                long totalSize = 0;
                task.State = DownloadTaskStateEnum.Queued;

                while (pendingFolders.Count > 0)
                {
                    var current = pendingFolders.Dequeue();
                    Directory.CreateDirectory(current.Directory);
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
                                pendingFolders.Enqueue((entry.FId!, Path.Combine(current.Directory, safeName)));
                            }
                            else if (entry.PC.IsNotBlank())
                            {
                                var exists = _downloadListViewModel.DownloadItems.Any(item => item.ParentTaskId == task.TaskId
                                    && string.Equals(item.PickCode, entry.PC, StringComparison.Ordinal));
                                if (!exists)
                                {
                                    await _downloadListViewModel.AddTask(entry.PC!, safeName, entry.FS, current.Directory, parentTaskId: task.TaskId);
                                }
                                totalFiles++;
                                totalSize += entry.FS ?? 0;
                            }
                        }
                        offset += entries.LongLength;
                    }
                    while (offset < totalCount && entries.Length > 0);
                }
                _downloadListViewModel.CompleteFolderCollection(task.TaskId.Value, totalFiles, totalSize);
            }
            catch (Exception ex)
            {
                _downloadListViewModel.MarkFolderCollectionFailed(task.TaskId.Value);
                await LogHelper.Error(ex);
            }
        }

        public async Task RestartFolderUploadAsync(UploadItemModel task)
        {
            if (task.TaskId is not > 0 || task.FilePath.IsBlank() || task.ParentId.IsBlank()) return;
            try
            {
                var root = new DirectoryInfo(task.FilePath!);
                var pending = new Queue<(DirectoryInfo Local, string Remote)>();
                pending.Enqueue((root, task.ParentId!));
                var totalFiles = 0;
                long totalSize = 0;
                task.State = UploadTaskStateEnum.Queued;
                while (pending.Count > 0)
                {
                    var current = pending.Dequeue();
                    foreach (var directory in current.Local.EnumerateDirectories())
                    {
                        pending.Enqueue((directory, await CreateRemoteFolderAsync(directory.Name, current.Remote)));
                    }
                    foreach (var file in current.Local.EnumerateFiles())
                    {
                        await App.Resolve<UploadListViewModel>().AddTask(file.FullName, current.Remote, task.TaskId);
                        totalFiles++;
                        totalSize += file.Length;
                    }
                }
                App.Resolve<UploadListViewModel>().CompleteFolderCollection(task.TaskId.Value, totalFiles, totalSize);
            }
            catch (Exception ex)
            {
                App.Resolve<UploadListViewModel>().MarkFolderCollectionFailed(task.TaskId.Value);
                await LogHelper.Error(ex);
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
            if (!User.IsLogin)
            {
                return;
            }
            if (SelectedFileItems.IsBlank())
            {
                return;
            }
            var title = new StackPanel
            {
                Orientation = Orientation.Horizontal,
            };
            title.Children.Add(new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Text = "选择复制到的目录",
                Margin = new Thickness(10, 0, 0, 0),
            });
            using var scope = App.CreateScope();
            var vm = scope.Resolve<SelectSavePathViewModel>();
            SelectSavePathContentDialog dialog = new SelectSavePathContentDialog(vm);
            dialog.XamlRoot = App.XamlRoot;
            dialog.Title = title;
            dialog.PrimaryButtonText = "确定";
            dialog.SecondaryButtonText = "取消";
            dialog.DefaultButton = ContentDialogButton.Primary;
            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                return;
            }
            if (vm.SavePathId is null || vm.SavePathId < 0)
            {
                return;
            }
            try
            {
                var cid = vm.SavePathId;
                var ids = string.Join(",", SelectedFileItems.Select(x => x.Id).ToList());
                var req = new RestRequest(ApiResource.OpenUfileCopy);
                req.AddOrUpdateParameter("file_id", ids);
                req.AddOrUpdateParameter("pid", $"{cid}");
                req.AlwaysMultipartFormData = true;
                var res = await App.ProApiClient.PostAsync(req);
                if (!res.IsSuccessful || res.Content.IsBlank())
                {
                    return;
                }
                var dto = JsonSerializer.Deserialize<ProResponseDTO>(res.Content);
                if (dto is null || !dto.State)
                {
                    if (dto?.Message.IsNotBlank() == true)
                    {
                        _ = App.ShowMessageBar(dto?.Message!, "错误", InfoBarSeverity.Error, autoClose: TimeSpan.FromSeconds(4));
                    }
                    return;
                }
                _ = App.ShowMessageBar("复制成功！", "成功", InfoBarSeverity.Success, autoClose: TimeSpan.FromSeconds(3));
                await RefreshFiles();
            }
            catch (Exception ex)
            {
                await LogHelper.Error(ex);
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
            if (SelectedFileItems.IsBlank())
            {
                return;
            }
            var title = new StackPanel
            {
                Orientation = Orientation.Horizontal,
            };
            title.Children.Add(new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Text = "选择移动到的目录",
                Margin = new Thickness(10, 0, 0, 0),
            });
            using var scope = App.CreateScope();
            var vm = scope.Resolve<SelectSavePathViewModel>();
            SelectSavePathContentDialog dialog = new SelectSavePathContentDialog(vm);
            dialog.XamlRoot = App.XamlRoot;
            dialog.Title = title;
            dialog.PrimaryButtonText = "确定";
            dialog.SecondaryButtonText = "取消";
            dialog.DefaultButton = ContentDialogButton.Primary;
            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                return;
            }
            if (vm.SavePathId is null || vm.SavePathId < 0)
            {
                return;
            }
            try
            {
                var cid = vm.SavePathId;
                var ids = string.Join(",", SelectedFileItems.Select(x => x.Id).ToList());
                var req = new RestRequest(ApiResource.OpenUfileMove);
                req.AddOrUpdateParameter("file_ids", ids);
                req.AddOrUpdateParameter("to_cid", $"{cid}");
                req.AlwaysMultipartFormData = true;
                var res = await App.ProApiClient.PostAsync(req);
                if (!res.IsSuccessful || res.Content.IsBlank())
                {
                    return;
                }
                var dto = JsonSerializer.Deserialize<ProResponseDTO>(res.Content);
                if (dto is null || !dto.State)
                {
                    if (dto?.Message.IsNotBlank() == true)
                    {
                        _ = App.ShowMessageBar(dto?.Message!, "错误", InfoBarSeverity.Error, autoClose: TimeSpan.FromSeconds(4));
                    }
                    return;
                }
                _ = App.ShowMessageBar("移动成功！", "成功", InfoBarSeverity.Success, autoClose: TimeSpan.FromSeconds(3));
                await RefreshFiles();
            }
            catch (Exception ex)
            {
                await LogHelper.Error(ex);
            }
        }

        /// <summary>
        /// 重命名
        /// </summary>
        [RelayCommand]
        public async Task UpdateFileName(object? item)
        {
            if (!User.IsLogin)
            {
                return;
            }
            if (item is not MyFileItemModel _item)
            {
                return;
            }

            var title = new StackPanel
            {
                Orientation = Orientation.Horizontal,
            };
            title.Children.Add(new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Text = "重命名",
                Margin = new Thickness(10, 0, 0, 0),
            });
            var content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
            };
            content.Children.Add(new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                Text = "名称：",
                Margin = new Thickness(10, 0, 0, 0),
            });
            var txt_name = new TextBox
            {
                VerticalAlignment = VerticalAlignment.Center,
                Text = _item.Name,
                Width = 400,
                Margin = new Thickness(5, 0, 0, 0),
            };
            content.Children.Add(txt_name);
            txt_name.SelectAll();
            using var scope = App.CreateScope();
            var dialog = new ContentDialog();
            dialog.XamlRoot = App.XamlRoot;
            dialog.Title = title;
            dialog.Content = content;
            dialog.PrimaryButtonText = "确定";
            dialog.SecondaryButtonText = "取消";
            dialog.DefaultButton = ContentDialogButton.Primary;
            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                return;
            }
            if (txt_name.Text.IsBlank())
            {
                _ = App.ShowMessageBar("文件(夹)名称不能为空！", "错误", InfoBarSeverity.Error, autoClose: TimeSpan.FromSeconds(4));
                return;
            }
            if (txt_name.Text.StringTrim().Equals(_item.Name.StringTrim()))
            {
                return;
            }
            try
            {
                var req = new RestRequest(ApiResource.OpenUfileUpdate);
                req.AddOrUpdateParameter("file_id", _item.Id);
                req.AddOrUpdateParameter("file_name", txt_name.Text.StringTrim());
                req.AlwaysMultipartFormData = true;
                var res = await App.ProApiClient.PostAsync(req);
                if (!res.IsSuccessful || res.Content.IsBlank())
                {
                    return;
                }
                var dto = JsonSerializer.Deserialize<ProResponseDTO>(res.Content);
                if (dto is null || !dto.State)
                {
                    return;
                }
                _ = App.ShowMessageBar("重命名成功！", "成功", InfoBarSeverity.Success, autoClose: TimeSpan.FromSeconds(3));
                await RefreshFiles();
            }
            catch (Exception ex)
            {
                await LogHelper.Error(ex);
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
                var dto = JsonSerializer.Deserialize<ProResponseDTO<string[]?>>(res.Content);
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
        /// 直接跳转进入指定目录，并选中文件
        /// </summary>
        [RelayCommand]
        public async Task JumpToFolder(object fileId)
        {
            if (!User.IsLogin)
            {
                return;
            }
            try
            {
                var req = new RestRequest(ApiResource.OpenFolderGetInfo);
                req.AddQueryParameter("file_id", $"{fileId}");
                var res = await App.ProApiClient.GetAsync(req);
                if (!res.IsSuccessful || res.Content.IsBlank())
                {
                    return;
                }
                var state = JsonSerializer.Deserialize<ProResponseDTO>(res.Content);
                if (state is null || !state.State)
                {
                    if (state?.Message.IsNotBlank() == true)
                    {
                        await App.ShowMessageBar(state.Message, "错误", InfoBarSeverity.Error, autoClose: TimeSpan.FromSeconds(5));
                    }
                    return;
                }
                OpenFolderGetInfoDTO? info = null;
                try
                {
                    var dto = JsonSerializer.Deserialize<ProResponseDTO<OpenFolderGetInfoDTO?>>(res.Content);
                    info = dto?.Data;
                }
                catch (Exception e)
                {
                    await LogHelper.Error(e);
                }
                if (info is null || info.Paths is null || info.Paths.Length == 0)
                {
                    await App.ShowMessageBar("详情获取失败！", "错误", InfoBarSeverity.Error, autoClose: TimeSpan.FromSeconds(5));
                    return;
                }
                PathItems.Clear();
                foreach (var p in info.Paths)
                {
                    PathItems.Add(new SelectOptionItem(p.FileId!.Value, p.FileName!));
                }
                await RefreshFiles();
                if (!FileItems.Any(x => x.Id == $"{fileId}"))
                {
                    await FileItems.LoadMoreItemsAsync(100);
                    for (; FileItems.HasMoreItems; await FileItems.LoadMoreItemsAsync(100))
                    {
                        if (FileItems.Any(x => x.Id == $"{fileId}"))
                        {
                            break;
                        }
                    }
                }
                var find = FileItems.Where(x => x.Id == $"{fileId}").FirstOrDefault();
                if (find is null)
                {
                    return;
                }
                var index = FileItems.IndexOf(find);
                _ = App.SelectedItemAndScrollIntoView(index, find);
            }
            catch
            {
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
