using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.Collections;
using CommunityToolkit.WinUI.Controls;
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
        public partial IncrementalLoadingCollection<MyFileImageIncrementalSource, MyFileItemModel> ImageFileItems { get; set; }

        public ViewImagesViewModel(UserInfoModel user, MyFilesViewModel myFilesViewModel)
        {
            User = user;
            _myFilesViewModel = myFilesViewModel;
            ImageFileItems = myFilesViewModel.ImageFileItems;
            _ = ImageFileItems.LoadMoreItemsAsync(1150);
        }
    }
}
