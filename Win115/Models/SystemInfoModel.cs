using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using System;

namespace Win115.Models
{
    public partial class SystemInfoModel : ObservableObject
    {
        [ObservableProperty]
        public partial string? DownloadDirPath { get; set; } = string.Empty;
    }
}
