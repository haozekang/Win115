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
            //new NavigationViewItem { Icon = new FontIcon() { Glyph = "\uE74D" }, Content = "回收站", Tag = MenuKeys.BackStation },
            //new NavigationViewItem { Icon = new FontIcon() { Glyph = "\uEBD3" }, Content = "云下载", Tag = MenuKeys.CloudDownload },
            new NavigationViewItem { Icon = new FontIcon() { Glyph = "\uE896" }, Content = "下载列表", Tag = MenuKeys.DownloadList },
            //new NavigationViewItem { Icon = new FontIcon() { Glyph = "\uE898" }, Content = "上传列表", Tag = MenuKeys.UploadList },
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

                _ = App.ShowMessageBar($"用户【{um.UserName}】登录成功，欢迎您使用！", "信息", severity: InfoBarSeverity.Success, autoClose: TimeSpan.FromSeconds(3));
                _ = App.SetFace(um.FaceS);
                var vm = App.Resolve<MyFilesViewModel>();
                _ = vm.RefreshFilesCommand.ExecuteAsync(null);
            }
            catch (Exception ex)
            {
                await LogHelper.Error(ex);
            }
        }

        [RelayCommand]
        public async Task OfflineDownload()
        {
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
        public void SignOut()
        {
            User.IsLogin = false;
            User.UserId = string.Empty;
            User.UserName = string.Empty;
            User.FaceS = string.Empty;
            User.FaceM = string.Empty;
            User.FaceL = string.Empty;
            User.AllTotalSize = 0;
            User.AllTotalFormat = string.Empty;
            User.AllRemainSize = 0;
            User.AllRemainFormat = string.Empty;
            User.AllUseSize = 0;
            User.AllUseFormat = string.Empty;
            User.VipLevelName = string.Empty;
            User.VipExpire = null;
            // 清理登录记录
            var col = _db.GetCollection<TokenEntity>(CollectionResource.Tokens);
            col.DeleteAll();
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
