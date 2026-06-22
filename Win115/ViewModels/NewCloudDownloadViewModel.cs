using CommunityToolkit.Mvvm.ComponentModel;
using RestSharp;
using System;
using System.Threading.Tasks;
using Tanovo.ExtensionMethods;
using Win115.Dtos;
using Win115.Models;
using Win115.Properties;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Win115.ViewModels
{
    public partial class NewCloudDownloadViewModel : ObservableRecipient
    {
        [ObservableProperty]
        public partial UserInfoModel User { get; set; }

        [ObservableProperty]
        public partial string Urls { get; set; } = string.Empty;

        [ObservableProperty]
        public partial long? Surplus { get; set; } = 0;

        [ObservableProperty]
        public partial long? Count { get; set; } = 0;

        [ObservableProperty]
        public partial string? SavePath { get; set; } = "根目录";

        [ObservableProperty]
        public partial long? SavePathId { get; set; } = 0;

        public NewCloudDownloadViewModel(UserInfoModel user)
        {
            User = user;

            Task.Factory.StartNew(async () =>
            {
                var req = new RestRequest(ApiResource.OpenOfflineGetQuotaInfo);
                var res = await App.ProApiClient.GetAsync(req);
                if (!res.IsSuccessful || res.Content.IsBlank())
                {
                    return;
                }
                var dto = JsonSerializer.Deserialize<ProResponseDTO>(res.Content);
                if (dto is null)
                {
                    return;
                }
                if (!dto.State)
                {
                    return;
                }
                var dto2 = JsonSerializer.Deserialize<ProResponseDTO<OpenOfflineGetQuotaInfo>>(res.Content);
                if (dto2 is null || dto2.Data is null)
                {
                    return;
                }
                App.DispatcherQueue!.TryEnqueue(() => 
                {
                    Surplus = dto2.Data.Surplus ?? 0;
                    Count = dto2.Data.Count ?? 0;
                });
            });
        }
    }
}
