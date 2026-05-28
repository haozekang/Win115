using Autofac;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Collections;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
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
using static Aliyun.OSS.Model.LiveChannelStat;
using static System.Net.Mime.MediaTypeNames;

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
                new SelectOptionItem(-1, "文件")
            };
            SelectedFileItems.Clear();
            FileItems.Clear();
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
            var state = JsonConvert.DeserializeObject<ProResponseDTO>(res.Content);
            if (state is null || !state.State)
            {
                return;
            }
            OpenFolderGetInfoDTO? info = null;
            try
            {
                var dto = JsonConvert.DeserializeObject<ProResponseDTO<OpenFolderGetInfoDTO?>>(res.Content);
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
            foreach (var f in files)
            {
                await vm.AddTask(f.Path, $"{PathItems.Last().Id}");
            }
            await App.JumpPage(MenuKeys.UploadList);
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
                else
                {
                    req.AddOrUpdateParameter("pid", PathItems.Last().Id);
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
            else if (item is MyFileItemModel _item)
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
            else if (item is MyFileItemModel _item)
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
                var dto = JsonConvert.DeserializeObject<ProResponseDTO>(res.Content);
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
                var dto = JsonConvert.DeserializeObject<ProResponseDTO>(res.Content);
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
                var dto = JsonConvert.DeserializeObject<ProResponseDTO>(res.Content);
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
