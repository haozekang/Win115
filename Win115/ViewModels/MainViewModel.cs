using Autofac;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteDB;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Tanovo.ExtensionMethods;
using Win115.Dtos;
using Win115.Entities;
using Win115.Enums;
using Win115.Helpers;
using Win115.Models;
using Win115.Properties;
using Win115.Views;
using static QRCoder.PayloadGenerator;

namespace Win115.ViewModels
{
    public partial class MainViewModel : ObservableRecipient
    {
        private LiteDatabase _db;
        private DownloadListViewModel _downloadListViewModel { get; set; }
        public Frame? _rootFrame { get; set; }
        public Type? _currentPageType { get; set; }

        [ObservableProperty]
        public partial UserInfoModel User { get; set; }

        [ObservableProperty]
        public partial string? Title { get; set; } = "115 Plus";

        [ObservableProperty]
        public partial ObservableCollection<NavigationViewItem> MenuItems { get; set; } = new ObservableCollection<NavigationViewItem>()
        {
            new NavigationViewItem { Icon = new FontIcon() { Glyph = "\uE977" }, Content = "我的文件", Tag = MenuKeys.MyFiles },
            new NavigationViewItem { Icon = new FontIcon() { Glyph = "\uE74D" }, Content = "回收站", Tag = MenuKeys.BackStation },
            new NavigationViewItem { Icon = new FontIcon() { Glyph = "\uEBD3" }, Content = "云下载", Tag = MenuKeys.CloudDownload },
            new NavigationViewItem { Icon = new FontIcon() { Glyph = "\uE896" }, Content = "下载列表", Tag = MenuKeys.DownloadList },
            new NavigationViewItem { Icon = new FontIcon() { Glyph = "\uE898" }, Content = "上传列表", Tag = MenuKeys.UploadList },
        };

        [ObservableProperty]
        public partial ObservableCollection<NavigationViewItem> FooterMenuItems { get; set; } = new ObservableCollection<NavigationViewItem>()
        {
            new NavigationViewItem { Icon = new FontIcon() { Glyph = "\uE946" }, Content = "关于", Tag = MenuKeys.About },
            new NavigationViewItem { Icon = new FontIcon() { Glyph = "\uE705" }, Content = "隐私策略", Tag = MenuKeys.PrivacyPolicy },
            new NavigationViewItem { Icon = new FontIcon() { Glyph = "\uE713" }, Content = "设置", Tag = MenuKeys.Settings },
        };

        [ObservableProperty]
        public partial object? SelectedItem { get; set; } = null;

        public MainViewModel(UserInfoModel user, LiteDatabase db, DownloadListViewModel downloadListViewModel)
        {
            User = user;
            _db = db;
            _downloadListViewModel = downloadListViewModel;
            _ = AutoLoginAsync();
        }

        private async Task AutoLoginAsync()
        {
            if (App.CodeVerifier.IsBlank())
            {
                App.CodeVerifier = string.Join("", "QWERTYUIOPASDFGHJKLZXCVBNMqwertyuiopasdfghjklzxcvbnm0123456789".ToCharArray().GetRandomItems(64)!);
            }
            var col = _db.GetCollection<TokenEntity>(CollectionResource.Tokens);
            var find = col.Query().Where(x => x.RefreshExpiresIn > DateTime.Now).OrderByDescending(x => x.Id).FirstOrDefault();
            if (find is null)
            {
                return;
            }
            var um = App.Resolve<UserInfoModel>();
            var token = find.AccessToken;
            var tokenExpiresIn = find.AccessExpiresIn;
            var refreshToken = find.RefreshToken;
            var refreshExpiresIn = find.RefreshExpiresIn;
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(refreshToken))
            {
                return;
            }
            um.RefreshToken = refreshToken;
            um.AccessToken = token;
            um.ExpiresIn = find.AccessExpiresIn;
            try
            {
                var reqUserInfo = new RestRequest(ApiResource.OpenUserInfo);
                var resUserInfo = await App.ProApiClient.GetAsync(reqUserInfo);
                if (resUserInfo is null || resUserInfo.Content is null)
                {
                    return;
                }
                await LogHelper.Trace(resUserInfo.Content);
                var state = JsonConvert.DeserializeObject<ProResponseDTO<object?>>(resUserInfo.Content!);
                if (state is null)
                {
                    return;
                }
                if (state.State != true)
                {
                    if (state.Code == 40140123 
                        || state.Code == 40140124
                        || state.Code == 40140125
                        || state.Code == 40140126)
                    {
                        User.ExpiresIn = DateTime.MinValue;
                        resUserInfo = await App.ProApiClient.GetAsync(reqUserInfo);
                    }
                    else
                    {
                        await App.ShowMessageBar($"{state.Message}", "错误", severity: InfoBarSeverity.Error, autoClose: TimeSpan.FromSeconds(5));
                        return;
                    }
                }
                var userInfo = JsonConvert.DeserializeObject<ProResponseDTO<OpenUserInfoDTO>>(resUserInfo.Content!);
                if (userInfo is null || userInfo.Data is null)
                {
                    return;
                }
                um.IsLogin = true;
                um.UserId = userInfo.Data.UserId!;
                um.UserName = userInfo.Data.UserName!;
                um.FaceS = userInfo.Data.UserFaceS!;
                um.FaceM = userInfo.Data.UserFaceM!;
                um.FaceL = userInfo.Data.UserFaceL!;
                um.AllTotalSize = userInfo.Data.RtSpaceInfo?.AllTotal?.Size ?? 0;
                um.AllTotalFormat = userInfo.Data.RtSpaceInfo?.AllTotal?.SizeFormat ?? string.Empty;
                um.AllRemainSize = userInfo.Data.RtSpaceInfo?.AllRemain?.Size ?? 0;
                um.AllRemainFormat = userInfo.Data.RtSpaceInfo?.AllRemain?.SizeFormat ?? string.Empty;
                um.AllUseSize = userInfo.Data.RtSpaceInfo?.AllUse?.Size ?? 0;
                um.AllUseFormat = userInfo.Data.RtSpaceInfo?.AllUse?.SizeFormat ?? string.Empty;
                um.VipLevelName = userInfo.Data.VipInfo?.LevelName ?? string.Empty;
                um.VipExpire = userInfo.Data.VipInfo?.Expire > 0 ? DateTimeOffset.FromUnixTimeSeconds(userInfo.Data.VipInfo.Expire.Value).AddHours(8).DateTime : null;

                var vm = App.Resolve<MyFilesViewModel>();
                Task.WaitAll(App.ShowMessageBar($"用户【{um.UserName}】登录成功，欢迎您使用！", "信息", severity: InfoBarSeverity.Success, autoClose: TimeSpan.FromSeconds(3)),
                    App.SetFace(um.FaceS),
                    vm.RefreshFilesCommand.ExecuteAsync(null), 
                    LoadHistory());
            }
            catch (Exception ex)
            {
                await LogHelper.Error(ex);
            }
        }

        [RelayCommand]
        public async Task LoadHistory()
        {
            if (User.IsLogin != true)
            {
                return;
            }
            var _db = App.Resolve<LiteDatabase>();
            if (_db.CollectionExists(CollectionResource.System) != true)
            {
                return;
            }
            // 载入历史下载记录
            var downloadTaskCol = _db.GetCollection<DownloadTaskEntity>(CollectionResource.DownloadTask);
            var downs = downloadTaskCol.Find(x => x.UserId == User.UserId);
            var downloadListViewModel = App.Resolve<DownloadListViewModel>();
            foreach (var down in downs)
            {
                downloadListViewModel.DownloadItems.Add(new DownloadItemModel
                {
                    TaskId = down.Id,
                    Name = down.Name,
                    Progress = down.Progress,
                    Size = down.Size,
                    State = down.State switch
                    {
                        DownloadTaskStateEnum.Failed => DownloadTaskStateEnum.Failed,
                        DownloadTaskStateEnum.Completed => DownloadTaskStateEnum.Completed,
                        DownloadTaskStateEnum.Canceled => DownloadTaskStateEnum.Canceled,
                        _ => DownloadTaskStateEnum.Paused
                    },
                    SavePath = down.SavePath,
                    PickCode = down.PickCode,
                    Url = down.Url
                });
            }

            // 载入历史上传记录
            var uploadTaskCol = _db.GetCollection<UploadTaskEntity>(CollectionResource.UploadTask);
            var ups = uploadTaskCol.Find(x => x.UserId == User.UserId);
            var uploadListViewModel = App.Resolve<UploadListViewModel>();
            foreach (var up in ups)
            {
                uploadListViewModel.UploadItems.Add(new UploadItemModel
                {
                    TaskId = up.Id,
                    Name = up.Name,
                    FileId = up.FileId,
                    ParentId = up.ParentId,
                    Progress = up.Progress,
                    Size = up.Size,
                    State = up.State switch
                    {
                        UploadTaskStateEnum.Failed => UploadTaskStateEnum.Failed,
                        UploadTaskStateEnum.Completed => UploadTaskStateEnum.Completed,
                        UploadTaskStateEnum.Canceled => UploadTaskStateEnum.Canceled,
                        _ => UploadTaskStateEnum.Paused
                    },
                    FilePath = up.FilePath,
                    PickCode = up.PickCode,
                    Bucket = up.Bucket,
                    Object = up.Object,
                    Endpoint = up.Endpoint,
                    Region = up.Region,
                });
            }
        }

        [RelayCommand]
        public async Task OfflineDownload()
        {
            if (User.IsLogin != true)
            {
                await App.ShowMessageBar($"请登录后再使用！", "警告", severity: InfoBarSeverity.Warning, autoClose: TimeSpan.FromSeconds(3));
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
                Text = "添加离线下载",
                Margin = new Thickness(10, 0, 0, 0),
            });
            using var scope = App.CreateScope();
            var vm = scope.Resolve<NewCloudDownloadViewModel>();
            NewCloudDownloadContentDialog dialog = new NewCloudDownloadContentDialog(vm);
            dialog.XamlRoot = App.XamlRoot;
            dialog.Title = title;
            dialog.PrimaryButtonText = "保存";
            dialog.SecondaryButtonText = "取消";
            dialog.DefaultButton = ContentDialogButton.Primary;
            dialog.PrimaryButtonCommand = OfflineAddTaskCommand;
            dialog.PrimaryButtonCommandParameter = vm;
            await dialog.ShowAsync();
        }

        [RelayCommand]
        public async Task OfflineAddTask(NewCloudDownloadViewModel vm)
        {
            if (vm is null)
            {
                return;
            }
            var req = new RestRequest(ApiResource.OpenOfflineAddTaskUrls);
            req.AddParameter("urls", vm.Urls);
            req.AddParameter("wp_path_id", $"{vm.SavePathId}");
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
            if (!dto.State || dto.Data is null)
            {
                return;
            }
            await App.JumpPage(MenuKeys.CloudDownload);
            var viewModel = App.Resolve<CloudDownloadViewModel>();
            await viewModel.RefreshTasksCommand.ExecuteAsync(null);
        }

        [RelayCommand]
        public async Task Login()
        {
            if (User.IsLogin)
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
                Text = "登录",
                Margin = new Thickness(10, 0, 0, 0),
            });
            using var scope = App.CreateScope();
            var vm = scope.Resolve<LoginViewModel>();
            ContentDialog dialog = new ContentDialog();
            dialog.XamlRoot = App.XamlRoot;
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.Title = title;
            dialog.PrimaryButtonText = "取消";
            dialog.DefaultButton = ContentDialogButton.Primary;
            dialog.Content = new LoginPage(vm)
            {
                Tag = dialog
            };
            await dialog.ShowAsync();
            GC.Collect();
        }

        [RelayCommand]
        public void UserInfo()
        {
            NavigateToPage(typeof(UserPage));
        }

        [RelayCommand]
        public async Task SignOut()
        {
            // 清理登录记录
            try
            {
                var col = _db.GetCollection<TokenEntity>(CollectionResource.Tokens);
                col.DeleteAll();
            }
            catch (Exception ex)
            {
                await LogHelper.Error(ex);
            }

            // 清理视图
            try
            {
                var filesView = App.Resolve<MyFilesViewModel>();
                var backView = App.Resolve<BackStationViewModel>();
                var cloudView = App.Resolve<CloudDownloadViewModel>();
                var downView = App.Resolve<DownloadListViewModel>();
                var upView = App.Resolve<UploadListViewModel>();
                var searchView = App.Resolve<SearchFilesViewModel>();
                var userView = App.Resolve<UserViewModel>();
                await Task.WhenAll(filesView.ClearDataCommand.ExecuteAsync(null),
                    backView.ClearDataCommand.ExecuteAsync(null),
                    cloudView.ClearDataCommand.ExecuteAsync(null),
                    downView.ClearDataCommand.ExecuteAsync(null),
                    upView.ClearDataCommand.ExecuteAsync(null),
                    searchView.ClearDataCommand.ExecuteAsync(null),
                    userView.ClearDataCommand.ExecuteAsync(null));
            }
            catch(Exception ex)
            {
                await LogHelper.Error(ex);
            }

        }

        public void NavigateToPage(Type? type)
        {
            if (_rootFrame is null || type is null)
            {
                return;
            }
            if (_currentPageType == type)
            {
                return;
            }
            // 用户\搜索结果 没有按钮
            if (type == typeof(UserPage) || type == typeof(SearchFilesPage))
            {
                SelectedItem = null;
            }
            DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
            {
                _rootFrame.Navigate(type);
                _currentPageType = type;
            });
        }

        public void NavigateToBlank()
        {
            if (_rootFrame is null)
            {
                return;
            }
            if (_currentPageType is null)
            {
                return;
            }
            DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
            {
                _rootFrame.Content = null;
                _currentPageType = null;
            });
        }
    }
}
