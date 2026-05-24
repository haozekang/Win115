using Aliyun.OSS;
using Aliyun.OSS.Common;
using Aliyun.OSS.Model;
using Aliyun.OSS.Util;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using LiteDB;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Utilities.Encoders;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml.Linq;
using Tanovo.ExtensionMethods;
using Win115.Dtos;
using Win115.Entities;
using Win115.Enums;
using Win115.Helpers;
using Win115.Models;
using Win115.Properties;
using Windows.Media.Protection.PlayReady;

namespace Win115.ViewModels
{
    public partial class UploadListViewModel : ObservableRecipient
    {
        private SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        private LiteDatabase _db;
        private string? _uploadingPk;
        private Channel<UploadItemModel> UploadQueue = Channel.CreateUnbounded<UploadItemModel>();

        [ObservableProperty]
        public partial UserInfoModel User { get; set; }

        [ObservableProperty]
        public partial ObservableCollection<UploadItemModel> UploadItems { get; set; }

        public UploadListViewModel(UserInfoModel user, LiteDatabase db)
        {
            User = user;
            _db = db;
            UploadItems = new();

            Task.Factory.StartNew(ReadTask);
            Task.Factory.StartNew(CheckTaskState);
        }

        /// <summary>
        /// 登出后，清理
        /// </summary>
        [RelayCommand]
        public async Task ClearData()
        {
            try
            {
                var col = _db.GetCollection<UploadTaskEntity>(CollectionResource.UploadTask);
                var uploads = UploadQueue.Reader.ReadAllAsync();
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
                App.DispatcherQueue?.TryEnqueue(() => 
                {
                    UploadItems.Clear();
                });
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
                await foreach (var item in UploadQueue.Reader.ReadAllAsync())
                {
                    App.DispatcherQueue?.EnqueueAsync(() =>
                    {
                        UploadItems.Insert(0, item);
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
                    await _semaphoreSlim.WaitAsync();
                    var uploadingTasks = UploadItems.Where(t => t.State == UploadTaskStateEnum.Uploading);
                    if (uploadingTasks.Count() >= 1)
                    {
                        continue;
                    }
                    var task = UploadItems.Where(t => t.State == UploadTaskStateEnum.Queued).FirstOrDefault();
                    if (task is null)
                    {
                        continue;
                    }
                    await UploadFileAsync(task);
                }
                finally
                {
                    _semaphoreSlim.Release();
                }
            }
        }

        private async Task UploadFileAsync(UploadItemModel task)
        {
            if (task.FilePath is null || task.FilePath.AsFilePathAndExists() != true)
            {
                App.DispatcherQueue?.TryEnqueue(() =>
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
                App.DispatcherQueue?.TryEnqueue(() =>
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
            App.DispatcherQueue?.TryEnqueue(() =>
            {
                task.State = UploadTaskStateEnum.CalcHash;
            });
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
                    App.DispatcherQueue?.TryEnqueue(() =>
                    {
                        task.State = UploadTaskStateEnum.Failed;
                    });
                    return;
                }
                dtoInitNoCallback = JsonConvert.DeserializeObject<ProResponseDTO<OpenUploadInitNoCallbackDTO>>(resInit.Content);
                if (dtoInitNoCallback is null || !dtoInitNoCallback.State || dtoInitNoCallback.Data is null)
                {
                    App.DispatcherQueue?.TryEnqueue(() =>
                    {
                        task.State = UploadTaskStateEnum.Failed;
                    });
                    return;
                }
                fNoCallback = dtoInitNoCallback.Data;
                if (fNoCallback.Status == 2)
                {
                    App.DispatcherQueue?.TryEnqueue(() =>
                    {
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
                        App.DispatcherQueue?.TryEnqueue(() =>
                        {
                            task.State = UploadTaskStateEnum.Failed;
                        });
                        return;
                    }
                    dtoInitNoCallback = JsonConvert.DeserializeObject<ProResponseDTO<OpenUploadInitNoCallbackDTO>>(resInit.Content);
                    if (dtoInitNoCallback is null || !dtoInitNoCallback.State || dtoInitNoCallback.Data is null)
                    {
                        App.DispatcherQueue?.TryEnqueue(() =>
                        {
                            task.State = UploadTaskStateEnum.Failed;
                        });
                        return;
                    }
                    fNoCallback = dtoInitNoCallback.Data;
                    if (fNoCallback.Status == 2)
                    {
                        App.DispatcherQueue?.TryEnqueue(() =>
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
                        dtoInit = JsonConvert.DeserializeObject<ProResponseDTO<OpenUploadInitDTO>>(resInit.Content);
                        f = dtoInit?.Data;
                        return;
                    }
                    else
                    {
                        App.DispatcherQueue?.TryEnqueue(() =>
                        {
                            task.State = UploadTaskStateEnum.Failed;
                        });
                        return;
                    }
                }
                else if (fNoCallback.Status == 1)
                {
                    dtoInit = JsonConvert.DeserializeObject<ProResponseDTO<OpenUploadInitDTO>>(resInit.Content);
                    f = dtoInit?.Data;
                }
                if (f is null)
                {
                    App.DispatcherQueue?.TryEnqueue(() =>
                    {
                        task.State = UploadTaskStateEnum.Failed;
                    });
                    return;
                }
                bucket = f.Bucket;
                objectId = f.Object;
                App.DispatcherQueue?.TryEnqueue(() =>
                {
                    task.PickCode = f.PickCode;
                    task.Bucket = bucket;
                    task.Object = objectId;
                });
                if (f.Callback is not null && f.Callback.CallbackVar is not null)
                {
                    callback = f.Callback.Callback;
                    callbackVars = JsonConvert.DeserializeObject<Dictionary<string, string>>(f.Callback.CallbackVar);
                    App.DispatcherQueue?.TryEnqueue(() =>
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
                App.DispatcherQueue?.TryEnqueue(() =>
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
                    App.DispatcherQueue?.TryEnqueue(() =>
                    {
                        task.State = UploadTaskStateEnum.Failed;
                    });
                    return;
                }
                var dtoToken = JsonConvert.DeserializeObject<ProResponseDTO<OpenUploadGetTokenDTO>>(resToken.Content);
                if (dtoToken is null || !dtoToken.State || dtoToken.Data is null || dtoToken.Data.Endpoint.IsBlank())
                {
                    App.DispatcherQueue?.TryEnqueue(() =>
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
                App.DispatcherQueue?.TryEnqueue(() =>
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
            _uploadingPk = task.PickCode ?? string.Empty;
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
                    var callbackDto = JsonConvert.DeserializeObject<AliyunOssCallbackDTO>(callback);
                    if (callbackDto is null)
                    {
                        App.DispatcherQueue?.TryEnqueue(() =>
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
                    PutObjectRequest request = new PutObjectRequest(bucket, objectId, File.OpenRead(localFilename))
                    {
                        StreamTransferProgress = streamProgressCallback,
                        Metadata = callbackMeta
                    };
                    var result = client.PutObject(request);
                    responseContent = GetCallbackResponse(result);
                }
                // 分片上传
                else
                {
                    var reqResume = new RestRequest(ApiResource.OpenUploadResume);
                    reqResume.AddParameter("file_size", $"{fileSize}");
                    reqResume.AddParameter("target", target);
                    reqResume.AddParameter("fileid", fileid);
                    reqResume.AddParameter("pick_code", task.PickCode);
                    var resResume = await App.ProApiClient.PostAsync(reqResume);
                    if (!resResume.IsSuccessful || resResume.Content.IsBlank())
                    {
                        App.DispatcherQueue?.TryEnqueue(() =>
                        {
                            task.State = UploadTaskStateEnum.Failed;
                        });
                        return;
                    }
                    var dto = JsonConvert.DeserializeObject<ProResponseDTO<object?>>(resResume.Content);
                    if (dto is null || dto.State != true)
                    {
                        App.DispatcherQueue?.TryEnqueue(() =>
                        {
                            task.State = UploadTaskStateEnum.Failed;
                        });
                        return;
                    }
                    var dtoResume = JsonConvert.DeserializeObject<ProResponseDTO<OpenUploadResumeDTO>>(resResume.Content);
                    if (dtoResume is null || dtoResume.Data is null || dtoResume.Data.Callback is null)
                    {
                        App.DispatcherQueue?.TryEnqueue(() =>
                        {
                            task.State = UploadTaskStateEnum.Failed;
                        });
                        return;
                    }
                    if (dtoResume.Data.Callback.Callback.IsBlank() || dtoResume.Data.Callback.CallbackVar.IsBlank())
                    {
                        App.DispatcherQueue?.TryEnqueue(() =>
                        {
                            task.State = UploadTaskStateEnum.Failed;
                        });
                        return;
                    }
                    _uploadingPk = dtoResume.Data.PickCode;
                    bucket = dtoResume.Data.Bucket;
                    objectId = dtoResume.Data.Object;
                    callback = dtoResume.Data.Callback.Callback;
                    callbackVars = JsonConvert.DeserializeObject<Dictionary<string, string>>(dtoResume.Data.Callback.CallbackVar);
                    var callbackDto = JsonConvert.DeserializeObject<AliyunOssCallbackDTO>(callback);
                    if (callbackDto is null || callbackVars is null)
                    {
                        App.DispatcherQueue?.TryEnqueue(() =>
                        {
                            task.State = UploadTaskStateEnum.Failed;
                        });
                        return;
                    }
                    App.DispatcherQueue?.TryEnqueue(() =>
                    {
                        task.PickCode = _uploadingPk;
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

                    var uploadId = "";
                    try
                    {
                        //var request = new InitiateMultipartUploadRequest(bucket, objectId);
                        var result = await InitiateMultipartUploadAsync(endpoint, bucket, objectId, accessKeyId, accessKeySecret, securityToken);
                        uploadId = result.UploadId;
                        Debug.WriteLine("Init multi part upload succeeded");
                        Debug.WriteLine("Upload Id:{0}", result.UploadId);
                    }
                    catch (Exception ex)
                    {
                        await LogHelper.Error(ex);
                    }
                    var partSize = 100 * 1024;
                    var fi = new FileInfo(localFilename);
                    var partCount = fileSize / partSize;
                    if (fileSize % partSize != 0)
                    {
                        partCount++;
                    }
                    var partETags = new List<PartETag>();
                    try
                    {
                        App.DispatcherQueue?.TryEnqueue(() =>
                        {
                            task.State = UploadTaskStateEnum.Uploading;
                            task.Progress = partETags.Count * 1.0 / partCount;
                        });
                        using (var fs = File.Open(localFilename, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            for (var i = 0; i < partCount; i++)
                            {
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
                                Debug.WriteLine("finish {0}/{1}", partETags.Count, partCount);
                                App.DispatcherQueue?.TryEnqueue(() =>
                                {
                                    task.Progress = partETags.Count * 1.0 / partCount;
                                });
                            }
                            Debug.WriteLine("Put multi part upload succeeded");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Put multi part upload failed, {0}", ex.Message);
                    }

                    try
                    {
                        var completeMultipartUploadRequest = new CompleteMultipartUploadRequest(bucket, objectId, uploadId);
                        foreach (var partETag in partETags)
                        {
                            completeMultipartUploadRequest.PartETags.Add(partETag);
                        }
                        completeMultipartUploadRequest.Metadata = callbackMeta;
                        var result = client.CompleteMultipartUpload(completeMultipartUploadRequest);
                        Debug.WriteLine("complete multi part succeeded");
                        responseContent = GetCallbackResponse(result);
                        App.DispatcherQueue?.TryEnqueue(() =>
                        {
                            task.State = UploadTaskStateEnum.Completed;
                            task.Progress = 1;
                        });
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("complete multi part failed, {0}", ex.Message);
                    }
                    //UploadObjectRequest request = new UploadObjectRequest(bucket, objectId, localFilename)
                    //{
                    //    PartSize = 1 * 1024 * 1024,
                    //    ParallelThreadCount = 3,
                    //    CheckpointDir = checkpointDir,
                    //    StreamTransferProgress = streamProgressCallback,
                    //    Metadata = callbackMeta,
                    //};
                    //var result = client.ResumableUploadObject(request);
                }
            }
            catch (Exception ex)
            {
                await LogHelper.Error(ex);
            }

            try
            {
                Debug.WriteLine($"===>res:{responseContent}");
                var uploadRes = JsonConvert.DeserializeObject<ProResponseDTO<object?>>(responseContent);
                if (uploadRes is null)
                {
                    return;
                }
                if (uploadRes.State == true)
                {
                    App.DispatcherQueue?.TryEnqueue(() =>
                    {
                        App.ShowMessageBar($"上传成功", "信息", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success, autoClose: TimeSpan.FromSeconds(5));
                    });
                }
                else
                {
                    App.DispatcherQueue?.TryEnqueue(() =>
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

        private void streamProgressCallback(object? sender, StreamTransferProgressArgs args)
        {
            var p = args.TransferredBytes * 1.0f / args.TotalBytes;
            if (_uploadingPk.IsNotBlank())
            {
                App.DispatcherQueue?.TryEnqueue(() =>
                {
                    var item = UploadItems.FirstOrDefault(x => x.PickCode == _uploadingPk);
                    if (item is not null)
                    {
                        item.Progress = p;
                    }
                });
                var col = _db.GetCollection<UploadTaskEntity>(CollectionResource.UploadTask);
                var find = col.Query().Where(x => x.PickCode == _uploadingPk).SingleOrDefault();
                if (find is not null)
                {
                    find.Progress = p;
                }
            }
        }

        public async Task AddTask(string filePath, string targetDirId = "0")
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
                await UploadQueue.Writer.WriteAsync(new UploadItemModel
                {
                    Name = Path.GetFileName(filePath),
                    Size = new FileInfo(filePath).Length,
                    ParentId = targetDirId,
                    FilePath = filePath,
                    State = UploadTaskStateEnum.Queued,
                });
            }
            catch (Exception ex)
            {
                await LogHelper.Error(ex);
            }
            finally
            {
            }
        }

        [RelayCommand]
        public async Task ClearFinish()
        {
        }

        [RelayCommand]
        public async Task PauseAll()
        {
        }

        [RelayCommand]
        public async Task StartAll()
        {
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
