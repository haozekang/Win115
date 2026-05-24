using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Collections;
using LiteDB;
using Microsoft.UI.Xaml.Controls;
using System;
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

        /// <summary>
        /// 登出后，清理
        /// </summary>
        [RelayCommand]
        public async Task ClearData()
        {
            App.DispatcherQueue?.TryEnqueue(() =>
            {
                TaskItems.Clear();
            });
        }

        [RelayCommand]
        private async Task RefreshTasks()
        {
            if (User.IsLogin != true)
            {
                await App.ShowMessageBar($"请登录后再使用！", "警告", InfoBarSeverity.Warning, autoClose: TimeSpan.FromSeconds(3));
                return;
            }
            await TaskItems.RefreshAsync();
        }

        [RelayCommand]
        private async Task ItemDetail(CloudTaskItemModel item)
        {
        }
    }
}
