using Aliyun.OSS;
using Aliyun.OSS.Common;
using Aliyun.OSS.Util;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.WinUI;
using LiteDB;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Utilities.Encoders;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Text.Json;
using Tanovo.ExtensionMethods;
using Win115.Dtos;
using Win115.Entities;
using Win115.Enums;
using Win115.Helpers;
using Win115.Models;
using Win115.Properties;
using Win115.Services;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Win115.ViewModels
{
    public partial class UploadListViewModel : ObservableRecipient
    {
        private readonly SemaphoreSlim _schedulerLock = new(1, 1);
        private readonly LiteDatabase _db;
        private readonly SystemInfoModel _system;
        private readonly HashSet<UploadItemModel> _runningTasks = new();
        private readonly Dictionary<UploadItemModel, int> _retryCounts = new();
        private readonly Channel<UploadItemModel> _uploadQueue = Channel.CreateUnbounded<UploadItemModel>();
        private readonly Dictionary<UploadItemModel, (long Bytes, DateTime Timestamp, double Speed)> _speedSnapshots = new();
        private bool _isQueuePaused;

        [ObservableProperty]
        public partial UserInfoModel User { get; set; }

        [ObservableProperty]
        public partial ObservableCollection<UploadItemModel> UploadItems { get; set; }

        public UploadListViewModel(UserInfoModel user, SystemInfoModel system, LiteDatabase db)
        {
            User = user;
            _system = system;
            _db = db;
            UploadItems = new();

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
                var col = _db.GetCollection<UploadTaskEntity>(CollectionResource.UploadTask);
                var uploads = _uploadQueue.Reader.ReadAllAsync();
                await foreach (var up in uploads)
                {
                    var find = col.Query().Where(x => x.Id == up.TaskId).Single();
                    if (find is null)
                    {
                        continue;
                    }
                    find.FileId = up.FileId;
                    find.ParentId = up.ParentId;
                    find.Size = up.Size;
                    find.Progress = up.Progress;
                    find.FilePath = up.FilePath;
                    find.Bucket = up.Bucket;
                    find.Object = up.Object;
                    find.Endpoint = up.Endpoint;
                    find.Region = up.Region;
                    find.PickCode = up.PickCode;
                    find.State = UploadTaskStateEnum.Queued;
                    col.Update(find);
                }
                foreach (var up in UploadItems)
                {
                    var find = col.Query().Where(x => x.Id == up.TaskId).Single();
                    if (find is null)
                    {
                        continue;
                    }
                    find.FileId = up.FileId;
                    find.ParentId = up.ParentId;
                    find.Size = up.Size;
                    find.Progress = up.Progress;
                    find.FilePath = up.FilePath;
                    find.Bucket = up.Bucket;
                    find.Object = up.Object;
                    find.Endpoint = up.Endpoint;
                    find.Region = up.Region;
                    find.PickCode = up.PickCode;
                    find.State = UploadTaskStateEnum.Queued;
                    col.Update(find);
                }
                UploadItems.Clear();
            }
            finally
            {
            }
        }

        private async Task ReadTask()
        {
            Debug.WriteLine($"===>Upload task reader start!");
            try
            {
                await foreach (var item in _uploadQueue.Reader.ReadAllAsync())
                {
                    App.DispatcherQueue?.EnqueueAsync(() =>
                    {
                        if (item.TaskId is null or 0)
                        {
                            item.TaskId = _db.GetCollection<UploadTaskEntity>(CollectionResource.UploadTask).Insert(new UploadTaskEntity
                            {
                                UserId = User.UserId,
                                Name = item.Name,
                                Size = item.Size,
                                ParentId = item.ParentId,
                                FilePath = item.FilePath,
                                Progress = item.Progress,
                                UploadedSize = item.UploadedSize,
                                ParentTaskId = item.ParentTaskId,
                                IsFolder = item.IsFolder,
                                TotalFiles = item.TotalFiles,
                                State = item.State,
                                CreateTime = DateTime.Now,
                                PartETagsJson = "{}"
                            }).AsInt32;
                        }
                        item.PropertyChanged += (_, _) => PersistTask(item);
                        UploadItems.Insert(0, item);
                        PersistTask(item);
                    });
                }
            }
            finally
            {
            }
            Debug.WriteLine($"===>Upload task reader close!");
        }

        private async Task CheckTaskState()
        {
            Debug.WriteLine($"===>Upload task checker start!");
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                try
                {
                    await App.DispatcherQueue!.EnqueueAsync(UpdateTransferMetrics);
                    await _schedulerLock.WaitAsync();
                    if (_isQueuePaused)
                    {
                        continue;
                    }
                    var availableSlots = Math.Clamp(
                        _system.UploadConcurrentTasks,
                        1,
                        UploadSettings.MaxConcurrentTasks) - _runningTasks.Count;
                    if (availableSlots <= 0)
                    {
                        continue;
                    }

                    var tasks = UploadItems
                        .Where(task => !task.IsFolder && task.State == UploadTaskStateEnum.Queued && !_runningTasks.Contains(task))
                        .Take(availableSlots)
                        .ToList();
                    foreach (var task in tasks)
                    {
                        _runningTasks.Add(task);
                        _ = RunUploadTaskAsync(task);
                    }
                }
                finally
                {
                    _schedulerLock.Release();
                }
            }
        }

        private async Task RunUploadTaskAsync(UploadItemModel task)
        {
            try
            {
                await UploadFileAsync(task);
                if (task.State is UploadTaskStateEnum.CalcHash or UploadTaskStateEnum.Uploading)
                {
                    await App.DispatcherQueue!.EnqueueAsync(() => task.State = UploadTaskStateEnum.Failed);
                }
            }
            catch (Exception ex)
            {
                await LogHelper.Error(ex);
                await App.DispatcherQueue!.EnqueueAsync(() => task.State = UploadTaskStateEnum.Failed);
            }
            finally
            {
                await _schedulerLock.WaitAsync();
                try
                {
                    _runningTasks.Remove(task);
                    if (task.State == UploadTaskStateEnum.Failed)
                    {
                        var retryCount = _retryCounts.GetValueOrDefault(task);
                        var maxRetry = Math.Clamp(_system.UploadMaxRetry, 0, UploadSettings.MaxRetry);
                        if (retryCount < maxRetry)
                        {
                            _retryCounts[task] = retryCount + 1;
                            await App.DispatcherQueue!.EnqueueAsync(() => task.State = UploadTaskStateEnum.Queued);
                        }
                    }
                    else if (task.State is UploadTaskStateEnum.Completed or UploadTaskStateEnum.Canceled)
                    {
                        _retryCounts.Remove(task);
                    }
                }
                finally
                {
                    _schedulerLock.Release();
                }
            }
        }

        private async Task UploadFileAsync(UploadItemModel task)
        {
            if (ShouldStopUpload(task))
            {
                return;
            }

            var checkpointUploadId = task.UploadId;
            var checkpointPickCode = task.PickCode;
            var checkpointBucket = task.Bucket;
            var checkpointObject = task.Object;
            if (task.FilePath is null || task.FilePath.AsFilePathAndExists() != true)
            {
                await App.DispatcherQueue!.EnqueueAsync(() =>
                {
                    task.State = UploadTaskStateEnum.Failed;
                });
                return;
            }
            Sha1Digest digest = new Sha1Digest();
            var fileName = Path.GetFileName(task.Name);
            var fileSize = task.Size;
            if (fileSize is null || fileSize == 0)
            {
                await App.DispatcherQueue!.EnqueueAsync(() =>
                {
                    task.State = UploadTaskStateEnum.Failed;
                });
                return;
            }
            var target = $"U_1_{task.ParentId}";
            if (task.ParentId is null || task.ParentId == "-1")
            {
                target = "U_1_0";
            }
            await App.DispatcherQueue!.EnqueueAsync(() =>
            {
                if (!ShouldStopUpload(task))
                {
                    task.State = UploadTaskStateEnum.CalcHash;
                }
            });
            if (ShouldStopUpload(task))
            {
                return;
            }
            var fileid = string.Empty;
            using (var fs = new FileStream(task.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var len = 0;
                var datas = new byte[1024];
                while ((len = await fs.ReadAsync(datas, 0, datas.Length)) > 0)
                {
                    digest.BlockUpdate(datas, 0, len);
                }
                var hash = new byte[digest.GetDigestSize()];
                digest.DoFinal(hash, 0);
                fileid = Hex.ToHexString(hash);
            }
            if (ShouldStopUpload(task))
            {
                return;
            }
            var reqInit = new RestRequest(ApiResource.OpenUploadInit);
            reqInit.AddParameter("file_name", fileName);
            reqInit.AddParameter("file_size", fileSize.Value);
            reqInit.AddParameter("target", target);
            reqInit.AddParameter("fileid", fileid);

            ProResponseDTO<OpenUploadInitDTO>? dtoInit = null;
            ProResponseDTO<OpenUploadInitNoCallbackDTO>? dtoInitNoCallback = null;
            OpenUploadInitDTO? f = null;
            OpenUploadInitNoCallbackDTO? fNoCallback = null;
            string? callback = null;
            string? region = null;
            string? endpoint = null;
            string? accessKeySecret = null;
            string? securityToken = null;
            string? expiration = null;
            string? accessKeyId = null;
            string? bucket = null;
            string? objectId = null;
            Dictionary<string, string>? callbackVars = null;
            try
            {
                var resInit = await App.ProApiClient.PostAsync(reqInit);
                if (!resInit.IsSuccessful || resInit.Content.IsBlank())
                {
                    await App.DispatcherQueue!.EnqueueAsync(() =>
                    {
                        task.State = UploadTaskStateEnum.Failed;
                    });
                    return;
                }
                dtoInitNoCallback = JsonSerializer.Deserialize<ProResponseDTO<OpenUploadInitNoCallbackDTO>>(resInit.Content);
                if (dtoInitNoCallback is null || !dtoInitNoCallback.State || dtoInitNoCallback.Data is null)
                {
                    await App.DispatcherQueue!.EnqueueAsync(() =>
                    {
                        task.State = UploadTaskStateEnum.Failed;
                    });
                    return;
                }
                fNoCallback = dtoInitNoCallback.Data;
                if (fNoCallback.Status == 2)
                {
                    await App.DispatcherQueue!.EnqueueAsync(() =>
                    {
                        task.PickCode = fNoCallback.PickCode;
                        task.FileId = fNoCallback.FileId;
                        task.Progress = 1;
                        task.State = UploadTaskStateEnum.Completed;
                    });
                    return;
                }
                // 二次校验
                else if (fNoCallback.Status == 7 && fNoCallback.Code == 701 && fNoCallback.SignCheck.IsNotBlank())
                {
                    var signVal = string.Empty;
                    var checkStart = fNoCallback.SignCheck.Split("-")[0].ToLong();
                    var checkLength = fNoCallback.SignCheck.Split("-")[1].ToLong() - checkStart + 1;
                    digest.Reset();
                    using (var fs = new FileStream(task.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        var ds = new byte[checkLength];
                        fs.Position = checkStart;
                        await fs.ReadExactlyAsync(ds, 0, ds.Length);
                        digest.BlockUpdate(ds);
                        var hash = new byte[digest.GetDigestSize()];
                        digest.DoFinal(hash, 0);
                        signVal = Hex.ToHexString(hash).ToUpper();
                    }
                    reqInit.AddParameter("sign_key", fNoCallback.SignKey);
                    reqInit.AddParameter("sign_val", signVal);
                    resInit = await App.ProApiClient.PostAsync(reqInit);
                    if (!resInit.IsSuccessful || resInit.Content.IsBlank())
                    {
                        await App.DispatcherQueue!.EnqueueAsync(() =>
                        {
                            task.State = UploadTaskStateEnum.Failed;
                        });
                        return;
                    }
                    dtoInitNoCallback = JsonSerializer.Deserialize<ProResponseDTO<OpenUploadInitNoCallbackDTO>>(resInit.Content);
                    if (dtoInitNoCallback is null || !dtoInitNoCallback.State || dtoInitNoCallback.Data is null)
                    {
                        await App.DispatcherQueue!.EnqueueAsync(() =>
                        {
                            task.State = UploadTaskStateEnum.Failed;
                        });
                        return;
                    }
                    fNoCallback = dtoInitNoCallback.Data;
                    if (fNoCallback.Status == 2)
                    {
                        await App.DispatcherQueue!.EnqueueAsync(() =>
                        {
                            task.PickCode = fNoCallback.PickCode;
                            task.FileId = fNoCallback.FileId;
                            task.Progress = 1;
                            task.State = UploadTaskStateEnum.Completed;
                        });
                        return;
                    }
                    else if (fNoCallback.Status == 1)
                    {
                        dtoInit = JsonSerializer.Deserialize<ProResponseDTO<OpenUploadInitDTO>>(resInit.Content);
                        f = dtoInit?.Data;
                        return;
                    }
                    else
                    {
                        await App.DispatcherQueue!.EnqueueAsync(() =>
                        {
                            task.State = UploadTaskStateEnum.Failed;
                        });
                        return;
                    }
                }
                else if (fNoCallback.Status == 1)
                {
                    dtoInit = JsonSerializer.Deserialize<ProResponseDTO<OpenUploadInitDTO>>(resInit.Content);
                    f = dtoInit?.Data;
                }
                if (f is null)
                {
                    await App.DispatcherQueue!.EnqueueAsync(() =>
                    {
                        task.State = UploadTaskStateEnum.Failed;
                    });
                    return;
                }
                bucket = f.Bucket;
                objectId = f.Object;
                await App.DispatcherQueue!.EnqueueAsync(() =>
                {
                    task.PickCode = f.PickCode;
                    task.Bucket = bucket;
                    task.Object = objectId;
                });
                if (f.Callback is not null && f.Callback.CallbackVar is not null)
                {
                    callback = f.Callback.Callback;
                    callbackVars = JsonSerializer.Deserialize<Dictionary<string, string>>(f.Callback.CallbackVar);
                    await App.DispatcherQueue!.EnqueueAsync(() =>
                    {
                        task.CallbackVar = callbackVars;
                        task.Callback = callback;
                    });
                }
            }
            catch (Exception ex)
            {
                await LogHelper.Error(ex);
            }
            if (callback.IsBlank())
            {
                await App.DispatcherQueue!.EnqueueAsync(() =>
                {
                    task.State = UploadTaskStateEnum.Failed;
                });
                return;
            }
            var reqToken = new RestRequest(ApiResource.OpenUploadGetToken);
            try
            {
                var resToken = await App.ProApiClient.GetAsync(reqToken);
                if (!resToken.IsSuccessful || resToken.Content.IsBlank())
                {
                    await App.DispatcherQueue!.EnqueueAsync(() =>
                    {
                        task.State = UploadTaskStateEnum.Failed;
                    });
                    return;
                }
                var dtoToken = JsonSerializer.Deserialize<ProResponseDTO<OpenUploadGetTokenDTO>>(resToken.Content);
                if (dtoToken is null || !dtoToken.State || dtoToken.Data is null || dtoToken.Data.Endpoint.IsBlank())
                {
                    await App.DispatcherQueue!.EnqueueAsync(() =>
                    {
                        task.State = UploadTaskStateEnum.Failed;
                    });
                    return;
                }
                var match = System.Text.RegularExpressions.Regex.Match(
                    dtoToken.Data.Endpoint,
                    @"oss-([^.]+)\.aliyun"
                );
                region = match.Success ? match.Groups[1].Value : string.Empty;
                endpoint = dtoToken.Data.Endpoint;
                accessKeySecret = dtoToken.Data.AccessKeySecret;
                securityToken = dtoToken.Data.SecurityToken;
                expiration = dtoToken.Data.Expiration;
                accessKeyId = dtoToken.Data.AccessKeyId;
                await App.DispatcherQueue!.EnqueueAsync(() =>
                {
                    task.Region = region;
                    task.Endpoint = endpoint;
                    task.AccessKeySecret = accessKeySecret;
                    task.SecurityToken = securityToken;
                    task.Expiration = expiration;
                    task.AccessKeyId = accessKeyId;
                });
            }
            catch (Exception ex)
            {
                await LogHelper.Error(ex);
            }
            string responseContent = string.Empty;
            var localFilename = task.FilePath;
            try
            {
                var conf = new ClientConfiguration();
                conf.SignatureVersion = SignatureVersion.V4;
                var client = new OssClient(endpoint, accessKeyId, accessKeySecret, securityToken, conf);
                client.SetRegion(task.Region);
                // 普通文件上传(小于200M)
                if (task.Size <= 209715200)
                {
                    var callbackDto = JsonSerializer.Deserialize<AliyunOssCallbackDTO>(callback);
                    if (callbackDto is null)
                    {
                        await App.DispatcherQueue!.EnqueueAsync(() =>
                        {
                            task.State = UploadTaskStateEnum.Failed;
                        });
                        return;
                    }
                    var callbackMeta = new ObjectMetadata();
                    string callbackHeaderBuilder = new CallbackHeaderBuilder(callbackDto.CallbackUrl, callbackDto.CallbackBody)
                        .Build();
                    callbackMeta.AddHeader(HttpHeaders.Callback, callbackHeaderBuilder);
                    var callbackVariableHeaderBuilder = new CallbackVariableHeaderBuilder();
                    if (callbackVars is not null)
                    {
                        foreach (var v in callbackVars)
                        {
                            callbackVariableHeaderBuilder.AddCallbackVariable(v.Key, v.Value);
                        }
                    }
                    callbackMeta.AddHeader(HttpHeaders.CallbackVar, callbackVariableHeaderBuilder.Build());
                    using var inputStream = File.OpenRead(localFilename);
                    PutObjectRequest request = new PutObjectRequest(bucket, objectId, inputStream)
                    {
                        StreamTransferProgress = (_, args) => UpdateStreamProgress(task, args),
                        Metadata = callbackMeta
                    };
                    var result = client.PutObject(request);
                    responseContent = GetCallbackResponse(result);
                }
                // 分片上传
                else
                {
                    if (ShouldStopUpload(task))
                    {
                        return;
                    }
                    var reqResume = new RestRequest(ApiResource.OpenUploadResume);
                    reqResume.AddParameter("file_size", $"{fileSize}");
                    reqResume.AddParameter("target", target);
                    reqResume.AddParameter("fileid", fileid);
                    reqResume.AddParameter("pick_code", checkpointUploadId.IsNotBlank() && checkpointPickCode.IsNotBlank()
                        ? checkpointPickCode
                        : task.PickCode);
                    var resResume = await App.ProApiClient.PostAsync(reqResume);
                    if (ShouldStopUpload(task))
                    {
                        return;
                    }
                    if (!resResume.IsSuccessful || resResume.Content.IsBlank())
                    {
                        await App.DispatcherQueue!.EnqueueAsync(() =>
                        {
                            task.State = UploadTaskStateEnum.Failed;
                        });
                        return;
                    }
                    var dto = JsonSerializer.Deserialize<ProResponseDTO>(resResume.Content);
                    if (dto is null || dto.State != true)
                    {
                        await App.DispatcherQueue!.EnqueueAsync(() =>
                        {
                            task.State = UploadTaskStateEnum.Failed;
                        });
                        return;
                    }
                    var dtoResume = JsonSerializer.Deserialize<ProResponseDTO<OpenUploadResumeDTO>>(resResume.Content);
                    if (dtoResume is null || dtoResume.Data is null || dtoResume.Data.Callback is null)
                    {
                        await App.DispatcherQueue!.EnqueueAsync(() =>
                        {
                            task.State = UploadTaskStateEnum.Failed;
                        });
                        return;
                    }
                    if (dtoResume.Data.Callback.Callback.IsBlank() || dtoResume.Data.Callback.CallbackVar.IsBlank())
                    {
                        await App.DispatcherQueue!.EnqueueAsync(() =>
                        {
                            task.State = UploadTaskStateEnum.Failed;
                        });
                        return;
                    }
                    var uploadingPickCode = checkpointUploadId.IsNotBlank() && checkpointPickCode.IsNotBlank()
                        ? checkpointPickCode
                        : dtoResume.Data.PickCode;
                    bucket = checkpointUploadId.IsNotBlank() && checkpointBucket.IsNotBlank()
                        ? checkpointBucket
                        : dtoResume.Data.Bucket;
                    objectId = checkpointUploadId.IsNotBlank() && checkpointObject.IsNotBlank()
                        ? checkpointObject
                        : dtoResume.Data.Object;
                    callback = dtoResume.Data.Callback.Callback;
                    callbackVars = JsonSerializer.Deserialize<Dictionary<string, string>>(dtoResume.Data.Callback.CallbackVar);
                    var callbackDto = JsonSerializer.Deserialize<AliyunOssCallbackDTO>(callback);
                    if (callbackDto is null || callbackVars is null)
                    {
                        await App.DispatcherQueue!.EnqueueAsync(() =>
                        {
                            task.State = UploadTaskStateEnum.Failed;
                        });
                        return;
                    }
                    await App.DispatcherQueue!.EnqueueAsync(() =>
                    {
                        task.PickCode = uploadingPickCode;
                        task.Bucket = bucket;
                        task.Object = objectId;
                    });
                    string checkpointDir = Path.Combine(App.AppPath, "upload_check");
                    if (Directory.Exists(checkpointDir) != true)
                    {
                        Directory.CreateDirectory(checkpointDir);
                    }
                    var callbackMeta = new ObjectMetadata();
                    string callbackHeaderBuilder = new CallbackHeaderBuilder(callbackDto.CallbackUrl, callbackDto.CallbackBody)
                        .Build();
                    callbackMeta.AddHeader(HttpHeaders.Callback, callbackHeaderBuilder);
                    var callbackVariableHeaderBuilder = new CallbackVariableHeaderBuilder();
                    if (callbackVars is not null)
                    {
                        foreach (var v in callbackVars)
                        {
                            callbackVariableHeaderBuilder.AddCallbackVariable(v.Key, v.Value);
                        }
                    }
                    callbackMeta.AddHeader(HttpHeaders.CallbackVar, callbackVariableHeaderBuilder.Build());
                    if (endpoint.IsBlank() || bucket.IsBlank() || objectId.IsBlank()||accessKeyId.IsBlank()|| accessKeySecret.IsBlank())
                    {
                        await App.DispatcherQueue!.EnqueueAsync(() =>
                        {
                            task.State = UploadTaskStateEnum.Failed;
                        });
                        return;
                    }
                    var uploadId = task.UploadId ?? "";
                    try
                    {
                        if (ShouldStopUpload(task))
                        {
                            return;
                        }
                        if (uploadId.IsNotBlank())
                        {
                            try
                            {
                                // 不盲信本地 ETag；恢复前以 OSS 服务端实际已存在的分片为准。
                                var listing = client.ListParts(new ListPartsRequest(bucket, objectId, uploadId)
                                {
                                    MaxParts = 1000
                                });
                                var serverParts = listing.Parts.ToDictionary(p => p.PartNumber, p => p.PartETag.ETag);
                                await App.DispatcherQueue!.EnqueueAsync(() => task.PartETags = serverParts);
                                if (ShouldStopUpload(task))
                                {
                                    return;
                                }
                            }
                            catch (OssException ex)
                            {
                                Debug.WriteLine(ex);
                                Debug.WriteLine(ex.StackTrace);
                                // UploadId 已失效，下面重新申请新的 multipart 会话。
                                uploadId = string.Empty;
                                await App.DispatcherQueue!.EnqueueAsync(() =>
                                {
                                    task.UploadId = string.Empty;
                                    task.PartETags = new Dictionary<int, string>();
                                });
                            }
                        }
                        if (uploadId.IsBlank())
                        {
                            if (ShouldStopUpload(task))
                            {
                                return;
                            }
                            var result = await InitiateMultipartUploadAsync(endpoint, bucket, objectId, accessKeyId, accessKeySecret, securityToken);
                            uploadId = result.UploadId;
                            await App.DispatcherQueue!.EnqueueAsync(() => task.UploadId = uploadId);
                            if (ShouldStopUpload(task))
                            {
                                return;
                            }
                        }
                        Debug.WriteLine("Init multi part upload succeeded");
                        Debug.WriteLine("Upload Id:{0}", uploadId);
                    }
                    catch (Exception ex)
                    {
                        await LogHelper.Error(ex);
                    }
                    const long minimumPartSize = 1L * 1024 * 1024;
                    const long maximumPartCount = 1000;
                    var partSize = Math.Max(minimumPartSize, (fileSize.Value + maximumPartCount - 1) / maximumPartCount);
                    var fi = new FileInfo(localFilename);
                    var partCount = fileSize / partSize;
                    if (fileSize % partSize != 0)
                    {
                        partCount++;
                    }
                    var partETags = (task.PartETags ?? new Dictionary<int, string>())
                        .OrderBy(x => x.Key)
                        .Select(x => new PartETag(x.Key, x.Value))
                        .ToList();
                    try
                    {
                        if (ShouldStopUpload(task))
                        {
                            return;
                        }
                        await App.DispatcherQueue!.EnqueueAsync(() =>
                        {
                            task.State = UploadTaskStateEnum.Uploading;
                            task.Progress = partETags.Count * 1.0 / partCount;
                        });
                        using (var fs = File.Open(localFilename, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            for (var i = 0; i < partCount; i++)
                            {
                                if (ShouldStopUpload(task))
                                {
                                    return;
                                }
                                if (task.PartETags?.ContainsKey(i + 1) == true)
                                {
                                    continue;
                                }
                                var skipBytes = (long)partSize * i;
                                // 定位到本次上传的起始位置。
                                fs.Seek(skipBytes, 0);
                                // 计算本次上传的分片大小，最后一片为剩余的数据大小。
                                var size = (partSize < fileSize - skipBytes) ? partSize : (fileSize - skipBytes);
                                var request = new UploadPartRequest(bucket, objectId, uploadId)
                                {
                                    InputStream = fs,
                                    PartSize = size,
                                    PartNumber = i + 1,
                                };
                                // 调用UploadPart接口执行上传功能，返回结果中包含了这个数据片的ETag值。
                                var result = client.UploadPart(request);
                                partETags.Add(result.PartETag);
                                await App.DispatcherQueue!.EnqueueAsync(() =>
                                {
                                    task.PartETags ??= new Dictionary<int, string>();
                                    task.PartETags[result.PartETag.PartNumber] = result.PartETag.ETag;
                                });
                                Debug.WriteLine("finish {0}/{1}", partETags.Count, partCount);
                                await App.DispatcherQueue!.EnqueueAsync(() =>
                                {
                                    task.Progress = partETags.Count * 1.0 / partCount;
                                });
                                if (ShouldStopUpload(task))
                                {
                                    return;
                                }
                            }
                            Debug.WriteLine("Put multi part upload succeeded");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Put multi part upload failed, {0}", ex.Message);
                        var errorText = ex.ToString();
                        if (errorText.Contains("specified upload does not exist", StringComparison.OrdinalIgnoreCase)
                            || errorText.Contains("upload ID may be invalid", StringComparison.OrdinalIgnoreCase))
                        {
                            // 服务端已清理检查点，丢弃本地 UploadId/ETag 后重新初始化。
                            task.UploadId = string.Empty;
                            task.PartETags = new Dictionary<int, string>();
                            task.Progress = 0;
                            await App.DispatcherQueue!.EnqueueAsync(() =>
                            {
                                task.State = UploadTaskStateEnum.Queued;
                            });
                        }
                        else
                        {
                            await App.DispatcherQueue!.EnqueueAsync(() => task.State = UploadTaskStateEnum.Failed);
                        }
                        return;
                    }

                    try
                    {
                        if (ShouldStopUpload(task))
                        {
                            return;
                        }
                        var completeMultipartUploadRequest = new CompleteMultipartUploadRequest(bucket, objectId, uploadId);
                        foreach (var partETag in partETags)
                        {
                            completeMultipartUploadRequest.PartETags.Add(partETag);
                        }
                        completeMultipartUploadRequest.Metadata = callbackMeta;
                        var result = client.CompleteMultipartUpload(completeMultipartUploadRequest);
                        Debug.WriteLine("complete multi part succeeded");
                        responseContent = GetCallbackResponse(result);
                        await App.DispatcherQueue!.EnqueueAsync(() =>
                        {
                            task.State = UploadTaskStateEnum.Completed;
                            task.Progress = 1;
                        });
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("complete multi part failed, {0}", ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                await LogHelper.Error(ex);
            }

            try
            {
                Debug.WriteLine($"===>res:{responseContent}");
                var uploadRes = JsonSerializer.Deserialize<ProResponseDTO>(responseContent);
                if (uploadRes is null)
                {
                    return;
                }
                if (uploadRes.State == true)
                {
                    await App.DispatcherQueue!.EnqueueAsync(() =>
                    {
                        task.Progress = 1;
                        task.State = UploadTaskStateEnum.Completed;
                        App.ShowMessageBar($"上传成功", "信息", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success, autoClose: TimeSpan.FromSeconds(5));
                    });
                }
                else
                {
                    App.DispatcherQueue!.TryEnqueue(() =>
                    {
                        App.ShowMessageBar($"{uploadRes?.Message}", "错误", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error, autoClose: TimeSpan.FromSeconds(5));
                    });
                }
            }
            catch (Exception ex)
            {
                await LogHelper.Error(ex);
            }
        }

        private static bool ShouldStopUpload(UploadItemModel task)
        {
            return task.State == UploadTaskStateEnum.Paused
                || task.State == UploadTaskStateEnum.Canceled;
        }

        /// <summary>
        /// 读取上传回调返回的消息内容。
        /// </summary>
        private static string GetCallbackResponse(PutObjectResult putObjectResult)
        {
            string? callbackResponse = null;
            using (var stream = putObjectResult.ResponseStream)
            {
                var buffer = new byte[4 * 1024];
                var bytesRead = stream.Read(buffer, 0, buffer.Length);
                callbackResponse = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            }
            return callbackResponse;
        }

        private void UpdateStreamProgress(UploadItemModel task, StreamTransferProgressArgs args)
        {
            var p = args.TransferredBytes * 1.0f / args.TotalBytes;
            App.DispatcherQueue?.TryEnqueue(() =>
            {
                task.Progress = p;
                task.UploadedSize = args.TransferredBytes;
            });
            if (task.TaskId is > 0)
            {
                var col = _db.GetCollection<UploadTaskEntity>(CollectionResource.UploadTask);
                var find = col.FindById(task.TaskId.Value);
                if (find is not null)
                {
                    find.Progress = p;
                    col.Update(find);
                }
            }
        }

        public async Task AddTask(string filePath, string targetDirId = "0", int? parentTaskId = null)
        {
            if (filePath.AsFilePathAndExists() != true)
            {
                return;
            }
            if (targetDirId.IsBlank())
            {
                return;
            }
            try
            {
                var item = new UploadItemModel
                {
                    Name = Path.GetFileName(filePath),
                    Size = new FileInfo(filePath).Length,
                    ParentId = targetDirId,
                    FilePath = filePath,
                    ParentTaskId = parentTaskId,
                    State = UploadTaskStateEnum.Queued,
                };
                await _uploadQueue.Writer.WriteAsync(item);
            }
            catch (Exception ex)
            {
                await LogHelper.Error(ex);
            }
            finally
            {
            }
        }

        private void PersistTask(UploadItemModel task)
        {
            if (task.TaskId is null or 0 || User.UserId.IsBlank())
            {
                return;
            }

            var collection = _db.GetCollection<UploadTaskEntity>(CollectionResource.UploadTask);
            var entity = collection.FindById(task.TaskId.Value);
            if (entity is null)
            {
                return;
            }

            entity.FileId = task.FileId;
            entity.ParentId = task.ParentId;
            entity.Name = task.Name;
            entity.Size = task.Size;
            entity.Progress = task.Progress;
            entity.UploadedSize = task.UploadedSize;
            entity.ParentTaskId = task.ParentTaskId;
            entity.IsFolder = task.IsFolder;
            entity.TotalFiles = task.TotalFiles;
            entity.FilePath = task.FilePath;
            entity.Bucket = task.Bucket;
            entity.Object = task.Object;
            entity.Endpoint = task.Endpoint;
            entity.Region = task.Region;
            entity.PickCode = task.PickCode;
            entity.UploadId = task.UploadId;
            entity.PartETagsJson = JsonSerializer.Serialize(task.PartETags ?? new Dictionary<int, string>());
            entity.State = task.State;
            collection.Update(entity);
        }

        public async Task<int> BeginFolderTaskAsync(string folderName, string folderPath, string targetDirectoryId)
        {
            var item = new UploadItemModel
            {
                Name = folderName,
                FilePath = folderPath,
                ParentId = targetDirectoryId,
                IsFolder = true,
                Progress = 0,
                State = UploadTaskStateEnum.Queued
            };
            var collection = _db.GetCollection<UploadTaskEntity>(CollectionResource.UploadTask);
            item.TaskId = collection.Insert(new UploadTaskEntity
            {
                UserId = User.UserId,
                Name = folderName,
                FilePath = folderPath,
                ParentId = targetDirectoryId,
                IsFolder = true,
                Progress = 0,
                State = UploadTaskStateEnum.Queued,
                CreateTime = DateTime.Now,
                PartETagsJson = "{}"
            }).AsInt32;
            item.PropertyChanged += (_, _) => PersistTask(item);
            await App.DispatcherQueue!.EnqueueAsync(() => UploadItems.Insert(0, item));
            return item.TaskId.Value;
        }

        public void CompleteFolderCollection(int taskId, int totalFiles, long totalSize)
        {
            var folder = UploadItems.FirstOrDefault(item => item.TaskId == taskId);
            if (folder is not null)
            {
                folder.TotalFiles = totalFiles;
                folder.Size = totalSize;
                if (totalFiles == 0)
                {
                    folder.Progress = 1;
                    folder.State = UploadTaskStateEnum.Completed;
                }
                PersistTask(folder);
            }
            var collection = _db.GetCollection<UploadTaskEntity>(CollectionResource.UploadTask);
            var entity = collection.FindById(taskId);
            if (entity is not null)
            {
                entity.TotalFiles = totalFiles;
                entity.Size = totalSize;
                if (totalFiles == 0)
                {
                    entity.Progress = 1;
                    entity.State = UploadTaskStateEnum.Completed;
                }
                collection.Update(entity);
            }
            UpdateTransferMetrics();
        }

        private void UpdateTransferMetrics()
        {
            var now = DateTime.UtcNow;
            foreach (var item in UploadItems.Where(item => !item.IsFolder))
            {
                var bytes = item.State == UploadTaskStateEnum.Completed
                    ? item.Size ?? 0
                    : (long)Math.Clamp((item.Size ?? 0) * (item.Progress ?? 0), 0, item.Size ?? 0);
                item.UploadedSize = bytes;
                if (_speedSnapshots.TryGetValue(item, out var previous))
                {
                    var seconds = (now - previous.Timestamp).TotalSeconds;
                    var rawSpeed = seconds > 0 ? Math.Max(0, bytes - previous.Bytes) / seconds : 0;
                    var speed = previous.Speed <= 0 ? rawSpeed : (0.3 * rawSpeed) + (0.7 * previous.Speed);
                    item.Speed = item.State == UploadTaskStateEnum.Uploading ? (long)speed : 0;
                    item.RemainingTime = item.Speed > 0 && item.Size > bytes
                        ? TimeSpan.FromSeconds((item.Size.Value - bytes) / (double)item.Speed.Value)
                        : null;
                    _speedSnapshots[item] = (bytes, now, speed);
                }
                else
                {
                    _speedSnapshots[item] = (bytes, now, 0);
                }
            }

            foreach (var folder in UploadItems.Where(item => item.IsFolder).ToList())
            {
                var children = UploadItems.Where(item => item.ParentTaskId == folder.TaskId).ToList();
                if (children.Count == 0)
                {
                    continue;
                }

                var uploaded = children.Sum(item => item.UploadedSize ?? 0);
                folder.TotalFiles = children.Count;
                folder.Size = children.Sum(item => item.Size ?? 0);
                folder.UploadedSize = uploaded;
                folder.Progress = folder.Size > 0 ? uploaded / (double)folder.Size : 0;
                folder.Speed = children.Where(item => item.State == UploadTaskStateEnum.Uploading).Sum(item => item.Speed ?? 0);
                folder.RemainingTime = folder.Speed > 0 && folder.Size > uploaded
                    ? TimeSpan.FromSeconds((folder.Size.Value - uploaded) / (double)folder.Speed.Value)
                    : null;
                folder.State = DeriveFolderState(children);
                PersistTask(folder);
            }
        }

        private static UploadTaskStateEnum DeriveFolderState(IReadOnlyCollection<UploadItemModel> children)
        {
            if (children.All(item => item.State == UploadTaskStateEnum.Completed)) return UploadTaskStateEnum.Completed;
            if (children.Any(item => item.State is UploadTaskStateEnum.Uploading or UploadTaskStateEnum.CalcHash)) return UploadTaskStateEnum.Uploading;
            if (children.Any(item => item.State == UploadTaskStateEnum.Queued)) return UploadTaskStateEnum.Queued;
            if (children.All(item => item.State == UploadTaskStateEnum.Paused)) return UploadTaskStateEnum.Paused;
            if (children.Any(item => item.State == UploadTaskStateEnum.Failed)) return UploadTaskStateEnum.Failed;
            return UploadTaskStateEnum.Canceled;
        }

        public async Task RetryTaskAsync(UploadItemModel task)
        {
            var retryTasks = task.IsFolder
                ? UploadItems.Where(item => item.ParentTaskId == task.TaskId
                    && item.State is UploadTaskStateEnum.Failed or UploadTaskStateEnum.Canceled).ToList()
                : [task];

            if (task.IsFolder && retryTasks.Count == 0)
            {
                if (task.FilePath.IsNotBlank() && Directory.Exists(task.FilePath))
                {
                    await App.Resolve<MyFilesViewModel>().RestartFolderUploadAsync(task);
                }
                return;
            }

            await App.DispatcherQueue!.EnqueueAsync(() =>
            {
                foreach (var item in retryTasks)
                {
                    _retryCounts.Remove(item);
                    item.Speed = 0;
                    item.RemainingTime = null;
                    item.State = UploadTaskStateEnum.Queued;
                }
                if (task.IsFolder) task.State = UploadTaskStateEnum.Queued;
            });
            SaveTaskStates(retryTasks, UploadTaskStateEnum.Queued);
            UpdateTransferMetrics();
        }

        public void MarkFolderCollectionFailed(int taskId)
        {
            var folder = UploadItems.FirstOrDefault(item => item.TaskId == taskId);
            if (folder is not null) folder.State = UploadTaskStateEnum.Failed;
            var entity = _db.GetCollection<UploadTaskEntity>(CollectionResource.UploadTask).FindById(taskId);
            if (entity is not null)
            {
                entity.State = UploadTaskStateEnum.Failed;
                _db.GetCollection<UploadTaskEntity>(CollectionResource.UploadTask).Update(entity);
            }
        }

        [RelayCommand]
        public async Task ClearFinish()
        {
        }

        [RelayCommand]
        public async Task PauseAll()
        {
            await _schedulerLock.WaitAsync();
            try
            {
                _isQueuePaused = true;
                var tasks = UploadItems
                    .Where(item => item.State is UploadTaskStateEnum.Queued
                        or UploadTaskStateEnum.CalcHash
                        or UploadTaskStateEnum.Uploading)
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
                        task.State = UploadTaskStateEnum.Paused;
                    }
                });
                SaveTaskStates(tasks, UploadTaskStateEnum.Paused);
            }
            finally
            {
                _schedulerLock.Release();
            }
        }

        [RelayCommand]
        public async Task StartAll()
        {
            await _schedulerLock.WaitAsync();
            try
            {
                var tasks = UploadItems
                    .Where(item => item.State == UploadTaskStateEnum.Paused)
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
                        task.State = UploadTaskStateEnum.Queued;
                    }
                });
                _isQueuePaused = false;
                SaveTaskStates(tasks, UploadTaskStateEnum.Queued);
            }
            finally
            {
                _schedulerLock.Release();
            }
        }

        private void SaveTaskStates(IEnumerable<UploadItemModel> tasks, UploadTaskStateEnum state)
        {
            var collection = _db.GetCollection<UploadTaskEntity>(CollectionResource.UploadTask);
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



        /// <summary>
        /// 手动实现 OSS InitiateMultipartUpload（Signature V1）
        /// 适用于：bucket.oss-cn-shenzhen.aliyuncs.com
        /// </summary>
        /// <param name="endpoint">例如：https://oss-cn-shenzhen.aliyuncs.com</param>
        /// <param name="bucketName">Bucket 名称</param>
        /// <param name="objectKey">对象名，例如 folder/test.txt</param>
        /// <param name="accessKeyId">AK</param>
        /// <param name="accessKeySecret">SK</param>
        /// <param name="securityToken">STS Token，可为空</param>
        /// <param name="extraQuery">额外 Query 参数</param>
        /// <param name="headers">额外请求头</param>
        public static async Task<InitiateMultipartUploadResultEx> InitiateMultipartUploadAsync(
            string endpoint,
            string bucketName,
            string objectKey,
            string accessKeyId,
            string accessKeySecret,
            string? securityToken = null,
            IDictionary<string, string>? extraQuery = null,
            IDictionary<string, string>? headers = null)
        {
            using var httpClient = new HttpClient();

            // ----------------------------
            // 1. 构造 Date
            // ----------------------------
            string date = DateTime.UtcNow.ToString("r");

            // ----------------------------
            // 2. Canonicalized OSS Headers
            // ----------------------------
            var ossHeaders = new SortedDictionary<string, string>(StringComparer.Ordinal);

            if (!string.IsNullOrWhiteSpace(securityToken))
            {
                ossHeaders["x-oss-security-token"] = securityToken;
            }

            if (headers != null)
            {
                foreach (var kv in headers)
                {
                    if (kv.Key.StartsWith("x-oss-", StringComparison.OrdinalIgnoreCase))
                    {
                        ossHeaders[kv.Key.ToLowerInvariant()] = kv.Value;
                    }
                }
            }

            string canonicalizedOssHeaders = string.Join(
                "\n",
                ossHeaders.Select(kv => $"{kv.Key}:{kv.Value}"));

            if (!string.IsNullOrEmpty(canonicalizedOssHeaders))
            {
                canonicalizedOssHeaders += "\n";
            }

            // ----------------------------
            // 3. Canonicalized Resource
            // ----------------------------
            var query = new SortedDictionary<string, string?>(StringComparer.Ordinal)
            {
                ["sequential"] = null,
                ["uploads"] = null,
            };

            if (extraQuery != null)
            {
                foreach (var kv in extraQuery)
                {
                    query[kv.Key] = kv.Value;
                }
            }

            // 用于签名的子资源
            string canonicalizedQuery = string.Join("&",
                query.Select(kv =>
                    kv.Value == null
                        ? $"{kv.Key}"
                        : $"{kv.Key}={kv.Value}"));

            string canonicalizedResource =
                $"/{bucketName}/{objectKey}" +
                (string.IsNullOrEmpty(canonicalizedQuery)
                    ? ""
                    : "?" + canonicalizedQuery);

            // ----------------------------
            // 4. StringToSign
            // ----------------------------
            string stringToSign =
                $"POST\n" +       // VERB
                $"\n" +           // Content-MD5
                $"\n" +           // Content-Type
                $"{date}\n" +
                $"{canonicalizedOssHeaders}" +
                $"{canonicalizedResource}";

            // ----------------------------
            // 5. HMAC-SHA1 签名
            // ----------------------------
            using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(accessKeySecret));
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
            string signature = Convert.ToBase64String(hash);

            string authorization = $"OSS {accessKeyId}:{signature}";

            // ----------------------------
            // 6. 构造最终 URL
            // ----------------------------
            string endpointHost = endpoint
                .Replace("https://", "")
                .Replace("http://", "")
                .TrimEnd('/');

            string encodedObjectKey = string.Join("/",
                objectKey.Split('/')
                         .Select(Uri.EscapeDataString));

            string url =
                $"https://{bucketName}.{endpointHost}/{encodedObjectKey}?{canonicalizedQuery}";

            // ----------------------------
            // 7. 创建请求
            // ----------------------------
            using var request = new HttpRequestMessage(HttpMethod.Post, url);

            request.Headers.TryAddWithoutValidation("Date", date);
            request.Headers.TryAddWithoutValidation("Authorization", authorization);

            // x-oss-* 头
            foreach (var kv in ossHeaders)
            {
                request.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
            }

            // 普通自定义 Header
            if (headers != null)
            {
                foreach (var kv in headers)
                {
                    if (!kv.Key.StartsWith("x-oss-", StringComparison.OrdinalIgnoreCase))
                    {
                        request.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
                    }
                }
            }

            // ----------------------------
            // 8. 发送请求
            // ----------------------------
            using var response = await httpClient.SendAsync(request);
            string xml = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"OSS Error: {(int)response.StatusCode}\n{xml}\n\nStringToSign:\n{stringToSign}");
            }

            // ----------------------------
            // 9. 解析 XML
            // ----------------------------
            var doc = XDocument.Parse(xml);
            XNamespace ns = doc.Root?.Name.Namespace ?? XNamespace.None;

            return new InitiateMultipartUploadResultEx
            {
                Bucket = doc.Root?.Element(ns + "Bucket")?.Value ?? "",
                Key = doc.Root?.Element(ns + "Key")?.Value ?? "",
                UploadId = doc.Root?.Element(ns + "UploadId")?.Value ?? ""
            };
        }
    }

    /// <summary>
    /// InitiateMultipartUpload 返回结果
    /// </summary>
    public sealed class InitiateMultipartUploadResultEx
    {
        public string Bucket { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string UploadId { get; set; } = string.Empty;
    }
}
