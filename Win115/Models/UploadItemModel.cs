using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Win115.Enums;
using Win115.Helpers;
using Microsoft.UI.Xaml;
using LiteDB;
using Win115.Entities;
using Win115.Properties;
using Win115.ViewModels;

namespace Win115.Models
{
    public partial class UploadItemModel : ObservableObject
    {
        [ObservableProperty]
        public partial int? TaskId { get; set; } = 0;

        [ObservableProperty]
        public partial string? UploadId { get; set; } = string.Empty;

        [ObservableProperty]
        public partial Dictionary<int, string>? PartETags { get; set; } = new Dictionary<int, string>();

        [ObservableProperty]
        public partial long? PartNumber { get; set; } = -1;

        [ObservableProperty]
        public partial string? FileId { get; set; } = string.Empty;

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
        public partial long? UploadedSize { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ProgressText))]
        public partial double? Progress { get; set; } = 0;
        public string? ProgressText => Progress.HasValue ? $"{Progress:P}" : "-";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SpeedText))]
        public partial long? Speed { get; set; }

        public string SpeedText => Speed > 0 ? StringHelper.FormatDownloadSpeed(Speed) : "-";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RemainingTimeText))]
        public partial TimeSpan? RemainingTime { get; set; }

        public string RemainingTimeText => RemainingTime is { } remaining && remaining > TimeSpan.Zero
            ? $"剩余 {StringHelper.FormatTimeSpan(remaining)}"
            : "-";

        public string TaskInfoText => IsFolder && TotalFiles is > 0
            ? $"{StateText} · {TotalFiles} 个文件"
            : StateText;

        public string StateText => State switch
        {
            UploadTaskStateEnum.Queued => "队列中",
            UploadTaskStateEnum.CalcHash => "校验中",
            UploadTaskStateEnum.Uploading => "上传中",
            UploadTaskStateEnum.Paused => "已暂停",
            UploadTaskStateEnum.Completed => "已完成",
            UploadTaskStateEnum.Failed => "失败",
            UploadTaskStateEnum.Canceled => "已取消",
            _ => "-"
        };

        [ObservableProperty]
        public partial string? ParentId { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string? FilePath { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string? PickCode { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string? Bucket { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string? Object { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string? SignCheck { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string? SignKey { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string? Endpoint { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string? Region { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string? AccessKeySecret { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string? SecurityToken { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string? Expiration { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string? AccessKeyId { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string? Callback { get; set; } = string.Empty;

        [ObservableProperty]
        public partial Dictionary<string, string>? CallbackVar { get; set; } = new Dictionary<string, string>();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowRemoteButton))]
        [NotifyPropertyChangedFor(nameof(ShowPauseButton))]
        [NotifyPropertyChangedFor(nameof(ShowStartButton))]
        [NotifyPropertyChangedFor(nameof(ShowRestartButton))]
        [NotifyPropertyChangedFor(nameof(StateText))]
        [NotifyPropertyChangedFor(nameof(TaskInfoText))]
        public partial UploadTaskStateEnum? State { get; set; } = UploadTaskStateEnum.Canceled;

        public Visibility ShowRemoteButton => State == UploadTaskStateEnum.Completed ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ShowPauseButton => State == UploadTaskStateEnum.Uploading
            || State == UploadTaskStateEnum.Queued
            ? Visibility.Visible
            : Visibility.Collapsed;
        public Visibility ShowStartButton => State == UploadTaskStateEnum.Paused ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ShowRestartButton => State is UploadTaskStateEnum.Failed or UploadTaskStateEnum.Canceled
            ? Visibility.Visible
            : Visibility.Collapsed;

        [ObservableProperty]
        public partial bool ShowDeleteTip { get; set; } = false;

        [RelayCommand]
        private Task Pause()
        {
            if (State == UploadTaskStateEnum.Uploading || State == UploadTaskStateEnum.Queued) State = UploadTaskStateEnum.Paused;
            return Task.CompletedTask;
        }

        [RelayCommand]
        private Task Start()
        {
            if (State == UploadTaskStateEnum.Paused) State = UploadTaskStateEnum.Queued;
            return Task.CompletedTask;
        }

        [RelayCommand]
        private Task Restart()
        {
            if (State is not UploadTaskStateEnum.Failed and not UploadTaskStateEnum.Canceled)
            {
                return Task.CompletedTask;
            }

            return App.Resolve<UploadListViewModel>().RetryTaskAsync(this);
        }

        [RelayCommand]
        private async Task OpenLocal()
        {
        }

        [RelayCommand]
        private async Task OpenRemote()
        {
        }

        [RelayCommand]
        private Task Delete()
        {
            State = UploadTaskStateEnum.Canceled;

            var db = App.Resolve<LiteDatabase>();
            var collection = db.GetCollection<UploadTaskEntity>(CollectionResource.UploadTask);
            if (TaskId is > 0)
            {
                collection.Delete(TaskId.Value);
            }
            else
            {
                collection.DeleteMany(x => x.Name == Name
                    && x.FilePath == FilePath
                    && x.ParentId == ParentId
                    && x.Size == Size);
            }

            var viewModel = App.Resolve<UploadListViewModel>();
            viewModel.UploadItems.Remove(this);
            ShowDeleteTip = false;
            return Task.CompletedTask;
        }
    }
}
