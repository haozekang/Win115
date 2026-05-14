using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Downloader;
using LiteDB;
using Microsoft.UI.Dispatching;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml.XPath;
using Tanovo.ExtensionMethods;
using Win115.Dtos;
using Win115.Entities;
using Win115.Enums;
using Win115.Handlers;
using Win115.Helpers;
using Win115.Models;
using Win115.Properties;
using static QRCoder.PayloadGenerator;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Win115.ViewModels
{
    public partial class DownloadListViewModel : ObservableRecipient
    {
        private LiteDatabase _db;
        private Channel<DownloadItemModel> DownloadQueue = Channel.CreateUnbounded<DownloadItemModel>();

        [ObservableProperty]
        public partial UserInfoModel User { get; set; }

        [ObservableProperty]
        public partial ObservableCollection<DownloadItemModel> DownloadItems { get; set; }

        public DownloadListViewModel(UserInfoModel user, LiteDatabase db)
        {
            User = user;
            _db = db;
            DownloadItems = new();

            Task.Factory.StartNew(ReadTask);
            Task.Factory.StartNew(CheckTaskState);
        }

        private async Task ReadTask()
        {
            Debug.WriteLine($"===>Download task reader start!");
            await foreach (var item in DownloadQueue.Reader.ReadAllAsync())
            {
                App.DispatcherQueue?.TryEnqueue(() =>
                {
                    DownloadItems.Add(item);
                });
            }
            Debug.WriteLine($"===>Download task reader close!");
        }

        private async Task CheckTaskState()
        {
            Debug.WriteLine($"===>Download task checker start!");
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                var downloadingTasks = DownloadItems.Where(t => t.State == DownloadTaskStateEnum.Downloading);
                if (downloadingTasks.Count() >= 5)
                {
                    continue;
                }
                var newCount = 5 - downloadingTasks.Count();
                var newStartTasks = DownloadItems.Where(t => t.State == DownloadTaskStateEnum.Queued).Take(newCount);
                foreach (var task in newStartTasks)
                {
                    _ = DownloadFileAsync(task);
                }
            }
        }

        private async Task DownloadFileAsync(DownloadItemModel task)
        {
            App.DispatcherQueue?.TryEnqueue(() =>
            {
                task.State = DownloadTaskStateEnum.Downloading;
            });
            var uri = new Uri(task.Url!);
            var baseUri = new Uri($"{uri.Scheme}://{uri.Authority}");
            var handler = new TokenRefreshHandler();
            handler.InnerHandler = new HttpClientHandler();
            using var client = new HttpClient(handler);
            client.BaseAddress = baseUri;
            var headReq = new HttpRequestMessage(HttpMethod.Head, uri.PathAndQuery);
            var headRes = await client.SendAsync(headReq);

            var downReq = new HttpRequestMessage(HttpMethod.Get, uri.PathAndQuery);
            try
            {
                using var downRes = await client.SendAsync(downReq, HttpCompletionOption.ResponseHeadersRead);
                downRes.EnsureSuccessStatusCode();
                double? totalBytes = downRes.Content.Headers.ContentLength;
                await using Stream input = await downRes.Content.ReadAsStreamAsync();
                await using FileStream output = new FileStream(
                    task.SavePath!,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 1024,
                    useAsync: true);
                byte[] buffer = new byte[81920];
                long totalRead = 0;
                int bytesRead;
                // 异步复制到文件
                // await input.CopyToAsync(output);
                Stopwatch stopwatch = Stopwatch.StartNew();
                long bytesSinceLastReport = 0;
                long lastReportMilliseconds = 0;

                while ((bytesRead = await input.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
                {
                    // 写入文件
                    await output.WriteAsync(buffer.AsMemory(0, bytesRead));
                    await output.FlushAsync();

                    // 更新统计
                    totalRead += bytesRead;
                    bytesSinceLastReport += bytesRead;

                    // 每 500ms 报一次进度
                    long elapsedMs = stopwatch.ElapsedMilliseconds;
                    if (elapsedMs - lastReportMilliseconds >= 500)
                    {
                        double intervalSeconds =
                            (elapsedMs - lastReportMilliseconds) / 1000.0;

                        double speed = bytesSinceLastReport / intervalSeconds; // B/s

                        App.DispatcherQueue?.TryEnqueue(() =>
                        {
                            if (totalBytes != 0)
                            {
                                task.Progress = $"{(totalRead / totalBytes):P}";
                            }
                            task.Speed = (long)speed;
                        });

                        bytesSinceLastReport = 0;
                        lastReportMilliseconds = elapsedMs;
                    }
                }

                // 最后再报告一次
                double averageSpeed = totalRead / Math.Max(1, stopwatch.Elapsed.TotalSeconds);
                App.DispatcherQueue?.TryEnqueue(() =>
                {
                    if (totalBytes != 0)
                    {
                        task.Progress = $"{(totalRead / totalBytes):P}";
                    }
                    task.Speed = (long)averageSpeed;
                });
                // 确保写入磁盘（可选）
                await output.FlushAsync();
                var col = _db.GetCollection<DownloadTaskEntity>(CollectionResource.DownloadTask);
                var find = col.Query().Where(x => x.Id == task.TaskId).SingleOrDefault();
                if (find is not null)
                {
                    find.State = DownloadTaskStateEnum.Completed;
                    col.Update(find);
                }
                App.DispatcherQueue?.TryEnqueue(() =>
                {
                    task.State = DownloadTaskStateEnum.Completed;
                });
            }
            catch (Exception ex)
            {
                App.DispatcherQueue?.TryEnqueue(() =>
                {
                    task.State = DownloadTaskStateEnum.Failed;
                });
                Debug.WriteLine(ex);
            }
        }

        /// <summary>
        /// 添加下载任务
        /// </summary>
        /// <param name="pk">文件唯一提取码</param>
        /// <param name="fileName">文件名称</param>
        /// <param name="fileSize">文件大小</param>
        /// <param name="saveDirPath">文件下载路径（为空时取默认下载目录）</param>
        /// <param name="start">立即启动下载（默认true）</param>
        /// <param name="progress">当前进度（默认0）</param>
        /// <returns></returns>
        public async Task AddTask(string pk, string fileName, long? fileSize, string saveDirPath = "", bool start = true, double progress = 0)
        {
            if (pk.IsBlank())
            {
                return;
            }
            try
            {
                var req = new RestRequest(ApiResource.OpenUfileDownurl);
                req.AddOrUpdateParameter("pick_code", pk);
                req.AlwaysMultipartFormData = true;
                var res = await App.ProApiClient.PostAsync(req);
                if (!res.IsSuccessful || res.Content.IsBlank())
                {
                    return;
                }
                var dto = JsonConvert.DeserializeObject<ProResponseDTO<Dictionary<string, OpenUfileDownurlDTO?>?>>(res.Content);
                if (dto is null || !dto.State || dto.Data is null || dto.Data.Count == 0)
                {
                    return;
                }
                // 下载记录
                var col = _db.GetCollection<DownloadTaskEntity>(CollectionResource.DownloadTask);
                long id = col.Insert(new DownloadTaskEntity
                {
                    Name = fileName,
                    Progress = $"{progress:P}",
                    State = start ? DownloadTaskStateEnum.Queued : DownloadTaskStateEnum.Paused,
                    SavePath = Path.Combine(saveDirPath, fileName),
                    Size = fileSize,
                    PickCode = pk,
                    Url = dto.Data.First().Value?.Url?.Url,
                    UserId = User.UserId
                });

                await DownloadQueue.Writer.WriteAsync(new DownloadItemModel
                {
                    TaskId = id,
                    Name = fileName,
                    Progress = $"{progress:P}",
                    State = start ? DownloadTaskStateEnum.Queued : DownloadTaskStateEnum.Paused,
                    SavePath = Path.Combine(saveDirPath, fileName),
                    Size = fileSize,
                    PickCode = pk,
                    Url = dto.Data.First().Value?.Url?.Url
                });
            }
            catch (Exception ex)
            {
                await LogHelper.Error(ex);
            }
        }

        [RelayCommand]
        public async Task ClearFinish()
        {
            if (DownloadItems.IsBlank())
            {
                return;
            }
            var pks = DownloadItems.Where(x => x.State == DownloadTaskStateEnum.Completed).Select(x => x.PickCode).ToArray();
            foreach (var pk in pks)
            {
                var item = DownloadItems.FirstOrDefault(x => x.PickCode == pk);
                if (item is null)
                {
                    continue;
                }
                DownloadItems.Remove(item);
            }
        }

        [RelayCommand]
        public async Task PauseAll()
        {
        }

        [RelayCommand]
        public async Task StartAll()
        {
        }
    }
}
