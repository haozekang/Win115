using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.WinUI;
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

namespace Win115.ViewModels
{
    public partial class DownloadListViewModel : ObservableRecipient
    {
        private SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
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

            Messenger.Register<ObservableRecipient, ValueChangedMessage<WeakMessengerTypes>, string>(this, nameof(MainViewModel), (r, msgType) =>
            {
                switch (msgType.Value)
                {
                    case WeakMessengerTypes.SignOut:
                        ClearData();
                        break;
                }
            });
        }

        /// <summary>
        /// 登出后，清理
        /// </summary>
        public async void ClearData()
        {
            try
            {
                var col = _db.GetCollection<DownloadTaskEntity>(CollectionResource.DownloadTask);
                // 处理尚在队列，未读取到的任务
                var downloads = DownloadQueue.Reader.ReadAllAsync();
                await foreach (var down in downloads)
                {
                    var find = col.Query().Where(x => x.Id == down.TaskId).Single();
                    if (find is null)
                    {
                        continue;
                    }
                    find.State = DownloadTaskStateEnum.Queued;
                    find.Size = down.Size;
                    find.Progress = down.Progress;
                    find.SavePath = down.SavePath;
                    find.Url = down.Url;
                    find.PickCode = down.PickCode;
                    col.Update(find);
                }
                // 处理正在处理的任务
                foreach (var down in DownloadItems)
                {
                    var find = col.Query().Where(x => x.Id == down.TaskId).Single();
                    if (find is null)
                    {
                        continue;
                    }
                    if (down.State == DownloadTaskStateEnum.Downloading)
                    {
                        find.State = DownloadTaskStateEnum.Paused;
                    }
                    find.Size = down.Size;
                    find.Progress = down.Progress;
                    find.SavePath = down.SavePath;
                    find.Url = down.Url;
                    find.PickCode = down.PickCode;
                    col.Update(find);
                }
                DownloadItems.Clear();
            }
            finally
            {
            }
        }

        private async Task ReadTask()
        {
            Debug.WriteLine($"===>Download task reader start!");
            try
            {
                await foreach (var item in DownloadQueue.Reader.ReadAllAsync())
                {
                    App.DispatcherQueue?.TryEnqueue(() =>
                    {
                        DownloadItems.Insert(0, item);
                    });
                }
            }
            finally
            {
            }
            Debug.WriteLine($"===>Download task reader close!");
        }

        private async Task CheckTaskState()
        {
            Debug.WriteLine($"===>Download task checker start!");
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                try
                {
                    await _semaphoreSlim.WaitAsync();
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
                finally
                {
                    _semaphoreSlim.Release();
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
            var col = _db.GetCollection<DownloadTaskEntity>(CollectionResource.DownloadTask);
            var find = col.Query().Where(x => x.Id == task.TaskId).Single();
            var downReq = new HttpRequestMessage(HttpMethod.Get, uri.PathAndQuery);
            if (find is null)
            {
                App.DispatcherQueue?.TryEnqueue(() =>
                {
                    task.State = DownloadTaskStateEnum.Failed;
                });
                return;
            }
            var jump = find.DownloadedSize ?? 0;
            downReq.Headers.Range = new RangeHeaderValue(jump, null);
            try
            {
                using var downRes = await client.SendAsync(downReq, HttpCompletionOption.ResponseHeadersRead);
                downRes.EnsureSuccessStatusCode();
                long? totalBytes = downRes.Content.Headers.ContentLength + jump;
                if (jump == 0)
                {
                    find.Size = totalBytes;
                    col.Update(find);
                }
                await using Stream input = await downRes.Content.ReadAsStreamAsync();
                await using FileStream output = new FileStream(
                    task.SavePath!,
                    FileMode.OpenOrCreate,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 1024,
                    useAsync: true);
                // 移动到已经下载的位置
                output.Seek(jump, SeekOrigin.Begin);
                byte[] buffer = new byte[10 * 1024];
                long totalRead = jump;
                int bytesRead;
                // 复制到文件
                Stopwatch stopwatch = Stopwatch.StartNew();
                long bytesSinceLastReport = 0;
                long lastReportMilliseconds = 0;

                while ((bytesRead = await input.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
                {
                    // 写入文件
                    await output.WriteAsync(buffer.AsMemory(0, bytesRead));
                    await output.FlushAsync();
                    if (task.State != DownloadTaskStateEnum.Downloading)
                    {
                        break;
                    }
                    // 更新统计
                    totalRead += bytesRead;
                    bytesSinceLastReport += bytesRead;

                    // 记录下载进度
                    find.DownloadedSize = totalRead;
                    col.Update(find);

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
                                task.Progress = totalRead * 1.0 / totalBytes;
                            }
                            task.Speed = (long)speed;
                        });

                        bytesSinceLastReport = 0;
                        lastReportMilliseconds = elapsedMs;
                    }
                }

                // 最后再报告一次
                double averageSpeed = totalRead / Math.Max(1, stopwatch.Elapsed.TotalSeconds);
                await App.DispatcherQueue!.EnqueueAsync(() =>
                {
                    if (totalBytes != 0)
                    {
                        task.Progress = totalRead * 1.0/ totalBytes;
                    }
                    task.Speed = (long)averageSpeed;
                    // 防击穿
                    if (totalRead >= totalBytes)
                    {
                        task.State = DownloadTaskStateEnum.Completed;
                    }
                });
                // 确保写入磁盘（可选）
                await output.FlushAsync();
                find.DownloadedSize = totalRead;
                find.State = task.State;
                find.Progress = totalRead * 1.0/ totalBytes;
                col.Update(find);
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
                var id = col.Insert(new DownloadTaskEntity
                {
                    Name = fileName,
                    Progress = progress,
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
                    Progress = progress,
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
            var col = _db.GetCollection<DownloadTaskEntity>(CollectionResource.DownloadTask);
            var pks = DownloadItems.Where(x => x.State == DownloadTaskStateEnum.Completed).Select(x => x.PickCode).ToArray();
            foreach (var pk in pks)
            {
                var item = DownloadItems.FirstOrDefault(x => x.PickCode == pk);
                if (item is null)
                {
                    continue;
                }
                col.DeleteMany(x => x.PickCode == item.PickCode && x.Name == item.Name && x.Size == item.Size && x.SavePath == item.SavePath && x.Url == item.Url);
                DownloadItems.Remove(item);
            }
        }

        [RelayCommand]
        public async Task PauseAll()
        {
            var ids = DownloadItems.Where(x => x.State == DownloadTaskStateEnum.Downloading).Select(x => x.TaskId);
            foreach (var id in ids)
            {
                var item = DownloadItems.FirstOrDefault(x => x.TaskId == id);
                if (item is null || item.State != DownloadTaskStateEnum.Downloading)
                {
                    continue;
                }
                await App.DispatcherQueue!.EnqueueAsync(() =>
                {
                    item.State = DownloadTaskStateEnum.Paused;
                });
            }
        }

        [RelayCommand]
        public async Task StartAll()
        {
            var ids = DownloadItems.Where(x => x.State == DownloadTaskStateEnum.Paused).Select(x => x.TaskId);
            foreach (var id in ids)
            {
                var item = DownloadItems.FirstOrDefault(x => x.TaskId == id);
                if (item is null || item.State != DownloadTaskStateEnum.Paused)
                {
                    continue;
                }
                await App.DispatcherQueue!.EnqueueAsync(() =>
                {
                    item.State = DownloadTaskStateEnum.Queued;
                });
            }
        }
    }
}
