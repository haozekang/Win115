using CommunityToolkit.WinUI;
using LiteDB;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Tanovo.ExtensionMethods;
using Win115.Entities;
using Win115.Enums;
using Win115.Models;
using Win115.Services;
using Win115.Properties;
using Win115.ViewModels;
using Win115.Views;
using Windows.System;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Win115
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private const int RestoreWindowCommand = 9;

        private readonly AppWindow _appWindow;
        private readonly SystemTrayService _systemTray;
        private readonly MainViewModel viewModel;
        private bool _isExitConfirmationOpen;
        private bool _isExitAllowed;
        private bool _isExitRequested;

        public MainWindow()
        {
            App.DispatcherQueue = this.DispatcherQueue;
            ExtendsContentIntoTitleBar = true;
            InitializeComponent();
            viewModel = App.Resolve<MainViewModel>();
            viewModel._rootFrame = RootFrame;

            this.SetTitleBar(titleBar);

            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            _appWindow = AppWindow.GetFromWindowId(windowId);

            if (_appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.SetBorderAndTitleBar(true, false);
            }
            viewModel.SelectedItem = viewModel.MenuItems?.FirstOrDefault();
            _appWindow.SetIcon("Assets/favicon.ico");
            _appWindow.Closing += AppWindow_Closing;
            _systemTray = new SystemTrayService(hwnd);
            _systemTray.RestoreRequested += ShowAndActivate;
            _systemTray.ExitRequested += RequestExit;

            _ = LoadSystemConfigAsync();
        }

        public void ShowAndActivate()
        {
            var windowHandle = WindowNative.GetWindowHandle(this);
            _appWindow.Show();
            ShowWindow(windowHandle, RestoreWindowCommand);
            Activate();
            SetForegroundWindow(windowHandle);
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(nint windowHandle, int command);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(nint windowHandle);

        private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            var system = App.Resolve<SystemInfoModel>();
            if (!_isExitRequested && system.CloseToTray)
            {
                args.Cancel = true;
                HideToTray();
                return;
            }

            if (_isExitAllowed || !HasActiveTransferTasks())
            {
                _systemTray.Dispose();
                return;
            }

            args.Cancel = true;
            if (_isExitConfirmationOpen)
            {
                return;
            }

            _isExitConfirmationOpen = true;
            _ = ConfirmExitAsync();
        }

        private bool HasActiveTransferTasks()
        {
            var downloadViewModel = App.Resolve<DownloadListViewModel>();
            var uploadViewModel = App.Resolve<UploadListViewModel>();

            return downloadViewModel.DownloadItems.Any(item =>
                       item.State is DownloadTaskStateEnum.Downloading or DownloadTaskStateEnum.Queued)
                || uploadViewModel.UploadItems.Any(item =>
                       item.State is UploadTaskStateEnum.Uploading
                           or UploadTaskStateEnum.CalcHash
                           or UploadTaskStateEnum.Queued);
        }

        private async Task ConfirmExitAsync()
        {
            try
            {
                var dialog = new ContentDialog
                {
                    Title = "提示",
                    Content = "当前有正在进行的传输任务，退出前将统一暂停这些任务。确定退出？",
                    PrimaryButtonText = "退出",
                    CloseButtonText = "取消",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = Content.XamlRoot
                };

                if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                {
                    return;
                }

                var downloadViewModel = App.Resolve<DownloadListViewModel>();
                var uploadViewModel = App.Resolve<UploadListViewModel>();
                await Task.WhenAll(downloadViewModel.PauseAll(), uploadViewModel.PauseAll());

                _isExitAllowed = true;
                Close();
            }
            catch (Exception ex)
            {
                await ShowMessageBar(
                    $"暂停传输任务失败，应用未退出：{ex.Message}",
                    "退出失败",
                    InfoBarSeverity.Error,
                    autoClose: TimeSpan.FromSeconds(5));
            }
            finally
            {
                _isExitConfirmationOpen = false;
            }
        }

        private async Task LoadSystemConfigAsync()
        {
            var _system = App.Resolve<SystemInfoModel>();
            var _db = App.Resolve<LiteDatabase>();
            if (_db.CollectionExists(CollectionResource.System) != true)
            {
                return;
            }
            var col = _db.GetCollection<SystemEntity>(CollectionResource.System);
            var apiRateLimit = ReadIntSetting(col, ApiSettings.RateLimitKey, ApiSettings.DefaultRateLimit);
            var downloadDirPath = col.Query().Where(x => x.Type == SystemConfigTypeResource.DownloadDirPath).SingleOrDefault();
            if (downloadDirPath is not null)
            {
                await DispatcherQueue.EnqueueAsync(() => 
                {
                    _system.DownloadDirPath = downloadDirPath.Value;
                });
            }

            var concurrentTasks = ReadIntSetting(col, DownloadSettings.ConcurrentTasksKey, DownloadSettings.DefaultConcurrentTasks);
            var segmentCount = ReadIntSetting(col, DownloadSettings.SegmentCountKey, DownloadSettings.DefaultSegmentCount);
            var speedLimit = ReadIntSetting(col, DownloadSettings.SpeedLimitKey, 0);
            var uploadMaxRetry = ReadIntSetting(col, UploadSettings.MaxRetryKey, UploadSettings.DefaultMaxRetry);
            var uploadConcurrentTasks = ReadIntSetting(
                col,
                UploadSettings.MaxConcurrentTasksKey,
                UploadSettings.DefaultMaxConcurrentTasks);
            var closeToTray = ReadBoolSetting(
                col,
                WindowSettings.CloseToTrayKey,
                WindowSettings.DefaultCloseToTray);
            await DispatcherQueue.EnqueueAsync(() =>
            {
                _system.ApiRateLimit = Math.Clamp(apiRateLimit, 0, ApiSettings.MaxRateLimit);
                _system.DownloadConcurrentTasks = Math.Clamp(concurrentTasks, 1, DownloadSettings.MaxConcurrentTasks);
                _system.DownloadSegmentCount = Math.Clamp(segmentCount, 1, DownloadSettings.MaxSegmentCount);
                _system.DownloadSpeedLimitKbps = Math.Max(0, speedLimit);
                _system.UploadMaxRetry = Math.Clamp(uploadMaxRetry, 0, UploadSettings.MaxRetry);
                _system.UploadConcurrentTasks = Math.Clamp(
                    uploadConcurrentTasks,
                    1,
                    UploadSettings.MaxConcurrentTasks);
                _system.CloseToTray = closeToTray;
            });
        }

        private static int ReadIntSetting(ILiteCollection<SystemEntity> collection, string key, int defaultValue)
        {
            var setting = collection.Query().Where(item => item.Type == key).SingleOrDefault();
            return int.TryParse(setting?.Value, out var value) ? value : defaultValue;
        }

        private static bool ReadBoolSetting(ILiteCollection<SystemEntity> collection, string key, bool defaultValue)
        {
            var setting = collection.Query().Where(item => item.Type == key).SingleOrDefault();
            return bool.TryParse(setting?.Value, out var value) ? value : defaultValue;
        }

        private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
        {
            RootNavigationView.IsPaneOpen = !RootNavigationView.IsPaneOpen;
        }

        private void RootNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            var item = args.SelectedItem;
            if (item is not NavigationViewItem menu)
            {
                return;
            }
            Type? type = null;
            if (args.IsSettingsSelected)
            {
                type = typeof(SettingsPage);
            }
            else
            {
                type = menu.Tag switch
                {
                    MenuKeys.MyFiles => typeof(MyFilesPage),
                    MenuKeys.About => typeof(AboutPage),
                    MenuKeys.PrivacyPolicy => typeof(PrivacyPolicyPage),
                    MenuKeys.BackStation => typeof(BackStationPage),
                    MenuKeys.CloudDownload => typeof(CloudDownloadPage),
                    MenuKeys.DownloadList => typeof(DownloadListPage),
                    MenuKeys.UploadList => typeof(UploadListPage),
                    MenuKeys.User => typeof(UserPage),
                    MenuKeys.Settings => typeof(SettingsPage),
                    MenuKeys.SearchFiles => typeof(SearchFilesPage),
                    _ => null
                };
            }
            if (type is null)
            {
                viewModel.NavigateToBlank();
                return;
            }
            viewModel.NavigateToPage(type);
        }

        private void btn_close_Click(object sender, RoutedEventArgs e)
        {
            var system = App.Resolve<SystemInfoModel>();
            if (system.CloseToTray)
            {
                HideToTray();
                return;
            }

            RequestExit();
        }

        private void btn_min_Click(object sender, RoutedEventArgs e)
        {
            HideToTray();
        }

        private void HideToTray()
        {
            _appWindow.Hide();
        }

        private void RequestExit()
        {
            _isExitRequested = true;
            Close();
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

        public async Task ShowMessageBar(string msg, string title, InfoBarSeverity severity = InfoBarSeverity.Informational, bool showClose = true, TimeSpan? autoClose = null)
        {
            await DispatcherQueue.EnqueueAsync(() =>
            {
                messageBar.Message = msg;
                messageBar.Title = title;
                messageBar.Severity = severity;
                messageBar.IsClosable = showClose;
                messageBar.IsOpen = true;
                messageBar.Tag = null;
                if (autoClose is not null)
                {
                    messageBar.Tag = Guid.NewGuid();
                    DispatcherTimer timer = new DispatcherTimer();
                    timer.Interval = autoClose.Value;
                    timer.Tick += messageBar_Tick;
                    timer.Start();
                }
            });
        }

        public async Task JumpPage(MenuKeys? menu)
        {
            if (menu is null)
            {
                viewModel.NavigateToBlank();
                return;
            }
            DispatcherQueue.TryEnqueue(() =>
            {
                var item = viewModel.MenuItems.FirstOrDefault(x => (x.Tag as MenuKeys?) == menu);
                if (item is null)
                {
                    if (menu == MenuKeys.User)
                    {
                        viewModel.NavigateToPage(typeof(UserPage));
                    }
                    return;
                }
                RootNavigationView.SelectedItem = item;
            });
        }

        public async Task SetFace(string url)
        {
            DispatcherQueue.TryEnqueue(() => 
            {
                img_face.Source = new BitmapImage(new Uri(url));
            });
        }

        private void messageBar_Tick(object? sender, object e)
        {
            if (sender is not DispatcherTimer timer || messageBar.Tag == null)
            {
                return;
            }
            messageBar.IsOpen = false;
            timer.Stop();
            GC.Collect();
        }

        private async void NavViewSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (viewModel is null || viewModel.User.IsLogin != true)
            {
                await App.ShowMessageBar($"请登录后再使用！", "警告", severity: InfoBarSeverity.Warning, autoClose: TimeSpan.FromSeconds(3));
                return;
            }
            var searchValue = NavViewSearchBox.Text.StringTrim();
            if (searchValue.IsBlank())
            {
                await App.ShowMessageBar($"请输入你要查询的关键字！", "警告", severity: InfoBarSeverity.Warning, autoClose: TimeSpan.FromSeconds(4));
                return;
            }
            var vm = App.Resolve<SearchFilesViewModel>();
            if (vm is null)
            {
                return;
            }
            vm.SearchValue = searchValue;
            await vm.RefreshFilesCommand.ExecuteAsync(null);
            await JumpPage(MenuKeys.SearchFiles);
        }

        internal async Task UpdatePathBar()
        {
            var vm = App.Resolve<MyFilesViewModel>();
            if (vm is null || RootFrame.Content is not MyFilesPage ui)
            {
                return;
            }
            var paths = new List<SelectOptionItem>();
            paths.AddRange(vm.PathItems);
            await DispatcherQueue.EnqueueAsync(() =>
            {
                ui.UpdatePathBar(paths);
            });
        }

        internal async Task SelectedItemAndScrollIntoView(int index, MyFileItemModel item)
        {
            var vm = App.Resolve<MyFilesViewModel>();
            if (vm is null || RootFrame.Content is not MyFilesPage ui)
            {
                return;
            }
            await DispatcherQueue.EnqueueAsync(() =>
            {
                ui.SelectedItemAndScrollIntoView(index, item);
            });
        }
    }
}
