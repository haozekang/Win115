using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Win115.Models;
using Win115.ViewModels;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Win115.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class UploadListPage : Page
    {
        private UploadListViewModel? viewModel;
        private UserInfoModel? _user;

        public UploadListPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            viewModel = App.Resolve<UploadListViewModel>();
            _user = App.Resolve<UserInfoModel>();
        }

        private void btn_delete_item_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not UploadItemModel item)
            {
                return;
            }
            item.ShowDeleteTip = true;
        }
    }
}
