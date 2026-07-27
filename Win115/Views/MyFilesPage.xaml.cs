using Autofac;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Tanovo.ExtensionMethods;
using Win115.Models;
using Win115.ViewModels;
using WinUIEx;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Win115.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MyFilesPage : Page
    {
        private MyFilesViewModel? viewModel;
        private UserInfoModel? _user;

        public MyFilesPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            viewModel = App.Resolve<MyFilesViewModel>();
            _user = App.Resolve<UserInfoModel>();
            App.UpdatePathBar();
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
            MyFileItemModel? item = null;
            if (e.OriginalSource is FrameworkElement element)
            {
                if (element.DataContext is MyFileItemModel)
                {
                    item = (MyFileItemModel)element.DataContext;
                }
                else if (sender is ItemContainer ic && ic.Tag is string id)
                {
                    item = viewModel.FileItems.FirstOrDefault(x => x.Id == id);
                }
            }
            if (item == null && e.OriginalSource is MenuFlyoutItem menu)
            {
                if (menu.CommandParameter is MyFileItemModel)
                {
                    item = (MyFileItemModel)menu.CommandParameter;
                }
            }

            if (item is not null)
            {
                // 文件夹，执行进入文件夹命令
                if (item.FileType == "0")
                {
                    await viewModel.EnterFolderCommand.ExecuteAsync(item);
                }
                else if (item.FileType == "1")
                {
                    if (item.IsImage)
                    {
                        MyFileItemModel? selectedItem = null;
                        using var scope = App.CreateScope();
                        var vm = scope.Resolve<ViewImagesViewModel>();
                        ViewImagesWindow vw = new ViewImagesWindow(vm);
                        for (; vm.ImageFileItems.HasMoreItems;)
                        {
                            await vm.ImageFileItems.LoadMoreItemsAsync(100);
                            selectedItem = vm.SelectedImageItem = vm.ImageFileItems.FirstOrDefault(x => x.Id == item.Id);
                            if (selectedItem is not null)
                            {
                                break;
                            }
                        }
                        if (selectedItem is null)
                        {
                            selectedItem = vm.ImageFileItems.FirstOrDefault(x => x.Id == item.Id);
                        }
                        vw.Show();
                        vw.SetSelectedItem(selectedItem);
                    }
                    else if (item.IsMedia)
                    {
                        MyFileItemModel? selectedItem = null;
                        using var scope = App.CreateScope();
                        var vm = scope.Resolve<ViewMediasViewModel>();
                        ViewMediasWindow vw = new ViewMediasWindow(vm);
                        for (; vm.MediaFileItems.HasMoreItems;)
                        {
                            await vm.MediaFileItems.LoadMoreItemsAsync(100);
                            selectedItem = vm.SelectedMediaItem = vm.MediaFileItems.FirstOrDefault(x => x.Id == item.Id);
                            if (selectedItem is not null)
                            {
                                break;
                            }
                        }
                        if (selectedItem is null)
                        {
                            selectedItem = vm.MediaFileItems.FirstOrDefault(x => x.Id == item.Id);
                        }
                        vw.Show();
                        vw.SetSelectedItem(selectedItem);
                    }
                }
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
            lv.SelectedItems.Clear();
            iv.SelectedItems.Clear();
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
            iv.SelectedItems.Clear();
            foreach (var item in viewModel.SelectedFileItems)
            {
                iv.SelectedItems.Add(item);
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

        private void iv_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not GridView || viewModel is null|| viewModel.FileItems.Count == 0 || _user is null)
            {
                return;
            }
            Debug.WriteLine($"===>iv_SelectionChanged");
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

        private async void path_Click(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs e)
        {
            if (viewModel is null || viewModel.PathItems.Count == 1)
            {
                return;
            }
            if (e.Item is not SelectOptionItem item)
            {
                return;
            }
            if (item.Id == viewModel.PathItems.Last().Id)
            {
                return;
            }
            Debug.WriteLine($"===>Index:{e.Index}  Name:{item.DisplayName}");
            var count = viewModel.PathItems.Count;
            var rmCount = viewModel.PathItems.Count - 1 - e.Index;
            while (rmCount > 0)
            {
                viewModel.PathItems.Remove(viewModel.PathItems.Last());
                rmCount--;
            }
            await viewModel.RefreshFilesCommand.ExecuteAsync(null);
        }

        private void sort_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is not UIElement ui)
            {
                return;
            }
            var type = typeof(UIElement);
            var cursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
            type.InvokeMember("ProtectedCursor", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty | BindingFlags.Instance, null, ui, new object[] { cursor });
        }

        private async void sort_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (viewModel is null)
            {
                return;
            }
            if (sender is not FrameworkElement grid)
            {
                return;
            }
            var type = grid.Tag as string;
            if (type.IsBlank())
            {
                return;
            }
            viewModel.SortNameUpVisibility = Visibility.Collapsed;
            viewModel.SortNameDownVisibility = Visibility.Collapsed;
            viewModel.SortSizeUpVisibility = Visibility.Collapsed;
            viewModel.SortSizeDownVisibility = Visibility.Collapsed;
            viewModel.SortFileTypeUpVisibility = Visibility.Collapsed;
            viewModel.SortFileTypeDownVisibility = Visibility.Collapsed;
            viewModel.SortCreateTimeUpVisibility = Visibility.Collapsed;
            viewModel.SortCreateTimeDownVisibility = Visibility.Collapsed;
            viewModel.SortUpdateTimeUpVisibility = Visibility.Collapsed;
            viewModel.SortUpdateTimeDownVisibility = Visibility.Collapsed;
            // 1：升序   0：降序
            switch (type)
            {
                case "name":
                    if (viewModel.SortField == "file_name")
                    {
                        if (viewModel.SortDirection == "1")
                        {
                            viewModel.SortDirection = "0";
                            viewModel.SortNameDownVisibility = Visibility.Visible;
                        }
                        else
                        {
                            viewModel.SortDirection = "1";
                            viewModel.SortNameUpVisibility = Visibility.Visible;
                        }
                    }
                    else
                    {
                        viewModel.SortDirection = "1";
                        viewModel.SortField = "file_name";
                        viewModel.SortNameUpVisibility = Visibility.Visible;
                    }
                    break;
                case "size":
                    if (viewModel.SortField == "file_size")
                    {
                        if (viewModel.SortDirection == "1")
                        {
                            viewModel.SortDirection = "0";
                            viewModel.SortSizeDownVisibility = Visibility.Visible;
                        }
                        else
                        {
                            viewModel.SortDirection = "1";
                            viewModel.SortSizeUpVisibility = Visibility.Visible;
                        }
                    }
                    else
                    {
                        viewModel.SortDirection = "1";
                        viewModel.SortField = "file_size";
                        viewModel.SortSizeUpVisibility = Visibility.Visible;
                    }
                    break;
                case "fileType":
                    if (viewModel.SortField == "file_type")
                    {
                        if (viewModel.SortDirection == "1")
                        {
                            viewModel.SortDirection = "0";
                            viewModel.SortFileTypeDownVisibility = Visibility.Visible;
                        }
                        else
                        {
                            viewModel.SortDirection = "1";
                            viewModel.SortFileTypeUpVisibility = Visibility.Visible;
                        }
                    }
                    else
                    {
                        viewModel.SortDirection = "1";
                        viewModel.SortField = "file_type";
                        viewModel.SortFileTypeUpVisibility = Visibility.Visible;
                    }
                    break;
                case "updateTime":
                    if (viewModel.SortField == "user_utime")
                    {
                        if (viewModel.SortDirection == "1")
                        {
                            viewModel.SortDirection = "0";
                            viewModel.SortUpdateTimeDownVisibility = Visibility.Visible;
                        }
                        else
                        {
                            viewModel.SortDirection = "1";
                            viewModel.SortUpdateTimeUpVisibility = Visibility.Visible;
                        }
                    }
                    else
                    {
                        viewModel.SortDirection = "1";
                        viewModel.SortField = "user_utime";
                        viewModel.SortUpdateTimeUpVisibility = Visibility.Visible;
                    }
                    break;
            }
            await viewModel.RefreshFilesCommand.ExecuteAsync(null);
        }

        private void item_menu_Opening(object sender, object e)
        {
            MenuFlyout? flyout = null;
            ContentControl? element = null;
            MyFileItemModel? file = null;
            if (sender is MenuFlyout)
            {
                flyout = sender as MenuFlyout;
            }
            if (flyout is not null && flyout.Target is ContentControl)
            {
                element = flyout?.Target as ContentControl;
            }
            if (flyout is null || element is null)
            {
                return;
            }
            if (element is not null && element.Content is MyFileItemModel)
            {
                file = element.Content as MyFileItemModel;
                if (lv.SelectedItems.Count <= 1)
                {
                    lv.SelectedItems.Clear();
                    lv.SelectedItems.Add(element.Content);
                }
                if (iv.SelectedItems.Count <= 1)
                {
                    iv.SelectedItems.Clear();
                    iv.SelectedItems.Add(element.Content);
                }
            }
            if (file is null)
            {
                return;
            }
            foreach (var menu in flyout.Items)
            {
                if (menu is MenuFlyoutItem item)
                {
                    item.Visibility = Visibility.Visible;
                    if (viewModel?.IsListView == true && lv.SelectedItems.Count > 1)
                    {
                        if ((item.Tag as string) == "detail")
                        {
                            item.Visibility = Visibility.Collapsed;
                        }
                    }
                    if (viewModel?.IsViewAllView == true && iv.SelectedItems.Count > 1)
                    {
                        if ((item.Tag as string) == "detail")
                        {
                            item.Visibility = Visibility.Collapsed;
                        }
                    }
                    if (file.FileType == "1")
                    {
                        if ((item.Tag as string) == "open")
                        {
                            if (!file.IsImage)
                            {
                                item.Visibility = Visibility.Collapsed;
                            }
                        }
                    }
                    item.CommandParameter = element?.Content;
                }
            }
        }

        internal void UpdatePathBar(List<SelectOptionItem> paths)
        {
            path_bar.ItemsSource = paths;
        }

        internal void SelectedItemAndScrollIntoView(int index, MyFileItemModel item)
        {
            if (index < 0 || item is null || viewModel is null)
            {
                return;
            }
            if (viewModel.ListVisibility == Visibility.Visible)
            {
                lv.SelectedItem = item;
                lv.ScrollIntoView(item);
            }
            else if (viewModel.ViewAllVisibility == Visibility.Visible)
            {
                iv.SelectedItem = item;
                iv.ScrollIntoView(item);
            }
        }

        private async void lv_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint((UIElement)sender);
            if (viewModel is null)
            {
                return;
            }
            if (point.Properties.IsXButton1Pressed)
            {
                await viewModel.ParentDirectoryCommand.ExecuteAsync(null);
            }
        }

        private void FilesGrid_DragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
                e.DragUIOverride.Caption = "上传文件或文件夹";
            }
            else
            {
                e.AcceptedOperation = DataPackageOperation.None;
            }
        }

        private void FilesGrid_DragEnter(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                dragDropOverlay.Visibility = Visibility.Visible;
            }
        }

        private void FilesGrid_DragLeave(object sender, DragEventArgs e)
        {
            dragDropOverlay.Visibility = Visibility.Collapsed;
        }

        private async void FilesGrid_Drop(object sender, DragEventArgs e)
        {
            dragDropOverlay.Visibility = Visibility.Collapsed;
            if (viewModel is null || !e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                return;
            }

            var items = await e.DataView.GetStorageItemsAsync();
            var filePaths = items.OfType<StorageFile>().Select(file => file.Path).ToList();
            var folderPaths = items.OfType<StorageFolder>().Select(folder => folder.Path).ToList();
            if (filePaths.Count > 0 || folderPaths.Count > 0)
            {
                await viewModel.UploadLocalItems(filePaths, folderPaths);
            }
        }

    }
}
