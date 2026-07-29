using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using System;
using Win115.Services;

namespace Win115.Models
{
    public partial class SystemInfoModel : ObservableObject
    {
        [ObservableProperty]
        public partial int ApiRateLimit { get; set; } = ApiSettings.DefaultRateLimit;

        [ObservableProperty]
        public partial string? DownloadDirPath { get; set; } = string.Empty;

        [ObservableProperty]
        public partial int DownloadConcurrentTasks { get; set; } = DownloadSettings.DefaultConcurrentTasks;

        [ObservableProperty]
        public partial int DownloadSegmentCount { get; set; } = DownloadSettings.DefaultSegmentCount;

        [ObservableProperty]
        public partial int DownloadSpeedLimitKbps { get; set; }

        [ObservableProperty]
        public partial int UploadMaxRetry { get; set; } = UploadSettings.DefaultMaxRetry;

        [ObservableProperty]
        public partial int UploadConcurrentTasks { get; set; } = UploadSettings.DefaultMaxConcurrentTasks;

        [ObservableProperty]
        public partial bool CloseToTray { get; set; } = WindowSettings.DefaultCloseToTray;
    }
}
