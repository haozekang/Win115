using Autofac;
using LiteDB;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using RestSharp;
using RestSharp.Serializers.Json;
using RestSharp.Serializers.NewtonsoftJson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Tanovo.ExtensionMethods;
using Win115.Enums;
using Win115.Handlers;
using Win115.Models;
using Win115.ViewModels;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Win115
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        public static string AppPath { get; } = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        public static string CodeVerifier { get; set; } = string.Empty;
        public static DispatcherQueue? DispatcherQueue { get; set; } = null;
        public static RestClient LoginClient { get; } = new RestClient(new RestClientOptions("https://passportapi.115.com")
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36 Edg/140.0.0.0",
        }, configureSerialization: s => s.UseNewtonsoftJson());
        public static RestClient ProApiClient { get; } = new RestClient(new RestClientOptions("https://proapi.115.com")
        {
            ConfigureMessageHandler = h => 
            {
                var handler = new TokenRefreshHandler();
                handler.InnerHandler = h;
                return handler;
            }
        }, configureSerialization: s => s.UseNewtonsoftJson());
        public static RestClient QrCodeClient { get; } = new RestClient(new RestClientOptions("https://qrcodeapi.115.com")
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36 Edg/140.0.0.0",
        }, configureSerialization: s => s.UseNewtonsoftJson());
        public static XamlRoot? XamlRoot => _window?.Content.XamlRoot;

        /// <summary>
        /// 心跳线程，保证持续在线，避免因长时间没操作导致的下线
        /// </summary>
        public static Thread? KeepAliveThread { get; set; }

        private static Window? _window;
        private static IContainer? _container;
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
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            ContainerBuilder builder = new ContainerBuilder();
            // Models
            builder.RegisterType<UserInfoModel>().AsSelf().SingleInstance();
            builder.RegisterType<SystemInfoModel>().AsSelf().SingleInstance();

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

            builder.RegisterInstance(new RestClient(configureSerialization: s =>
            {
                s.UseNewtonsoftJson();
            })).AsSelf().SingleInstance();
            builder.RegisterInstance(new LiteDatabase(Path.Combine(App.AppPath, "app.db"))).AsSelf().SingleInstance();

            _container = builder.Build();

            // 检查License
            // CheckLicense();

            _window = new MainWindow();
            _window.Activate();
            WindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(_window);
            Resources["ContentDialogMaxWidth"] = 99999d;
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
