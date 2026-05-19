using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;
using Win115.Models;

namespace Win115.ViewModels
{
    public partial class SelectSavePathViewModel : ObservableRecipient
    {
        [ObservableProperty]
        public partial UserInfoModel User { get; set; }

        [ObservableProperty]
        public partial string? SavePath { get; set; } = "根目录";

        [ObservableProperty]
        public partial long? SavePathId { get; set; } = 0;

        public SelectSavePathViewModel(UserInfoModel user)
        {
            User = user;
        }
    }
}
