using Microsoft.UI.Input;
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
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Xml.Linq;
using Tanovo.ExtensionMethods;
using Win115.Models;
using Win115.ViewModels;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Win115.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SearchFilesPage : Page
    {
        private SearchFilesViewModel? viewModel;
        private UserInfoModel? _user;

        public SearchFilesPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            viewModel = App.Resolve<SearchFilesViewModel>();
            _user = App.Resolve<UserInfoModel>();
        }

        private async void menu_open_Click(object sender, RoutedEventArgs e)
        {
            await OpenAsync(sender, e);
        }

        private async void item_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            await OpenAsync(sender, e);
        }

        private async Task OpenAsync(object sender, RoutedEventArgs e)
        {
            if (viewModel is null)
            {
                return;
            }
            if (e.OriginalSource is FrameworkElement element)
            {
                MyFileItemModel? item = null;
                if (element.DataContext is MyFileItemModel)
                {
                    item = (MyFileItemModel)element.DataContext;
                }
                else if (sender is ItemContainer ic && ic.Tag is string id)
                {
                    item = viewModel.FileItems.FirstOrDefault(x => x.Id == id);
                }
            }
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
                        viewModel.SelectedFileItems.Remove((MyFileItemModel)f);
                    }
                    catch { }
                }
            }
            if (e.AddedItems is not null && e.AddedItems.Count > 0)
            {
                foreach (var f in e.AddedItems)
                {
                    var _f = (MyFileItemModel)f;
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

        private void chk_all_Checked(object sender, RoutedEventArgs e)
        {
            if (viewModel is null)
            {
                return;
            }
            if (viewModel.ListVisibility == Visibility.Visible)
            {
                lv.SelectAll();
            }
            else
            {
                iv.SelectAll();
            }
        }

        private void chk_all_Unchecked(object sender, RoutedEventArgs e)
        {
            if (viewModel is null)
            {
                return;
            }
            if (viewModel.ListVisibility == Visibility.Visible)
            {
                lv.SelectedItems.Clear();
            }
            else
            {
                iv.DeselectAll();
            }
        }

        private void chk_viewAll_Checked(object sender, RoutedEventArgs e)
        {
            if (viewModel is null)
            {
                return;
            }
            viewModel.ListVisibility = Visibility.Collapsed;
            viewModel.ViewAllVisibility = Visibility.Visible;
            iv.SelectionChanged -= iv_SelectionChanged;
            iv.DeselectAll();
            foreach (var item in viewModel.SelectedFileItems)
            {
                var index = viewModel.FileItems.IndexOf(item);
                iv.Select(index);
            }
            iv.SelectionChanged += iv_SelectionChanged;
        }

        private void chk_list_Checked(object sender, RoutedEventArgs e)
        {
            if (viewModel is null)
            {
                return;
            }
            viewModel.ViewAllVisibility = Visibility.Collapsed;
            viewModel.ListVisibility = Visibility.Visible;
            lv.SelectionChanged -= lv_SelectionChanged;
            lv.SelectedItems.Clear();
            foreach (var item in viewModel.SelectedFileItems)
            {
                lv.SelectedItems.Add(item);
            }
            lv.SelectionChanged += lv_SelectionChanged;
        }

        private void iv_SelectionChanged(ItemsView sender, ItemsViewSelectionChangedEventArgs e)
        {
            if (viewModel is null || viewModel.FileItems.Count == 0 || _user is null)
            {
                return;
            }
            Debug.WriteLine($"===>iv_SelectionChanged");
            viewModel.SelectedFileItems.Clear();
            foreach (var f in iv.SelectedItems)
            {
                var _f = (MyFileItemModel)f;
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
