using CommunityToolkit.Mvvm.ComponentModel;
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
    }
}
