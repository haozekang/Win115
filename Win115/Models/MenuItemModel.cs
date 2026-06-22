using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Text;

namespace Win115.Models
{
    public partial class MenuItemModel : ObservableObject
    {
        [ObservableProperty]
        public partial object? Content { get; set; }

        [ObservableProperty]
        public partial IconElement? Icon { get; set; }

        [ObservableProperty]
        public partial object? Tag { get; set; }
    }
}
