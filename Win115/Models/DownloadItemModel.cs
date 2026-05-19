using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Downloader;
using LiteDB;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanovo.ExtensionMethods;
using Win115.Entities;
using Win115.Enums;
using Win115.Helpers;
using Win115.Properties;
using Win115.ViewModels;

namespace Win115.Models
{
    public partial class DownloadItemModel : ObservableObject
    {
        [ObservableProperty]
        public partial int? TaskId { get; set; } = 0;

        [ObservableProperty]
        public partial string? Name { get; set; } = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SizeText))]
        public partial long? Size { get; set; } = 0;

        public string? SizeText => Size > 0 ? StringHelper.FormatFileSize(Size) : "-";

        [ObservableProperty]
        public partial string? Progress { get; set; } = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SpeedText))]
        public partial long? Speed { get; set; } = 0;

        public string? SpeedText => Speed > 0 ? StringHelper.FormatDownloadSpeed(Speed) : "-";

        [ObservableProperty]
        public partial string? Url { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string? SavePath { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string? PickCode { get; set; } = string.Empty;

        [ObservableProperty]
        public partial DownloadTaskStateEnum State { get; set; } = DownloadTaskStateEnum.Canceled;

        [ObservableProperty]
        public partial bool ShowDeleteTip { get; set; } = false;

        [RelayCommand]
        private async Task Open()
        {
            if (SavePath.IsNotBlank() && SavePath.AsFilePathAndExists())
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = SavePath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    await LogHelper.Error(ex);
                }
            }
        }

        [RelayCommand]
        private async Task Delete()
        {
            ContentDialog dialog = new ContentDialog();
            var content = new StackPanel();
            var checkBox = new CheckBox() 
            {
                VerticalContentAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center,
                VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center,
                Content = "是否删除本地文件？"
            };
            dialog.Tag = checkBox;
            content.Children.Add(checkBox);
            dialog.XamlRoot = App.XamlRoot;
            dialog.Title = "警告";
            dialog.Content = content;
            dialog.PrimaryButtonText = "确认";
            dialog.SecondaryButtonText = "取消";
            dialog.DefaultButton = ContentDialogButton.Secondary;
            dialog.PrimaryButtonClick += Delete;
            await dialog.ShowAsync();
        }

        private async void Delete(ContentDialog dialog, ContentDialogButtonClickEventArgs args)
        {
            if (dialog.Tag is not CheckBox cmb)
            {
                return;
            }
            if (cmb.IsChecked == true && SavePath.IsNotBlank() && SavePath.AsFilePathAndExists())
            {
                try
                {
                    File.Delete(SavePath);
                }
                catch(Exception ex) 
                {
                    await LogHelper.Error(ex);
                }
            }
            var _db = App.Resolve<LiteDatabase>();
            var col = _db.GetCollection<DownloadTaskEntity>(CollectionResource.DownloadTask);
            DownloadTaskEntity? find = null;
            if (TaskId is null || TaskId <= 0)
            {
                find = col.Query().Where(x => x.Name == Name && x.PickCode == PickCode && x.SavePath == SavePath && x.Size == Size && x.Url == Url).SingleOrDefault();
            }
            else
            {
                find = col.FindById(TaskId);
            }
            if (find is not null)
            {
                col.Delete(find.Id);
            }
            var vm = App.Resolve<DownloadListViewModel>();
            vm.DownloadItems.Remove(this);
        }
    }
}
