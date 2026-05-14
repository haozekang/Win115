using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteDB;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.XPath;
using Win115.Models;
using Win115.Properties;

namespace Win115.ViewModels
{
    public partial class BackStationViewModel : ObservableRecipient
    {
        private LiteDatabase _db;

        [ObservableProperty]
        public partial UserInfoModel User { get; set; }

        public BackStationViewModel(UserInfoModel user, LiteDatabase db)
        {
            User = user;
            _db = db;

            _ = RefreshFiles();
        }

        [RelayCommand]
        private async Task RefreshFiles()
        {
            var req = new RestRequest(ApiResource.OpenRbList);
            req.AddQueryParameter("limit", 50);
            req.AddQueryParameter("offset", 0);
        }

        [RelayCommand]
        private async Task RevertSelected()
        {
            var req = new RestRequest(ApiResource.OpenRbList);
            req.AddQueryParameter("limit", 50);
            req.AddQueryParameter("offset", 0);
        }

        [RelayCommand]
        private async Task ClearAll()
        {
            var req = new RestRequest(ApiResource.OpenRbList);
            req.AddQueryParameter("limit", 50);
            req.AddQueryParameter("offset", 0);
        }
    }
}
