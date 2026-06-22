using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.Collections;
using Microsoft.UI.Xaml;
using Win115.Models;

namespace Win115.ViewModels
{
    public partial class ViewMediasViewModel : ObservableRecipient
    {
        private readonly MyFilesViewModel _myFilesViewModel;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PlayButtonVisibility))]
        [NotifyPropertyChangedFor(nameof(PauseButtonVisibility))]
        public partial bool? IsPlaying { get; set; }

        public Visibility PlayButtonVisibility => IsPlaying != true ? Visibility.Visible : Visibility.Collapsed;

        public Visibility PauseButtonVisibility => IsPlaying == true ? Visibility.Visible : Visibility.Collapsed;

        [ObservableProperty]
        public partial UserInfoModel User { get; set; }

        [ObservableProperty]
        public partial MyFileItemModel? SelectedMediaItem { get; set; } = null;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MediaLoadingVisibility))]
        [NotifyPropertyChangedFor(nameof(MediaLoadedVisibility))]
        public partial bool IsMediaLoading { get; set; } = true;

        public Visibility MediaLoadingVisibility => IsMediaLoading ? Visibility.Visible : Visibility.Collapsed;

        public Visibility MediaLoadedVisibility => IsMediaLoading ? Visibility.Collapsed : Visibility.Visible;

        [ObservableProperty]
        public partial IncrementalLoadingCollection<MyFileMediaIncrementalSource, MyFileItemModel> MediaFileItems { get; set; }

        public ViewMediasViewModel(UserInfoModel user, MyFilesViewModel myFilesViewModel)
        {
            User = user;
            _myFilesViewModel = myFilesViewModel;
            MediaFileItems = myFilesViewModel.MediaFileItems;
            if (MediaFileItems.Count == 0)
            {
                _ = MediaFileItems.LoadMoreItemsAsync(30);
            }
        }

        internal void UpdateUI(bool? flag)
        {
            if (flag == true)
            {
                IsPlaying = true;
            }
            else
            {
                IsPlaying = false;
            }
        }
    }
}
