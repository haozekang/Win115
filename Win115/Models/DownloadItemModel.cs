using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteDB;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.IO;
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

        public string? SizeText => IsFolder && TotalFiles is > 0
            ? $"{StringHelper.FormatFileSize(Size)} · {TotalFiles} 个文件"
            : Size > 0 ? StringHelper.FormatFileSize(Size) : "-";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SizeText))]
        [NotifyPropertyChangedFor(nameof(TaskInfoText))]
        public partial bool IsFolder { get; set; }

        [ObservableProperty]
        public partial int? ParentTaskId { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SizeText))]
        [NotifyPropertyChangedFor(nameof(TaskInfoText))]
        public partial int? TotalFiles { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ProgressText))]
        public partial double? Progress { get; set; } = 0;
        public string? ProgressText => Progress.HasValue ? $"{Progress:P}" : "-";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SpeedText))]
        public partial long? Speed { get; set; } = 0;

        public string? SpeedText => Speed > 0 ? StringHelper.FormatDownloadSpeed(Speed) : "-";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RemainingTimeText))]
        public partial TimeSpan? RemainingTime { get; set; }

        public string RemainingTimeText => RemainingTime is { } remaining && remaining > TimeSpan.Zero
            ? $"剩余 {StringHelper.FormatTimeSpan(remaining)}"
            : "-";

        public string TaskInfoText => IsFolder && TotalFiles is > 0
            ? $"{StateText} · {TotalFiles} 个文件"
            : StateText ?? "-";

        [ObservableProperty]
        public partial string? Url { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string? SavePath { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string? PickCode { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string? SourceFolderId { get; set; } = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StateText))]
        [NotifyPropertyChangedFor(nameof(TaskInfoText))]
        [NotifyPropertyChangedFor(nameof(ShowPauseButton))]
        [NotifyPropertyChangedFor(nameof(ShowStartButton))]
        [NotifyPropertyChangedFor(nameof(ShowRestartButton))]
        [NotifyPropertyChangedFor(nameof(ShowOpenButton))]
        [NotifyPropertyChangedFor(nameof(ShowActionSeparator))]
        public partial DownloadTaskStateEnum State { get; set; } = DownloadTaskStateEnum.Canceled;
        public string? StateText => State switch 
        {
            DownloadTaskStateEnum.Paused => "暂停",
            DownloadTaskStateEnum.Queued => "队列中",
            DownloadTaskStateEnum.Downloading => "下载中",
            DownloadTaskStateEnum.Completed => "完成",
            DownloadTaskStateEnum.Failed => "失败",
            DownloadTaskStateEnum.Canceled => "已取消",
            _ => "-",
        };

        public Visibility ShowPauseButton => State == DownloadTaskStateEnum.Downloading || State == DownloadTaskStateEnum.Queued ? Visibility.Visible : Visibility.Collapsed;

        public Visibility ShowStartButton => State == DownloadTaskStateEnum.Paused ? Visibility.Visible : Visibility.Collapsed;

        public Visibility ShowRestartButton => State == DownloadTaskStateEnum.Failed || State == DownloadTaskStateEnum.Canceled ? Visibility.Visible : Visibility.Collapsed;

        public Visibility ShowOpenButton => State == DownloadTaskStateEnum.Completed ? Visibility.Visible : Visibility.Collapsed;

        public Visibility ShowActionSeparator => ShowPauseButton == Visibility.Visible
            || ShowStartButton == Visibility.Visible
            || ShowRestartButton == Visibility.Visible
            ? Visibility.Visible
            : Visibility.Collapsed;

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
        private async Task OpenFolder()
        {
            if (SavePath.IsBlank())
            {
                return;
            }

            var folderPath = Path.GetDirectoryName(SavePath);
            if (folderPath.IsBlank() || !Directory.Exists(folderPath))
            {
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = File.Exists(SavePath) ? $"/select,\"{SavePath}\"" : $"\"{folderPath}\"",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                await LogHelper.Error(ex);
            }
        }

        [RelayCommand]
        private async Task Pause()
        {
            if (State != DownloadTaskStateEnum.Downloading)
            {
                return;
            }
            State = DownloadTaskStateEnum.Paused;
        }

        [RelayCommand]
        private async Task Start()
        {
            if (State != DownloadTaskStateEnum.Paused)
            {
                return;
            }
            State = DownloadTaskStateEnum.Queued;
        }

        [RelayCommand]
        private Task Restart()
        {
            if (State != DownloadTaskStateEnum.Failed && State != DownloadTaskStateEnum.Canceled)
            {
                return Task.CompletedTask;
            }

            return App.Resolve<DownloadListViewModel>().RetryTaskAsync(this);
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
