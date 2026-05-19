using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public sealed partial class BackStationPage : Page
    {
        private BackStationViewModel? viewModel;
        private UserInfoModel? _user;

        public BackStationPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            viewModel = App.Resolve<BackStationViewModel>();
            _user = App.Resolve<UserInfoModel>();
        }

        private void btn_delete_item_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not RbFileItemModel item)
            {
                return;
            }
            item.ShowDeleteTip = true;
        }

        private void btn_recycle_item_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not RbFileItemModel item)
            {
                return;
            }
            item.ShowRecycleTip = true;
        }

        private void chk_all_Checked(object sender, RoutedEventArgs e)
        {
            if (viewModel is null)
            {
                return;
            }
            lv.SelectAll();
        }

        private void chk_all_Unchecked(object sender, RoutedEventArgs e)
        {
            if (viewModel is null)
            {
                return;
            }
            lv.SelectedItems.Clear();
        }

        private void lv_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ListView || viewModel is null|| viewModel.FileItems.Count == 0 || _user is null)
            {
                return;
            }
            Debug.WriteLine($"===>lv_SelectionChanged");
            if (e.RemovedItems is not null && e.RemovedItems.Count > 0)
            {
                foreach (var f in e.RemovedItems)
                {
                    try
                    {
                        viewModel.SelectedFileItems.Remove((RbFileItemModel)f);
                    }
                    catch { }
                }
            }
            if (e.AddedItems is not null && e.AddedItems.Count > 0)
            {
                foreach (var f in e.AddedItems)
                {
                    var _f = (RbFileItemModel)f;
                    try
                    {
                        if (viewModel.SelectedFileItems.Any(x => x.Id == _f.Id))
                        {
                            continue;
                        }
                        viewModel.SelectedFileItems.Add(_f);
                    }
                    catch { }
                }
            }
            viewModel.HasSelectedItems = viewModel.SelectedFileItems.Count > 0 && _user.IsLogin;
            if (viewModel.SelectedFileItems.Count == viewModel.FileItems.Count)
            {
                viewModel.IsCheckAll = viewModel.SelectedFileItems.Count == viewModel.FileItems.Count;
            }
            else if (viewModel.HasSelectedItems)
            {
                viewModel.IsCheckAll = null;
            }
            else
            {
                viewModel.IsCheckAll = false;
            }
        }
    }
}
