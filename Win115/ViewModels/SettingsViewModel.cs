using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteDB;
using System;
using System.Threading.Tasks;
using Tanovo.ExtensionMethods;
using Win115.Entities;
using Win115.Models;
using Win115.Properties;
using Win115.Services;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Win115.ViewModels
{
    public partial class SettingsViewModel : ObservableRecipient
    {
        private readonly LiteDatabase _db;

        [ObservableProperty]
        public partial UserInfoModel User { get; set; }

        [ObservableProperty]
        public partial SystemInfoModel System { get; set; }

        public SettingsViewModel(UserInfoModel user, SystemInfoModel system, LiteDatabase db)
        {
            User = user;
            System = system;
            _db = db;
        }

        [RelayCommand]
        public async Task SelectDownloadDir()
        {
            FolderPicker picker = new();
            picker.FileTypeFilter.Add("*");
            InitializeWithWindow.Initialize(picker, App.WindowHandle);
            StorageFolder? folder = await picker.PickSingleFolderAsync();

            // 用户取消
            if (folder == null || folder.Path.IsBlank())
            {
                return;
            }
            System.DownloadDirPath = folder.Path;
            var col = _db.GetCollection<SystemEntity>(CollectionResource.System);
            var find = col.Query().Where(x => x.Type == SystemConfigTypeResource.DownloadDirPath).SingleOrDefault();
            if (find is null)
            {
                col.Insert(new SystemEntity 
                {
                    Type = SystemConfigTypeResource.DownloadDirPath,
                    Value = System.DownloadDirPath
                });
            }
            else
            {
                find.Value = System.DownloadDirPath;
                col.Update(find);
            }
        }

        [RelayCommand]
        public async Task SaveApiSettings()
        {
            System.ApiRateLimit = Math.Clamp(System.ApiRateLimit, 0, ApiSettings.MaxRateLimit);
            var collection = _db.GetCollection<SystemEntity>(CollectionResource.System);
            SaveSetting(collection, ApiSettings.RateLimitKey, System.ApiRateLimit.ToString());
            await App.ShowMessageBar("API 设置已保存", "设置");
        }

        [RelayCommand]
        public async Task SaveDownloadSettings()
        {
            System.DownloadConcurrentTasks = Math.Clamp(
                System.DownloadConcurrentTasks,
                1,
                DownloadSettings.MaxConcurrentTasks);
            System.DownloadSegmentCount = Math.Clamp(
                System.DownloadSegmentCount,
                1,
                DownloadSettings.MaxSegmentCount);
            System.DownloadSpeedLimitKbps = Math.Max(0, System.DownloadSpeedLimitKbps);

            var collection = _db.GetCollection<SystemEntity>(CollectionResource.System);
            SaveSetting(collection, DownloadSettings.ConcurrentTasksKey, System.DownloadConcurrentTasks.ToString());
            SaveSetting(collection, DownloadSettings.SegmentCountKey, System.DownloadSegmentCount.ToString());
            SaveSetting(collection, DownloadSettings.SpeedLimitKey, System.DownloadSpeedLimitKbps.ToString());
            await App.ShowMessageBar("下载设置已保存", "设置");
        }

        [RelayCommand]
        public async Task SaveUploadSettings()
        {
            System.UploadMaxRetry = Math.Clamp(System.UploadMaxRetry, 0, UploadSettings.MaxRetry);
            System.UploadConcurrentTasks = Math.Clamp(
                System.UploadConcurrentTasks,
                1,
                UploadSettings.MaxConcurrentTasks);

            var collection = _db.GetCollection<SystemEntity>(CollectionResource.System);
            SaveSetting(collection, UploadSettings.MaxRetryKey, System.UploadMaxRetry.ToString());
            SaveSetting(
                collection,
                UploadSettings.MaxConcurrentTasksKey,
                System.UploadConcurrentTasks.ToString());
            await App.ShowMessageBar("上传设置已保存", "设置");
        }

        private static void SaveSetting(ILiteCollection<SystemEntity> collection, string key, string value)
        {
            var setting = collection.Query().Where(item => item.Type == key).SingleOrDefault();
            if (setting is null)
            {
                collection.Insert(new SystemEntity { Type = key, Value = value });
                return;
            }

            setting.Value = value;
            collection.Update(setting);
        }
    }
}
