using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Win115.ViewModels
{
    public partial class LoginViewModel : ObservableRecipient
    {
        [ObservableProperty]
        public partial Visibility ScanInfoVisibility { get; set; } = Visibility.Visible;

        [ObservableProperty]
        public partial Visibility ScanSuccessfulVisibility { get; set; } = Visibility.Collapsed;

        [ObservableProperty]
        public partial Visibility ScanFailedVisibility { get; set; } = Visibility.Collapsed;
    }
}
