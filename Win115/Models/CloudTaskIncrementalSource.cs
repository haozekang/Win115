using CommunityToolkit.WinUI.Collections;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.XPath;
using Tanovo.ExtensionMethods;
using Win115.Dtos;
using Win115.Properties;
using Win115.ViewModels;

namespace Win115.Models
{
    public class CloudTaskIncrementalSource : IIncrementalSource<CloudTaskItemModel>
    {
        private readonly List<CloudTaskItemModel> items = new();

        public async Task<IEnumerable<CloudTaskItemModel>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
        {
            var user = App.Resolve<UserInfoModel>();
            var vm = App.Resolve<CloudDownloadViewModel>();
            if (user.IsLogin != true || vm is null)
            {
                return Enumerable.Empty<CloudTaskItemModel>();
            }
            try
            {
                var req = new RestRequest(ApiResource.OpenOfflineGetTaskList);
                req.AddQueryParameter("page", pageIndex + 1);

                var res = await App.ProApiClient.GetAsync(req);
                if (!res.IsSuccessful || res.Content.IsBlank())
                {
                    return Enumerable.Empty<CloudTaskItemModel>();
                }
                var dto = JsonConvert.DeserializeObject<ProResponseDTO<OpenOfflineGetTaskListDTO>>(res.Content);
                if (dto is null)
                {
                    App.DispatcherQueue?.TryEnqueue(async () =>
                    {
                        await App.ShowMessageBar("序列化失败！", "错误", InfoBarSeverity.Error);
                    });
                    return Enumerable.Empty<CloudTaskItemModel>();
                }
                if (dto.State != true)
                {
                    App.DispatcherQueue?.TryEnqueue(async () =>
                    {
                        await App.ShowMessageBar(dto.Message ?? "未知错误", "错误", InfoBarSeverity.Error, autoClose: TimeSpan.FromSeconds(5));
                    });
                    return Enumerable.Empty<CloudTaskItemModel>();
                }
                if (dto.Data is null || dto.Data.Tasks is null)
                {
                    return Enumerable.Empty<CloudTaskItemModel>();
                }
                return dto.Data.Tasks.Select(f => new CloudTaskItemModel
                {
                    InfoHash = f.InfoHash,
                    AddTime = f.AddTime?.TimeStampToDateTime(),
                    PercentDone = f.PercentDone,
                    FileSize = f.Size,
                    Name = f.Name,
                    LastUpdate = f.LastUpdate?.TimeStampToDateTime(),
                    FileId = f.FileId,
                    DeleteFileId = f.DeleteFileId,
                    Status = f.Status,
                    Url = f.Url,
                    WpPathId = f.WpPathId,
                    Def2 = f.Def2,
                    PlayLong = f.PlayLong,
                    CanAppeal = f.CanAppeal,
                });
            }
            finally
            {
            }
        }
    }
}
