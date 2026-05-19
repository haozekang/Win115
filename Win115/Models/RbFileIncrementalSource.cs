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
    public class RbFileIncrementalSource : IIncrementalSource<RbFileItemModel>
    {
        private readonly List<RbFileItemModel> items = new();

        public async Task<IEnumerable<RbFileItemModel>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
        {
            var user = App.Resolve<UserInfoModel>();
            var vm = App.Resolve<BackStationViewModel>();
            if (user.IsLogin != true || vm is null)
            {
                return Enumerable.Empty<RbFileItemModel>();
            }
            bool has = false;
            try
            {
                var req = new RestRequest(ApiResource.OpenRbList);
                req.AddQueryParameter("limit", pageSize);
                req.AddQueryParameter("offset", pageIndex * pageSize);

                var res = await App.ProApiClient.GetAsync(req);
                if (!res.IsSuccessful || res.Content.IsBlank())
                {
                    return Enumerable.Empty<RbFileItemModel>();
                }
                var dto = JsonConvert.DeserializeObject<ProResponseDTO<OpenRbListDTO>>(res.Content);
                if (dto is null)
                {
                    App.DispatcherQueue?.TryEnqueue(async () =>
                    {
                        await App.ShowMessageBar("序列化失败！", "错误", InfoBarSeverity.Error);
                    });
                    return Enumerable.Empty<RbFileItemModel>();
                }
                if (dto.State != true)
                {
                    App.DispatcherQueue?.TryEnqueue(async () =>
                    {
                        await App.ShowMessageBar(dto.Message ?? "未知错误", "错误", InfoBarSeverity.Error, autoClose: TimeSpan.FromSeconds(5));
                    });
                    return Enumerable.Empty<RbFileItemModel>();
                }
                if (dto.Data is null)
                {
                    return Enumerable.Empty<RbFileItemModel>();
                }
                has = dto.Data.Items.Count > 0;
                return dto.Data.Items.Select(f => new RbFileItemModel
                {
                    ParentId = f.Value.ParentId,
                    Id = f.Value.Id,
                    FileName = f.Value.FileName,
                    Type = f.Value.Type.ToInteger(),
                    FileSize = f.Value.FileSize.ToLong(),
                    DeleteTime = f.Value.DeleteTime.ToLong().TimeStampToDateTime(),
                    ParentName = f.Value.ParentName,
                    Status = f.Value.Status,
                    ThumbUrl = f.Value.ThumbUrl,
                    PickCode = f.Value.PickCode,
                });
            }
            finally
            {
                vm.CanClearAll = items.Count > 0 || has || vm.FileItems.Count > 0;
            }
        }
    }
}
