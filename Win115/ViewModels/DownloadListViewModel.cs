using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.WinUI;
using LiteDB;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Tanovo.ExtensionMethods;
using Win115.Dtos;
using Win115.Entities;
using Win115.Enums;
using Win115.Handlers;
using Win115.Helpers;
using Win115.Models;
using Win115.Properties;
using Win115.Services;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Win115.ViewModels
{
    public partial class DownloadListViewModel : ObservableRecipient
    {
        private SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        private LiteDatabase _db;
        private readonly DownloadEngine _downloadEngine;
        private readonly SystemInfoModel _system;
        private bool _isQueuePaused;
        private Channel<DownloadItemModel> DownloadQueue = Channel.CreateUnbounded<DownloadItemModel>();

        [ObservableProperty]
        public partial UserInfoModel User { get; set; }

        [ObservableProperty]
        public partial ObservableCollection<DownloadItemModel> DownloadItems { get; set; }

        public DownloadListViewModel(UserInfoModel user, SystemInfoModel system, LiteDatabase db, DownloadEngine downloadEngine)
        {
            User = user;
            _db = db;
            _downloadEngine = downloadEngine;
            _system = system;
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
                    await App.DispatcherQueue!.EnqueueAsync(UpdateFolderAggregates);
                    await _semaphoreSlim.WaitAsync();
                    if (_isQueuePaused)
                    {
                        continue;
                    }
                    var downloadingTasks = DownloadItems.Where(t => !t.IsFolder && t.State == DownloadTaskStateEnum.Downloading);
                    var concurrentTasks = Math.Clamp(_system.DownloadConcurrentTasks, 1, DownloadSettings.MaxConcurrentTasks);
                    if (downloadingTasks.Count() >= concurrentTasks)
                    {
                        continue;
                    }
                    var newCount = concurrentTasks - downloadingTasks.Count();
                    var newStartTasks = DownloadItems.Where(t => !t.IsFolder && t.State == DownloadTaskStateEnum.Queued).Take(newCount);
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
            if (task.State != DownloadTaskStateEnum.Queued)
            {
                return;
            }

            if (App.DispatcherQueue is not null)
            {
                await App.DispatcherQueue.EnqueueAsync(() =>
                {
                    if (task.State == DownloadTaskStateEnum.Queued)
                    {
                        task.State = DownloadTaskStateEnum.Downloading;
                    }
                });
            }
            else
            {
                task.State = DownloadTaskStateEnum.Downloading;
            }
            if (task.State != DownloadTaskStateEnum.Downloading)
            {
                return;
            }
            var collection = _db.GetCollection<DownloadTaskEntity>(CollectionResource.DownloadTask);
            var entity = collection.FindById(task.TaskId);
            if (entity is null)
            {
                task.State = DownloadTaskStateEnum.Failed;
                return;
            }

            try
            {
                if (task.Url.IsBlank() || task.SavePath.IsBlank() || task.Size is null or <= 0)
                {
                    throw new InvalidOperationException("下载任务缺少地址、保存路径或文件大小。");
                }

                DownloadResult? result = null;
                for (var urlRefreshAttempt = 0; urlRefreshAttempt <= 2; urlRefreshAttempt++)
                {
                    try
                    {
                        result = await _downloadEngine.DownloadAsync(
                            new Uri(task.Url),
                            task.SavePath,
                            task.Size.Value,
                            entity.DownloadedSize ?? 0,
                            entity.Segments,
                            () => task.State == DownloadTaskStateEnum.Downloading,
                            async progress =>
                            {
                                entity.DownloadedSize = progress.DownloadedBytes;
                                entity.Progress = progress.DownloadedBytes * 1.0 / progress.TotalBytes;
                                entity.Segments = progress.Segments;
                                collection.Update(entity);
                                if (App.DispatcherQueue is not null)
                                {
                                    await App.DispatcherQueue.EnqueueAsync(() =>
                                    {
                                        task.Progress = entity.Progress;
                                        task.Speed = progress.BytesPerSecond;
                                        task.RemainingTime = progress.BytesPerSecond > 0
                                            ? TimeSpan.FromSeconds((progress.TotalBytes - progress.DownloadedBytes) / (double)progress.BytesPerSecond)
                                            : null;
                                    });
                                }
                            });
                        break;
                    }
                    catch (HttpRequestException ex) when (
                        ex.StatusCode == System.Net.HttpStatusCode.Forbidden &&
                        urlRefreshAttempt < 2 &&
                        task.State == DownloadTaskStateEnum.Downloading)
                    {
                        var refreshedUrl = await RefreshDownloadUrlAsync(task.PickCode!);
                        if (refreshedUrl.IsBlank() || string.Equals(refreshedUrl, task.Url, StringComparison.Ordinal))
                        {
                            throw;
                        }

                        task.Url = refreshedUrl;
                        entity.Url = refreshedUrl;
                        collection.Update(entity);
                        await Task.Delay(TimeSpan.FromSeconds(urlRefreshAttempt + 1));
                    }
                }

                if (result is null)
                {
                    throw new InvalidOperationException("下载地址刷新后仍无法继续下载。");
                }

                entity.DownloadedSize = result.DownloadedBytes;
                entity.Progress = result.DownloadedBytes * 1.0 / task.Size.Value;
                entity.Segments = result.Segments;
                entity.State = result.IsCompleted ? DownloadTaskStateEnum.Completed : task.State;
                collection.Update(entity);
                await App.DispatcherQueue!.EnqueueAsync(() =>
                {
                    task.Progress = entity.Progress;
                    task.Speed = 0;
                    task.RemainingTime = null;
                    if (result.IsCompleted)
                    {
                        task.State = DownloadTaskStateEnum.Completed;
                    }
                });
            }
            catch (Exception ex)
            {
                entity.State = DownloadTaskStateEnum.Failed;
                collection.Update(entity);
                App.DispatcherQueue?.TryEnqueue(() => task.State = DownloadTaskStateEnum.Failed);
                await LogHelper.Error(ex);
            }
        }

        private static async Task<string?> RefreshDownloadUrlAsync(string pickCode)
        {
            var request = new RestRequest(ApiResource.OpenUfileDownurl);
            request.AddOrUpdateParameter("pick_code", pickCode);
            request.AlwaysMultipartFormData = true;
            var response = await App.ProApiClient.PostAsync(request);
            if (!response.IsSuccessful || response.Content.IsBlank())
            {
                return null;
            }

            var dto = JsonSerializer.Deserialize<ProResponseDTO<Dictionary<string, OpenUfileDownurlDTO?>?>>(response.Content);
            return dto is { State: true, Data.Count: > 0 }
                ? dto.Data.First().Value?.Url?.Url
                : null;
        }

        private async Task DownloadFileLegacyAsync(DownloadItemModel task)
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
        public async Task AddTask(string pk, string fileName, long? fileSize, string saveDirPath = "", bool start = true, double progress = 0, int? parentTaskId = null)
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
                var dto = JsonSerializer.Deserialize<ProResponseDTO<Dictionary<string, OpenUfileDownurlDTO?>?>>(res.Content);
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
                    UserId = User.UserId,
                    ParentTaskId = parentTaskId
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
                    Url = dto.Data.First().Value?.Url?.Url,
                    ParentTaskId = parentTaskId
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
            await _semaphoreSlim.WaitAsync();
            try
            {
                _isQueuePaused = true;
                var tasks = DownloadItems
                    .Where(item => item.State is DownloadTaskStateEnum.Downloading or DownloadTaskStateEnum.Queued)
                    .ToList();
                if (tasks.Count == 0)
                {
                    _isQueuePaused = false;
                    return;
                }

                await App.DispatcherQueue!.EnqueueAsync(() =>
                {
                    foreach (var task in tasks)
                    {
                        task.Speed = 0;
                        task.State = DownloadTaskStateEnum.Paused;
                    }
                });
                SaveTaskStates(tasks, DownloadTaskStateEnum.Paused);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task<int> BeginFolderTaskAsync(string folderName, string savePath)
        {
            var collection = _db.GetCollection<DownloadTaskEntity>(CollectionResource.DownloadTask);
            var entity = new DownloadTaskEntity
            {
                UserId = User.UserId,
                Name = folderName,
                SavePath = savePath,
                IsFolder = true,
                Progress = 0,
                State = DownloadTaskStateEnum.Queued,
                CreateTime = DateTime.Now
            };
            var id = collection.Insert(entity).AsInt32;
            var item = new DownloadItemModel
            {
                TaskId = id,
                Name = folderName,
                SavePath = savePath,
                IsFolder = true,
                Progress = 0,
                State = DownloadTaskStateEnum.Queued
            };
            await App.DispatcherQueue!.EnqueueAsync(() => DownloadItems.Insert(0, item));
            return id;
        }

        public void CompleteFolderCollection(int taskId, int totalFiles, long totalSize)
        {
            var folder = DownloadItems.FirstOrDefault(item => item.TaskId == taskId);
            if (folder is not null)
            {
                folder.TotalFiles = totalFiles;
                folder.Size = totalSize;
                if (totalFiles == 0)
                {
                    folder.Progress = 1;
                    folder.State = DownloadTaskStateEnum.Completed;
                }
            }
            var collection = _db.GetCollection<DownloadTaskEntity>(CollectionResource.DownloadTask);
            var entity = collection.FindById(taskId);
            if (entity is not null)
            {
                entity.TotalFiles = totalFiles;
                entity.Size = totalSize;
                if (totalFiles == 0)
                {
                    entity.Progress = 1;
                    entity.State = DownloadTaskStateEnum.Completed;
                }
                collection.Update(entity);
            }
            UpdateFolderAggregates();
        }

        private void UpdateFolderAggregates()
        {
            var collection = _db.GetCollection<DownloadTaskEntity>(CollectionResource.DownloadTask);
            foreach (var folder in DownloadItems.Where(item => item.IsFolder).ToList())
            {
                var children = DownloadItems.Where(item => item.ParentTaskId == folder.TaskId).ToList();
                if (children.Count == 0)
                {
                    continue;
                }

                var downloaded = children.Sum(item => (long)Math.Clamp((item.Size ?? 0) * (item.Progress ?? 0), 0, item.Size ?? 0));
                folder.TotalFiles = children.Count;
                folder.Size = children.Sum(item => item.Size ?? 0);
                folder.Progress = folder.Size > 0 ? downloaded / (double)folder.Size : 0;
                folder.Speed = children.Where(item => item.State == DownloadTaskStateEnum.Downloading).Sum(item => item.Speed ?? 0);
                folder.RemainingTime = folder.Speed > 0 && folder.Size > downloaded
                    ? TimeSpan.FromSeconds((folder.Size.Value - downloaded) / (double)folder.Speed.Value)
                    : null;
                folder.State = DeriveFolderState(children);

                var entity = collection.FindById(folder.TaskId);
                if (entity is not null)
                {
                    entity.DownloadedSize = downloaded;
                    entity.Size = folder.Size;
                    entity.TotalFiles = folder.TotalFiles;
                    entity.Progress = folder.Progress;
                    entity.State = folder.State;
                    collection.Update(entity);
                }
            }
        }

        private static DownloadTaskStateEnum DeriveFolderState(IReadOnlyCollection<DownloadItemModel> children)
        {
            if (children.All(item => item.State == DownloadTaskStateEnum.Completed)) return DownloadTaskStateEnum.Completed;
            if (children.Any(item => item.State == DownloadTaskStateEnum.Downloading)) return DownloadTaskStateEnum.Downloading;
            if (children.Any(item => item.State == DownloadTaskStateEnum.Queued)) return DownloadTaskStateEnum.Queued;
            if (children.All(item => item.State == DownloadTaskStateEnum.Paused)) return DownloadTaskStateEnum.Paused;
            if (children.Any(item => item.State == DownloadTaskStateEnum.Failed)) return DownloadTaskStateEnum.Failed;
            return DownloadTaskStateEnum.Canceled;
        }

        [RelayCommand]
        public async Task StartAll()
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                var tasks = DownloadItems
                    .Where(item => item.State == DownloadTaskStateEnum.Paused)
                    .ToList();
                if (tasks.Count == 0)
                {
                    _isQueuePaused = false;
                    return;
                }

                await App.DispatcherQueue!.EnqueueAsync(() =>
                {
                    foreach (var task in tasks)
                    {
                        task.State = DownloadTaskStateEnum.Queued;
                    }
                });
                _isQueuePaused = false;
                SaveTaskStates(tasks, DownloadTaskStateEnum.Queued);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        private void SaveTaskStates(IEnumerable<DownloadItemModel> tasks, DownloadTaskStateEnum state)
        {
            var collection = _db.GetCollection<DownloadTaskEntity>(CollectionResource.DownloadTask);
            foreach (var task in tasks)
            {
                if (task.TaskId is not > 0)
                {
                    continue;
                }

                var entity = collection.FindById(task.TaskId.Value);
                if (entity is null)
                {
                    continue;
                }

                entity.State = state;
                collection.Update(entity);
            }
        }
    }
}
