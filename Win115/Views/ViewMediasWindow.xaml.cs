using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RestSharp;
using System;
using System.Linq;
using System.Threading.Tasks;
using Tanovo.ExtensionMethods;
using Win115.Dtos;
using Win115.Helpers;
using Win115.Models;
using Win115.Properties;
using Win115.ViewModels;
using Windows.Media.Core;
using WinRT.Interop;
using JsonSerializer = System.Text.Json.JsonSerializer;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Win115.Views
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ViewMediasWindow : Window
    {
        private ViewMediasViewModel _viewModel;
        private UserInfoModel? _user;

        public ViewMediasWindow(ViewMediasViewModel viewModel)
        {
            _user = App.Resolve<UserInfoModel>();
            _viewModel = viewModel;
            ExtendsContentIntoTitleBar = true;
            InitializeComponent();

            this.SetTitleBar(titleBar);

            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.SetBorderAndTitleBar(true, false);
            }
            appWindow.SetIcon("Assets/favicon.ico");
        }

        private async void lv_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _viewModel.IsMediaLoading = true;
            var item = lv.SelectedItem as MyFileItemModel;
            _viewModel.SelectedMediaItem = item;

            if (item is null || item.PickCode.IsBlank() || item.VideoUrl.IsBlank())
            {
                App.DispatcherQueue?.TryEnqueue(async () =>
                {
                    await ShowMessageBar("数据异常！", "错误", InfoBarSeverity.Error);
                });
                _viewModel.IsMediaLoading = false;
                return;
            }

            string? videoUrl = null;
            long playTimeSeconds = 0;
            // 获取视频播放进度
            var reqHistory = new RestRequest(ApiResource.OpenVideoHistory);
            reqHistory.AddQueryParameter("pick_code", item.PickCode);
            var resHistory = await App.ProApiClient.GetAsync(reqHistory);
            if (!resHistory.IsSuccessful || resHistory.Content.IsBlank())
            {
                return;
            }
            try
            {
                var dtoHistory = JsonSerializer.Deserialize<ProResponseDTO<OpenVideoHistoryDTO[]?>>(resHistory.Content);
                if (dtoHistory is null)
                {
                    App.DispatcherQueue?.TryEnqueue(async () =>
                    {
                        await ShowMessageBar("序列化失败！", "错误", InfoBarSeverity.Error);
                    });
                    return;
                }
                if (dtoHistory.State != true)
                {
                    App.DispatcherQueue?.TryEnqueue(async () =>
                    {
                        await ShowMessageBar(dtoHistory.Message ?? "未知错误", "错误", InfoBarSeverity.Error, autoClose: TimeSpan.FromSeconds(5));
                    });
                    return;
                }
                playTimeSeconds = dtoHistory.Data?.OrderByDescending(h => h.AddTime)?.Select(h => h.Time).FirstOrDefault()?.ToLong() ?? 0;
            }
            catch
            {
                try
                {
                    var dtoHistory = JsonSerializer.Deserialize<ProResponseDTO<OpenVideoHistoryDTO?>>(resHistory.Content);
                    if (dtoHistory is null)
                    {
                        App.DispatcherQueue?.TryEnqueue(async () =>
                        {
                            await ShowMessageBar("序列化失败！", "错误", InfoBarSeverity.Error);
                        });
                        return;
                    }
                    if (dtoHistory.State != true)
                    {
                        App.DispatcherQueue?.TryEnqueue(async () =>
                        {
                            await ShowMessageBar(dtoHistory.Message ?? "未知错误", "错误", InfoBarSeverity.Error, autoClose: TimeSpan.FromSeconds(5));
                        });
                        return;
                    }
                    playTimeSeconds = dtoHistory.Data?.Time.ToLong() ?? 0;
                }
                catch
                {
                    playTimeSeconds = 0;
                }
            }

            // 获取播放地址
            RestResponse? resPlay = null;
            bool code409ErrorShow = false;
            var reqPlay = new RestRequest(ApiResource.OpenVideoPlay);
            reqPlay.AddQueryParameter("pick_code", item.PickCode);
        lab: resPlay = await App.ProApiClient.GetAsync(reqPlay);
            if (!resPlay.IsSuccessful || resPlay.Content.IsBlank())
            {
                return;
            }
            ProResponseDTO<OpenVideoPlayDTO>? dtoPlay = null;
            try
            {
                dtoPlay = JsonSerializer.Deserialize<ProResponseDTO<OpenVideoPlayDTO>>(resPlay.Content);
            }
            catch (Exception ex)
            {
                await LogHelper.Error(ex); 
                await LogHelper.Error(resPlay.Content);
            }
            if (dtoPlay is null)
            {
                App.DispatcherQueue?.TryEnqueue(async () =>
                {
                    await ShowMessageBar("序列化失败！", "错误", InfoBarSeverity.Error);
                });
                return;
            }
            if (dtoPlay.State != true)
            {
                if (!code409ErrorShow)
                {
                    App.DispatcherQueue?.TryEnqueue(async () =>
                    {
                        await ShowMessageBar("服务端正在转码，请稍等，视频会自动刷新！", "错误", InfoBarSeverity.Error, autoClose: TimeSpan.FromSeconds(5));
                    });
                }
                if (dtoPlay.Code == 409)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    goto lab;
                }
                return;
            }
            if (dtoPlay.Data is null || dtoPlay.Data.VideoUrl is null || dtoPlay.Data.VideoUrl.Count == 0)
            {
                App.DispatcherQueue?.TryEnqueue(async () =>
                {
                    await ShowMessageBar("序列化数据中VideoUrl找不到", "错误", InfoBarSeverity.Error, autoClose: TimeSpan.FromSeconds(5));
                });
                _viewModel.IsMediaLoading = false;
                return;
            }
            int userDef = dtoPlay.Data.UserDef ?? 100;
            int userRotate = dtoPlay.Data.UserRotate ?? 0;
            int userTurn = dtoPlay.Data.UserTurn ?? 0;
            var video = dtoPlay.Data.VideoUrl.FirstOrDefault(v => v.DefinitionN == userDef) ?? dtoPlay.Data.VideoUrl.First();
            videoUrl = video.Url;
            if (videoUrl.IsBlank())
            {
                App.DispatcherQueue?.TryEnqueue(async () =>
                {
                    await ShowMessageBar("VideoUrl为空", "错误", InfoBarSeverity.Error, autoClose: TimeSpan.FromSeconds(5));
                });
                _viewModel.IsMediaLoading = false;
                return;
            }
            sc.Source = MediaSource.CreateFromUri(new Uri(videoUrl));
            _viewModel.IsMediaLoading = false;
            _viewModel.UpdateUI(true);
        }

        internal void SetSelectedItem(MyFileItemModel? selectedItem)
        {
            if (selectedItem is null)
            {
                _viewModel.IsMediaLoading = false;
                return;
            }
            int index = _viewModel.MediaFileItems.IndexOf(selectedItem);
            if (index < 0)
            {
                return;
            }
            lv.SelectedIndex = index;
            lv.ScrollIntoView(selectedItem);
        }

        private void btn_max_Click(object sender, RoutedEventArgs e)
        {
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                if (presenter.State != OverlappedPresenterState.Maximized)
                {
                    presenter.Maximize();
                }
                else
                {
                    presenter.Restore();
                }
            }
        }

        private void btn_close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btn_list_Click(object sender, RoutedEventArgs e)
        {
            sv.IsPaneOpen = !sv.IsPaneOpen;
        }

        private void btn_play_Click(object sender, RoutedEventArgs e)
        {
            sc.MediaPlayer.Play();
            _viewModel.UpdateUI(true);
        }

        private void btn_pause_Click(object sender, RoutedEventArgs e)
        {
            sc.MediaPlayer.Pause();
            _viewModel.UpdateUI(false);
        }

        public async Task ShowMessageBar(string msg, string title, InfoBarSeverity severity = InfoBarSeverity.Informational, bool showClose = true, TimeSpan? autoClose = null)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                msgBar.Message = msg;
                msgBar.Title = title;
                msgBar.Severity = severity;
                msgBar.IsClosable = showClose;
                msgBar.IsOpen = true;
                msgBar.Tag = null;
                if (autoClose is not null)
                {
                    msgBar.Tag = Guid.NewGuid();
                    DispatcherTimer timer = new DispatcherTimer();
                    timer.Interval = autoClose.Value;
                    timer.Tick += messageBar_Tick;
                    timer.Start();
                }
            });
        }

        private void messageBar_Tick(object? sender, object e)
        {
            if (sender is not DispatcherTimer timer || msgBar.Tag == null)
            {
                return;
            }
            msgBar.IsOpen = false;
            timer.Stop();
            GC.Collect();
        }
    }
}
