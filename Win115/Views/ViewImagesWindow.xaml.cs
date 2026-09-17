using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.ComponentModel;
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
            // ViewModel 中的文件选择器、对话框需要图片窗口自身的句柄与 XamlRoot
            _viewModel.WindowHandle = hwnd;
            _viewModel.XamlRootProvider = () => img.XamlRoot;
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
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
            // 窗口的 x:Bind 绑定在首次激活时才初始化，其中重新给 ItemsSource 赋值会清空选中项；
            // 待列表加载完成后重新同步选中项，并刷新命令的可用状态（下一张按钮在刚打开时依赖它）
            if (_viewModel.SelectedImageItem is not null)
            {
                SelectInView(_viewModel.SelectedImageItem);
            }
            _viewModel.NotifyImageCommandStateChanged();
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
            var item = sender.SelectedItem as MyFileItemModel;
            // ItemsView 重新设置 ItemsSource（窗口首次激活时 x:Bind 才会赋值）后会清空选中项，
            // 这里不能跟着把 ViewModel 中已选中的图片清掉，否则上一张/下一张会一直不可用
            if (item is null)
            {
                return;
            }
            _viewModel.IsImageLoading = true;
            // 由 ViewModel 驱动的选中项变化无需再回写
            if (!ReferenceEquals(_viewModel.SelectedImageItem, item))
            {
                _viewModel.SelectedImageItem = item;
            }
        }

        /// <summary>
        /// ViewModel 切换图片（上一张/下一张/删除后自动切换）时同步视图选中项
        /// </summary>
        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ViewImagesViewModel.SelectedImageItem))
            {
                return;
            }
            var selectedImageItem = _viewModel.SelectedImageItem;
            if (selectedImageItem is null)
            {
                // 图片被全部删除时清空视图选中项
                iv.DeselectAll();
                return;
            }
            SelectInView(selectedImageItem);
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
            // ViewModel 是选中项的唯一来源，命令（上一张/下一张等）的可用状态都依据它计算，
            // 这里必须显式回写，不能只依赖视图的选中回调
            _viewModel.SelectedImageItem = selectedImageItem;
            SelectInView(selectedImageItem);
        }

        /// <summary>
        /// 在图片列表中选中指定图片，并将其滚动到可视区域
        /// </summary>
        private void SelectInView(MyFileItemModel? selectedImageItem)
        {
            if (selectedImageItem is null || ReferenceEquals(iv.SelectedItem, selectedImageItem))
            {
                return;
            }
            int index = _viewModel.ImageFileItems.IndexOf(selectedImageItem);
            if (index < 0)
            {
                return;
            }
            iv.Select(index);
            if (iv.ScrollView is null)
            {
                return;
            }
            var offset = index * 108 - 8;
            iv.ScrollView.ScrollTo(offset, iv.ScrollView.VerticalOffset);
            iv.ScrollView.StartBringIntoView(new BringIntoViewOptions { AnimationDesired = true });
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
