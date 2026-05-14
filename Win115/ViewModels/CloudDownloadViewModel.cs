using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Win115.Models;

namespace Win115.ViewModels
{
    public partial class CloudDownloadViewModel : ObservableRecipient
    {
        [ObservableProperty]
        public partial UserInfoModel User { get; set; }

        public CloudDownloadViewModel(UserInfoModel user)
        {
            User = user;
        }
    }
}
