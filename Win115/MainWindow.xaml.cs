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
using System.Threading.Tasks;
using Tanovo.ExtensionMethods;
using Win115.Entities;
using Win115.Enums;
using Win115.Models;
using Win115.Properties;
using Win115.ViewModels;
using Win115.Views;
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
        private MainViewModel viewModel;

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
            var appWindow = AppWindow.GetFromWindowId(windowId);

            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.SetBorderAndTitleBar(true, false);
            }
            viewModel.SelectedItem = viewModel.MenuItems?.FirstOrDefault();
            AppWindow.SetIcon("/Assets/favicon.ico");

            _ = LoadSystemConfigAsync();
        }

        private async Task LoadSystemConfigAsync()
        {
            var _user = App.Resolve<UserInfoModel>();
            var _system = App.Resolve<SystemInfoModel>();
            var _db = App.Resolve<LiteDatabase>();
            if (_db.CollectionExists(CollectionResource.System) != true)
            {
                return;
            }
            var col = _db.GetCollection<SystemEntity>(CollectionResource.System);
            var downloadDirPath = col.Query().Where(x => x.Type == SystemConfigTypeResource.DownloadDirPath).SingleOrDefault();
            if (downloadDirPath is not null)
            {
                _system.DownloadDirPath = downloadDirPath.Value;
            }

            // 载入历史下载记录
            var downloadTaskCol = _db.GetCollection<DownloadTaskEntity>(CollectionResource.DownloadTask);
            var tasks = downloadTaskCol.FindAll();
            var downloadListViewModel = App.Resolve<DownloadListViewModel>();
            foreach (var task in tasks)
            {
                downloadListViewModel.DownloadItems.Add(new DownloadItemModel 
                {
                    Name = task.Name,
                    Progress = task.Progress,
                    State = task.State.HasValue ? task.State.Value == DownloadTaskStateEnum.Queued ? DownloadTaskStateEnum.Paused : task.State.Value : DownloadTaskStateEnum.Canceled,
                    SavePath = task.SavePath,
                    Size = task.Size,
                    PickCode = task.PickCode,
                    Url = task.Url
                });
            }
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
                    MenuKeys.SearchFiles => typeof(SettingsPage),
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
            this.Close();
        }

        private void btn_min_Click(object sender, RoutedEventArgs e)
        {
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.Minimize();
            }
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

        public Task ShowMessageBar(string msg, string title, InfoBarSeverity severity = InfoBarSeverity.Informational, bool showClose = true, TimeSpan? autoClose = null)
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
            return Task.CompletedTask;
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
                    if (menu == MenuKeys.SearchFiles)
                    {
                        viewModel.NavigateToPage(typeof(SearchFilesPage));
                    }
                    return;
                }
                RootNavigationView.SelectedItem = item;
            });
        }

        public Task SetFace(string url)
        {
            img_face.Source = new BitmapImage(new Uri(url));
            return Task.CompletedTask;
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
            var searchValue = NavViewSearchBox.Text.StringTrim();
            if (searchValue.IsBlank())
            {
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
    }
}
