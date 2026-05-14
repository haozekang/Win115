using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Win115.Models
{
    public partial class UploadItemModel : ObservableObject
    {
        [ObservableProperty]
        public partial string? Name { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string? Size { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string? Progress { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool ShowDeleteTip { get; set; } = false;

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
