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

namespace Win115.Models
{
    public class MyFileIncrementalSource : IIncrementalSource<MyFileItemModel>
    {
        private long _pathId;
        private string _sortDirection;
        private string _sortField;
        private readonly List<MyFileItemModel> items = new();

        public MyFileIncrementalSource(long pathId, string sortDirection, string sortField)
        {
            _pathId = pathId;
            _sortDirection = sortDirection;
            _sortField = sortField;
        }

        public async Task<IEnumerable<MyFileItemModel>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
        {
            var user = App.Resolve<UserInfoModel>();
            if (user.IsLogin != true)
            {
                return Enumerable.Empty<MyFileItemModel>();
            }
            var req = new RestRequest(ApiResource.OpenUfileFiles);
            if (_pathId != -1)
            {
                req.AddQueryParameter("cid", _pathId);
            }
            req.AddQueryParameter("limit", pageSize);
            req.AddQueryParameter("offset", pageIndex * pageSize);
            req.AddQueryParameter("asc", _sortDirection);
            req.AddQueryParameter("o", _sortField);
            req.AddQueryParameter("custom_order", "1");
            req.AddQueryParameter("cur", 1);
            req.AddQueryParameter("show_dir", 1);

            var res = await App.ProApiClient.GetAsync(req);
            if (!res.IsSuccessful || res.Content.IsBlank())
            {
                return Enumerable.Empty<MyFileItemModel>();
            }
            var dto = JsonConvert.DeserializeObject<OpenUfileFilesDTO>(res.Content);
            if (dto is null)
            {
                App.DispatcherQueue?.TryEnqueue(async() => 
                {
                    await App.ShowMessageBar("序列化失败！", "错误", InfoBarSeverity.Error);
                });
                return Enumerable.Empty<MyFileItemModel>();
            }
            if (dto.State != true)
            {
                App.DispatcherQueue?.TryEnqueue(async () =>
                {
                    await App.ShowMessageBar(dto.Message ?? "未知错误", "错误", InfoBarSeverity.Error, autoClose: TimeSpan.FromSeconds(5));
                });
                return Enumerable.Empty<MyFileItemModel>();
            }
            if (dto.Data is null)
            {
                return Enumerable.Empty<MyFileItemModel>();
            }
            return dto.Data.Select(f => new MyFileItemModel
            {
                ParentId = f.PId,
                Id = f.FId,
                Name = f.FN,
                FileType = f.FC,
                FileCover = f.FCO,
                FileExtension = f.ICO,
                FileState = f.FTA,
                Sha1 = f.Sha1,
                IsVideo = f.IsV,
                IsEncrypted = f.IsP,
                Size = f.FS,
                CreateTime = f.UppT?.TimeStampToDateTime(),
                UpdateTime = f.UpT?.TimeStampToDateTime(),
                PickCode = f.PC,
                FileDesc = f.FDesc,
                AudioLength = f.FATR,
                VideoResolution = f.Def,
                VideoResolution2 = f.Def2,
                PlayLong = f.PlayLong,
                VideoUrl = f.VImg,
                ThumbUrl = f.Thumb,
                OriginalUrl = f.UO,
            });
        }
    }
}
