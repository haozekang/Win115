using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Win115.Helpers;
using Win115.Models;
using Win115.Services;
using Win115.ViewModels;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Win115.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AboutPage : Page
    {
        private AboutViewModel? viewModel;
        private UserInfoModel? _user;

        public AboutPage()
        {
            InitializeComponent();

            //md.OnLinkClicked += MarkdownTextBlock_OnLinkClicked;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            viewModel = App.Resolve<AboutViewModel>();
            _user = App.Resolve<UserInfoModel>();
            CurrentVersionText.Text = $"当前版本：{App.Resolve<UpdateService>().CurrentVersion}";
        }

        private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            CheckUpdateButton.IsEnabled = false;
            try
            {
                var updateService = App.Resolve<UpdateService>();
                if (!await updateService.CheckForUpdatesAsync())
                {
                    await App.ShowMessageBar("暂时无法获取最新版本信息，请稍后重试。", "检查更新失败",
                        InfoBarSeverity.Warning, autoClose: TimeSpan.FromSeconds(4));
                    return;
                }

                if (updateService.IsUpdateAvailable)
                {
                    await App.ShowMessageBar(
                        $"发现新版本 {updateService.LatestVersion}，可以前往 Microsoft Store 更新。",
                        "发现新版本", InfoBarSeverity.Success, autoClose: TimeSpan.FromSeconds(5));
                }
                else
                {
                    await App.ShowMessageBar("当前已是最新版本。", "检查更新",
                        InfoBarSeverity.Success, autoClose: TimeSpan.FromSeconds(4));
                }
            }
            finally
            {
                CheckUpdateButton.IsEnabled = true;
            }
        }

        private async void OpenMicrosoftStore_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var launched = await Windows.System.Launcher.LaunchUriAsync(
                    new Uri("ms-windows-store://pdp/?ProductId=9PKKZZP19P42"));
                if (!launched)
                {
                    await Windows.System.Launcher.LaunchUriAsync(new Uri(
                        "https://apps.microsoft.com/detail/9PKKZZP19P42?hl=zh-cn&gl=CN&ocid=pdpshare"));
                }
            }
            catch (Exception ex)
            {
                await LogHelper.Error(ex);
                await App.ShowMessageBar("无法打开 Microsoft Store 页面。", "打开失败",
                    InfoBarSeverity.Error, autoClose: TimeSpan.FromSeconds(4));
            }
        }

        private async void MarkdownTextBlock_OnLinkClicked(object? sender, LinkClickedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = $"{e.Uri}",
                    UseShellExecute = true
                });
            }
            catch(Exception ex)
            {
                await LogHelper.Error(ex);
            }
        }
    }
}
