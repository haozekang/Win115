using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteDB;
using System;
using System.Threading.Tasks;
using Tanovo.ExtensionMethods;
using Win115.Entities;
using Win115.Models;
using Win115.Properties;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Win115.ViewModels
{
    public partial class SettingsViewModel : ObservableRecipient
    {
        private LiteDatabase _db;

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

        public async Task Save()
        {
        }
    }
}
