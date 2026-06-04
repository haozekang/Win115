using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using Win115.Models;
using Win115.ViewModels;
using WinRT.Interop;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Win115.Views
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ViewImagesWindow : Window
    {
        private ViewImagesViewModel _viewModel;
        private UserInfoModel? _user;

        public ViewImagesWindow(ViewImagesViewModel viewModel)
        {
            _user = App.Resolve<UserInfoModel>();
            _viewModel = viewModel;
            ExtendsContentIntoTitleBar = true;
            InitializeComponent();

            this.SetTitleBar(titleBar);

            iv.Loaded += Iv_Loaded;
            img.ImageOpened += Img_ImageOpened;
            img.ImageFailed += Img_ImageFailed;

            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.SetBorderAndTitleBar(true, false);
            }
            appWindow.SetIcon("Assets/favicon.ico");
        }

        private void Img_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            _viewModel.IsImageLoading = false;
        }

        private void Img_ImageOpened(object sender, RoutedEventArgs e)
        {
            _viewModel.IsImageLoading = false;
            sc.ZoomTo(1f, null);
        }

        private void Iv_Loaded(object sender, RoutedEventArgs e)
        {
            iv.ScrollView.ViewChanged += ScrollView_ViewChanged;
            iv.Loaded -= Iv_Loaded;
        }

        private async void ScrollView_ViewChanged(ScrollView sender, object args)
        {
            double horizontalOffset = iv.ScrollView.HorizontalOffset;
            double scrollableWidth = iv.ScrollView.ScrollableWidth;
            if (scrollableWidth - horizontalOffset < 100)
            {
                if (_viewModel.ImageFileItems.HasMoreItems)
                {
                    await _viewModel.ImageFileItems.LoadMoreItemsAsync(30);
                }
            }
        }

        private void iv_SelectionChanged(ItemsView sender, ItemsViewSelectionChangedEventArgs args)
        {
            _viewModel.IsImageLoading = true;
            _viewModel.SelectedImageItem = sender.SelectedItem as MyFileItemModel;
        }

        private async void iv_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            var pointerPoint = e.GetCurrentPoint(sender as UIElement);
            if (pointerPoint.Properties.MouseWheelDelta > 0)
            {
                if (iv.ScrollView.HorizontalOffset <= 0)
                {
                    e.Handled = true;
                    return;
                }
                iv.ScrollView.ScrollTo(iv.ScrollView.HorizontalOffset - pointerPoint.Properties.MouseWheelDelta, iv.ScrollView.VerticalOffset);
            }
            else if (pointerPoint.Properties.MouseWheelDelta < 0)
            {
                double horizontalOffset = iv.ScrollView.HorizontalOffset;
                double scrollableWidth = iv.ScrollView.ScrollableWidth;
                if (scrollableWidth <= horizontalOffset)
                {
                    e.Handled = true;
                    return;
                }
                iv.ScrollView.ScrollTo(iv.ScrollView.HorizontalOffset - pointerPoint.Properties.MouseWheelDelta, iv.ScrollView.VerticalOffset);
            }
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
        }

        internal void SetSelectedItem(MyFileItemModel? selectedImageItem)
        {
            if (selectedImageItem is null)
            {
                _viewModel.IsImageLoading = false;
                return;
            }
            int index = _viewModel.ImageFileItems.IndexOf(selectedImageItem);
            if (index < 0)
            {
                return;
            }
            iv.Select(index);
            var offset = index * 108 - 8;
            iv.ScrollView.ScrollTo(offset, iv.ScrollView.VerticalOffset);
            iv.ScrollView.StartBringIntoView(new BringIntoViewOptions { AnimationDesired = true });
        }

        internal void SetSelectedItem(string? id)
        {
        }

        private void btn_max_Click(object sender, RoutedEventArgs e)
        {
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                if (presenter.State != OverlappedPresenterState.Maximized)
                {
                    presenter.Maximize();
                }
                else
                {
                    presenter.Restore();
                }
            }
        }

        private void btn_close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
