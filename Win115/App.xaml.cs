using Autofac;
using LiteDB;
using Microsoft.Windows.AppLifecycle;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RestSharp;
using RestSharp.Serializers.Json;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Win115.Enums;
using Win115.Handlers;
using Win115.Models;
using Win115.Services;
using Win115.ViewModels;

namespace Win115
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        public static string AppPath { get; } = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        public static DispatcherQueue? DispatcherQueue { get; set; } = null;
        public static string CodeVerifier { get; set; } = string.Empty;
        public static RestClient LoginClient { get; } = new RestClient(new RestClientOptions("https://passportapi.115.com")
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36 Edg/140.0.0.0",
        }, configureSerialization: s => s.UseSystemTextJson());
        public static RestClient ProApiClient { get; } = new RestClient(new RestClientOptions("https://proapi.115.com")
        {
            ConfigureMessageHandler = h => 
            {
                var tokenHandler = new TokenRefreshHandler { InnerHandler = h };
                return new ApiRateLimitHandler { InnerHandler = tokenHandler };
            }
        }, configureSerialization: s => s.UseSystemTextJson());
        public static RestClient QrCodeClient { get; } = new RestClient(new RestClientOptions("https://qrcodeapi.115.com")
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36 Edg/140.0.0.0",
        }, configureSerialization: s => s.UseSystemTextJson());
        public static XamlRoot? XamlRoot => _window?.Content.XamlRoot;

        private static Window? _window;
        private static IContainer? _container;
        private AppInstance? _mainInstance;
        private bool _isActivationPending;
        public static nint WindowHandle;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            _mainInstance = AppInstance.FindOrRegisterForKey("Win115.MainInstance");
            if (!_mainInstance.IsCurrent)
            {
                await _mainInstance.RedirectActivationToAsync(AppInstance.GetCurrent().GetActivatedEventArgs());
                Exit();
                return;
            }

            _mainInstance.Activated += MainInstance_Activated;

            ContainerBuilder builder = new ContainerBuilder();
            // Models
            builder.RegisterType<UserInfoModel>().AsSelf().SingleInstance();
            builder.RegisterType<SystemInfoModel>().AsSelf().SingleInstance();
            builder.RegisterType<DownloadEngine>().AsSelf().SingleInstance();

            //ViewModels
            builder.RegisterType<MainViewModel>().AsSelf().SingleInstance();
            builder.RegisterType<SettingsViewModel>().AsSelf().SingleInstance();
            builder.RegisterType<UserViewModel>().AsSelf().SingleInstance();
            builder.RegisterType<MyFilesViewModel>().AsSelf().SingleInstance();
            builder.RegisterType<SearchFilesViewModel>().AsSelf().SingleInstance();
            builder.RegisterType<UploadListViewModel>().AsSelf().SingleInstance();
            builder.RegisterType<DownloadListViewModel>().AsSelf().SingleInstance();
            builder.RegisterType<CloudDownloadViewModel>().AsSelf().SingleInstance();
            builder.RegisterType<BackStationViewModel>().AsSelf().SingleInstance();
            builder.RegisterType<AboutViewModel>().AsSelf().SingleInstance();

            builder.RegisterType<LoginViewModel>().AsSelf().InstancePerLifetimeScope();
            builder.RegisterType<NewFolderViewModel>().AsSelf().InstancePerLifetimeScope();
            builder.RegisterType<NewCloudDownloadViewModel>().AsSelf().InstancePerLifetimeScope();
            builder.RegisterType<SelectSavePathViewModel>().AsSelf().InstancePerLifetimeScope();
            builder.RegisterType<ViewImagesViewModel>().AsSelf().InstancePerLifetimeScope();
            builder.RegisterType<ViewMediasViewModel>().AsSelf().InstancePerLifetimeScope();

            builder.RegisterInstance(new RestClient()).AsSelf().SingleInstance();
            builder.RegisterInstance(new LiteDatabase(Path.Combine(App.AppPath, "app.db"))).AsSelf().SingleInstance();

            _container = builder.Build();

            // 检查License
            // CheckLicense();

            _window = new MainWindow();
            _window.Activate();
            WindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(_window);
            Resources["ContentDialogMaxWidth"] = 99999d;

            if (_isActivationPending)
            {
                ((MainWindow)_window).ShowAndActivate();
                _isActivationPending = false;
            }
        }

        private void MainInstance_Activated(object? sender, AppActivationArguments args)
        {
            DispatcherQueue?.TryEnqueue(() =>
            {
                if (_window is MainWindow mainWindow)
                {
                    mainWindow.ShowAndActivate();
                    return;
                }

                _isActivationPending = true;
            });
        }

        public static Task ShowMessageBar(string msg, string title, InfoBarSeverity severity = InfoBarSeverity.Informational, bool showClose = true, TimeSpan? autoClose = null)
        {
            if (_window is null || _window is not MainWindow mw)
            {
                return Task.CompletedTask;
            }
            return mw.ShowMessageBar(msg, title, severity, showClose, autoClose); ;
        }

        public static Task SetFace(string url)
        {
            if (_window is null || _window is not MainWindow mw)
            {
                return Task.CompletedTask;
            }
            return mw.SetFace(url);
        }

        public static Task JumpPage(MenuKeys? menu)
        {
            if (_window is null || _window is not MainWindow mw)
            {
                return Task.CompletedTask;
            }
            return mw.JumpPage(menu);
        }

        public static Task UpdatePathBar()
        {
            if (_window is null || _window is not MainWindow mw)
            {
                return Task.CompletedTask;
            }
            return mw.UpdatePathBar();
        }

        public static Task SelectedItemAndScrollIntoView(int index, MyFileItemModel item)
        {
            if (_window is null || _window is not MainWindow mw)
            {
                return Task.CompletedTask;
            }
            return mw.SelectedItemAndScrollIntoView(index, item);
        }

        public static T Resolve<T>() where T : notnull
        {
            if (_container is null)
            {
                throw new Exception();
            }
            return _container.Resolve<T>();
        }

        public static ILifetimeScope CreateScope()
        {
            if (_container is null)
            {
                throw new Exception();
            }
            return _container.BeginLifetimeScope();
        }
    }
}
