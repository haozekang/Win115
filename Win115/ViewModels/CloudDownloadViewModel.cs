using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Collections;
using LiteDB;
using System.Threading.Tasks;
using Win115.Models;

namespace Win115.ViewModels
{
    public partial class CloudDownloadViewModel : ObservableRecipient
    {
        private LiteDatabase _db;

        [ObservableProperty]
        public partial UserInfoModel User { get; set; }

        [ObservableProperty]
        public partial IncrementalLoadingCollection<CloudTaskIncrementalSource, CloudTaskItemModel> TaskItems { get; set; }

        public CloudDownloadViewModel(UserInfoModel user, LiteDatabase db)
        {
            User = user;
            _db = db;
            TaskItems = new IncrementalLoadingCollection<CloudTaskIncrementalSource, CloudTaskItemModel>(new CloudTaskIncrementalSource(), 30);
        }

        [RelayCommand]
        private async Task RefreshTasks()
        {
            await TaskItems.RefreshAsync();
        }

        [RelayCommand]
        private async Task ItemDetail(CloudTaskItemModel item)
        {
        }
    }
}
