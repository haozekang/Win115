using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Win115.Enums;
using Win115.Helpers;

namespace Win115.Models
{
    public partial class UploadItemModel : ObservableObject
    {
        [ObservableProperty]
        public partial string? FileId { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string? Name { get; set; } = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SizeText))]
        public partial long? Size { get; set; } = 0;
        public string? SizeText => Size > 0 ? StringHelper.FormatFileSize(Size) : "-";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ProgressText))]
        public partial double? Progress { get; set; } = 0;
        public string? ProgressText => Progress.HasValue ? $"{Progress:P}" : "-";

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
        public partial UploadTaskStateEnum? State { get; set; } = UploadTaskStateEnum.Canceled;

        [ObservableProperty]
        public partial bool ShowDeleteTip { get; set; } = false;

        [RelayCommand]
        private async Task Pause()
        {
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
        private async Task Delete()
        {
        }
    }
}
