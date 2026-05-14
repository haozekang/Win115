using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Win115.Models;

namespace Win115.ViewModels
{
    public partial class UploadListViewModel : ObservableRecipient
    {
        [ObservableProperty]
        public partial UserInfoModel User { get; set; }

        [ObservableProperty]
        public partial ObservableCollection<UploadItemModel> UploadItems { get; set; }

        public UploadListViewModel(UserInfoModel user)
        {
            User = user;
            UploadItems = new() 
            {
                new UploadItemModel{ Name = "jd-gui-1.6.6.jar", Size = "3.09 MB", Progress = "上传完成", ShowDeleteTip = false },
                new UploadItemModel{ Name = "ubuntu-26.04-wsl-arm64.wsl", Size = "390.47 MB", Progress = "上传完成", ShowDeleteTip = false },
            };
        }

        [RelayCommand]
        public async Task ClearFinish()
        {
        }

        [RelayCommand]
        public async Task PauseAll()
        {
        }

        [RelayCommand]
        public async Task StartAll()
        {
        }
    }
}
