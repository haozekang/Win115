using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;
using Win115.Models;

namespace Win115.ViewModels
{
    public partial class NewFolderViewModel : ObservableRecipient
    {
        [ObservableProperty]
        public partial UserInfoModel User { get; set; }

        [ObservableProperty]
        public partial string FileName { get; set; } = string.Empty;

        public NewFolderViewModel(UserInfoModel user)
        {
            User = user;
        }
    }
}
