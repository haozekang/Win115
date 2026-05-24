using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanovo.ExtensionMethods;
using Win115.Models;

namespace Win115.ViewModels
{
    public partial class UserViewModel : ObservableRecipient
    {
        [ObservableProperty]
        public partial UserInfoModel User { get; set; }

        public UserViewModel(UserInfoModel User) 
        {
            this.User = User;
        }

        [RelayCommand]
        public async Task ClearData()
        {
            App.DispatcherQueue?.TryEnqueue(() => 
            {
                User.IsLogin = false;
                User.UserId = string.Empty;
                User.UserName = string.Empty;
                User.FaceS = string.Empty;
                User.FaceM = string.Empty;
                User.FaceL = string.Empty;
                User.AllTotalSize = 0;
                User.AllTotalFormat = string.Empty;
                User.AllRemainSize = 0;
                User.AllRemainFormat = string.Empty;
                User.AllUseSize = 0;
                User.AllUseFormat = string.Empty;
                User.VipLevelName = string.Empty;
                User.VipExpire = null;
            });
        }
    }
}
