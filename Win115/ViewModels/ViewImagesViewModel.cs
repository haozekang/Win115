using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.Collections;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Tanovo.ExtensionMethods;
using Win115.Models;

namespace Win115.ViewModels
{
    public partial class ViewImagesViewModel : ObservableRecipient
    {
        private readonly MyFilesViewModel _myFilesViewModel;

        [ObservableProperty]
        public partial UserInfoModel User { get; set; }

        [ObservableProperty]
        public partial MyFileItemModel? SelectedImageItem { get; set; } = null;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ImageLoadingVisibility))]
        [NotifyPropertyChangedFor(nameof(ImageLoadedVisibility))]
        public partial bool IsImageLoading { get; set; } = true;

        public Visibility ImageLoadingVisibility => IsImageLoading ? Visibility.Visible : Visibility.Collapsed;

        public Visibility ImageLoadedVisibility => IsImageLoading ? Visibility.Collapsed : Visibility.Visible;

        [ObservableProperty]
        public partial IncrementalLoadingCollection<MyFileImageIncrementalSource, MyFileItemModel> ImageFileItems { get; set; }

        public ViewImagesViewModel(UserInfoModel user, MyFilesViewModel myFilesViewModel)
        {
            User = user;
            _myFilesViewModel = myFilesViewModel;
            ImageFileItems = myFilesViewModel.ImageFileItems;
            if (ImageFileItems.Count == 0)
            {
                _ = ImageFileItems.LoadMoreItemsAsync(30);
            }
        }
    }
}
