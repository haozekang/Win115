using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Tanovo.ExtensionMethods;
using Win115.Dtos;
using Win115.Helpers;
using Win115.Models;
using Win115.Properties;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using WinRT.Interop;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Win115.ViewModels
{
    public partial class ViewImagesViewModel : ObservableRecipient
    {
        private readonly MyFilesViewModel _myFilesViewModel;

        /// <summary>
        /// 剪贴板中图像的流，需在复制后继续保持存活，否则其他程序粘贴时会失败
        /// </summary>
        private InMemoryRandomAccessStream? _clipboardStream;

        /// <summary>
        /// 已删除图片的Id，避免分页重新加载时把已删除的图片带回来
        /// </summary>
        private readonly HashSet<string> _deletedImageIds = new();

        private static readonly HttpClient _httpClient = CreateHttpClient();

        [ObservableProperty]
        public partial UserInfoModel User { get; set; }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(PrevImageCommand))]
        [NotifyCanExecuteChangedFor(nameof(NextImageCommand))]
        [NotifyCanExecuteChangedFor(nameof(SaveImageAsCommand))]
        [NotifyCanExecuteChangedFor(nameof(CopyCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
        public partial MyFileItemModel? SelectedImageItem { get; set; } = null;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ImageLoadingVisibility))]
        [NotifyPropertyChangedFor(nameof(ImageLoadedVisibility))]
        public partial bool IsImageLoading { get; set; } = true;

        public Visibility ImageLoadingVisibility => IsImageLoading ? Visibility.Visible : Visibility.Collapsed;

        public Visibility ImageLoadedVisibility => IsImageLoading ? Visibility.Collapsed : Visibility.Visible;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(PrevImageCommand))]
        [NotifyCanExecuteChangedFor(nameof(NextImageCommand))]
        [NotifyCanExecuteChangedFor(nameof(SaveImageAsCommand))]
        [NotifyCanExecuteChangedFor(nameof(CopyCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
        public partial bool IsBusy { get; set; } = false;

        [ObservableProperty]
        public partial IncrementalLoadingCollection<MyFileImageIncrementalSource, MyFileItemModel> ImageFileItems { get; set; }

        /// <summary>
        /// 图片窗口句柄，供文件选择器等 WinRT 交互对象初始化使用
        /// </summary>
        public nint WindowHandle { get; set; } = App.WindowHandle;

        /// <summary>
        /// 图片窗口的 XamlRoot 提供器，供弹出对话框使用
        /// </summary>
        public Func<XamlRoot?>? XamlRootProvider { get; set; }

        private XamlRoot? DialogXamlRoot => XamlRootProvider?.Invoke() ?? App.XamlRoot;

        /// <summary>
        /// 当前图片在图片列表中的序号，未选中时为 -1
        /// </summary>
        private int CurrentIndex => SelectedImageItem is null ? -1 : ImageFileItems.IndexOf(SelectedImageItem);

        public ViewImagesViewModel(UserInfoModel user, MyFilesViewModel myFilesViewModel)
        {
            User = user;
            _myFilesViewModel = myFilesViewModel;
            ImageFileItems = myFilesViewModel.ImageFileItems;
            if (ImageFileItems.Count == 0)
            {
                _ = ImageFileItems.LoadMoreItemsAsync(30);
            }
            // 列表变化（加载更多、删除）后需要重新计算上一张/下一张是否可用
            ImageFileItems.CollectionChanged += OnImageFileItemsChanged;
        }

        private void OnImageFileItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems is not null)
            {
                foreach (var item in e.NewItems.OfType<MyFileItemModel>().ToList())
                {
                    if (item.Id.IsNotBlank() && _deletedImageIds.Contains(item.Id!))
                    {
                        // 延迟移除，避免在集合变更通知过程中再次修改集合
                        App.DispatcherQueue?.TryEnqueue(() => ImageFileItems.Remove(item));
                    }
                }
            }
            NotifyImageCommandStateChanged();
        }

        /// <summary>
        /// 重新计算图片相关命令的可用状态；窗口的 x:Bind 绑定在首次激活时才初始化，
        /// 视图初始化完成后需要再调用一次，否则按钮可能停留在绑定前的禁用状态
        /// </summary>
        public void NotifyImageCommandStateChanged()
        {
            PrevImageCommand.NotifyCanExecuteChanged();
            NextImageCommand.NotifyCanExecuteChanged();
            SaveImageAsCommand.NotifyCanExecuteChanged();
            CopyCommand.NotifyCanExecuteChanged();
            DeleteCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// 是否存在可操作的图片
        /// </summary>
        private bool CanOperateImage() => !IsBusy && SelectedImageItem is not null;

        /// <summary>
        /// 是否存在上一张图片
        /// </summary>
        private bool CanPrevImage() => !IsBusy && CurrentIndex > 0;

        /// <summary>
        /// 是否存在下一张图片
        /// </summary>
        private bool CanNextImage() => !IsBusy && CurrentIndex >= 0
            && (CurrentIndex < ImageFileItems.Count - 1 || ImageFileItems.HasMoreItems);

        /// <summary>
        /// 切换到上一张图片
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanPrevImage))]
        private void PrevImage()
        {
            var index = CurrentIndex;
            if (index <= 0)
            {
                return;
            }
            SelectedImageItem = ImageFileItems[index - 1];
        }

        /// <summary>
        /// 切换到下一张图片
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanNextImage))]
        private async Task NextImage()
        {
            var index = CurrentIndex;
            if (index < 0)
            {
                return;
            }
            // 当前已是已加载的最后一张，先尝试加载下一页图片
            if (index >= ImageFileItems.Count - 1)
            {
                if (!ImageFileItems.HasMoreItems)
                {
                    return;
                }
                try
                {
                    await ImageFileItems.LoadMoreItemsAsync(30);
                }
                catch (Exception ex)
                {
                    await LogHelper.Error(ex);
                }
                NotifyImageCommandStateChanged();
                index = CurrentIndex;
            }
            if (index < 0 || index >= ImageFileItems.Count - 1)
            {
                return;
            }
            SelectedImageItem = ImageFileItems[index + 1];
        }

        /// <summary>
        /// 将当前图片另存为指定格式
        /// </summary>
        /// <param name="format">JPEG/PNG/BMP/TIF</param>
        [RelayCommand(CanExecute = nameof(CanSaveImageAs))]
        private async Task SaveImageAs(string? format)
        {
            var item = SelectedImageItem;
            if (item is null)
            {
                return;
            }
            var (extension, description, encoderId) = GetImageFormat(format);
            IsBusy = true;
            try
            {
                var bytes = await GetOriginalImageBytesAsync(item);
                if (bytes is null || bytes.Length == 0)
                {
                    await App.ShowMessageBar("图片内容获取失败！", "错误", InfoBarSeverity.Error, autoClose: TimeSpan.FromSeconds(5));
                    return;
                }
                var suggestedName = Path.GetFileNameWithoutExtension(item.Name ?? string.Empty);
                FileSavePicker picker = new()
                {
                    SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                    SuggestedFileName = suggestedName.IsBlank() ? "image" : suggestedName,
                };
                picker.FileTypeChoices.Add(description, new List<string> { extension });
                InitializeWithWindow.Initialize(picker, WindowHandle);
                var file = await picker.PickSaveFileAsync();
                if (file is null)
                {
                    return;
                }
                await ConvertAndSaveImageAsync(bytes, file, encoderId);
                await App.ShowMessageBar($"图片已保存到{file.Path}", "成功", InfoBarSeverity.Success, autoClose: TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                await LogHelper.Error(ex);
                await App.ShowMessageBar("图片保存失败！", "错误", InfoBarSeverity.Error, autoClose: TimeSpan.FromSeconds(5));
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanSaveImageAs(string? format) => CanOperateImage();

        /// <summary>
        /// 复制当前图片到剪贴板
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanOperateImage))]
        private async Task Copy()
        {
            var item = SelectedImageItem;
            if (item is null)
            {
                return;
            }
            IsBusy = true;
            try
            {
                var bytes = await GetOriginalImageBytesAsync(item);
                if (bytes is null || bytes.Length == 0)
                {
                    await App.ShowMessageBar("图片内容获取失败！", "错误", InfoBarSeverity.Error, autoClose: TimeSpan.FromSeconds(5));
                    return;
                }
                var stream = new InMemoryRandomAccessStream();
                using (var writer = new DataWriter(stream))
                {
                    writer.WriteBytes(bytes);
                    await writer.StoreAsync();
                    writer.DetachStream();
                }
                stream.Seek(0);
                DataPackage package = new()
                {
                    RequestedOperation = DataPackageOperation.Copy,
                };
                package.SetBitmap(RandomAccessStreamReference.CreateFromStream(stream));
                Clipboard.SetContent(package);
                Clipboard.Flush();
                var previous = _clipboardStream;
                _clipboardStream = stream;
                previous?.Dispose();
                await App.ShowMessageBar("复制成功！", "成功", InfoBarSeverity.Success, autoClose: TimeSpan.FromSeconds(3));
            }
            catch (Exception ex)
            {
                await LogHelper.Error(ex);
                await App.ShowMessageBar("复制失败！", "错误", InfoBarSeverity.Error, autoClose: TimeSpan.FromSeconds(5));
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 删除当前图片（删除后可在回收站恢复）
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanOperateImage))]
        private async Task Delete()
        {
            var item = SelectedImageItem;
            if (item is null || item.Id.IsBlank())
            {
                return;
            }
            ContentDialog dialog = new()
            {
                XamlRoot = DialogXamlRoot,
                Title = "警告",
                Content = $"确认删除图片“{item.Name}”吗？删除后可在回收站进行恢复。",
                PrimaryButtonText = "确认",
                SecondaryButtonText = "取消",
                DefaultButton = ContentDialogButton.Secondary,
            };
            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
            {
                return;
            }
            IsBusy = true;
            try
            {
                var req = new RestRequest(ApiResource.OpenUfileDelete);
                req.AddOrUpdateParameter("file_ids", item.Id);
                req.AddOrUpdateParameter("parent_id", item.ParentId.IsNotBlank() ? item.ParentId! : "0");
                req.AlwaysMultipartFormData = true;
                var res = await App.ProApiClient.PostAsync(req);
                if (!res.IsSuccessful || res.Content.IsBlank())
                {
                    await App.ShowMessageBar("删除失败！", "错误", InfoBarSeverity.Error, autoClose: TimeSpan.FromSeconds(5));
                    return;
                }
                var dto = JsonSerializer.Deserialize<ProResponseDTO<string[]?>>(res.Content);
                if (dto is null || !dto.State)
                {
                    await App.ShowMessageBar(dto?.Message.IsNotBlank() == true ? dto.Message! : "删除失败！", "错误", InfoBarSeverity.Error, autoClose: TimeSpan.FromSeconds(5));
                    return;
                }
                RemoveImage(item);
                await App.ShowMessageBar("删除成功！", "成功", InfoBarSeverity.Success, autoClose: TimeSpan.FromSeconds(3));
                // 主界面的文件列表同步刷新（后台进行，不阻塞图片窗口）
                _ = RefreshFilesAsync();
            }
            catch (Exception ex)
            {
                await LogHelper.Error(ex);
                await App.ShowMessageBar("删除失败！", "错误", InfoBarSeverity.Error, autoClose: TimeSpan.FromSeconds(5));
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 从列表中移除已删除的图片，并自动切换到相邻图片
        /// </summary>
        private void RemoveImage(MyFileItemModel item)
        {
            var index = ImageFileItems.IndexOf(item);
            if (item.Id.IsNotBlank())
            {
                _deletedImageIds.Add(item.Id!);
            }
            if (index >= 0)
            {
                ImageFileItems.Remove(item);
            }
            if (ImageFileItems.Count == 0)
            {
                SelectedImageItem = null;
                IsImageLoading = false;
            }
            else
            {
                IsImageLoading = true;
                SelectedImageItem = ImageFileItems[index < 0 ? 0 : Math.Min(index, ImageFileItems.Count - 1)];
            }
            NotifyImageCommandStateChanged();
        }

        private async Task RefreshFilesAsync()
        {
            try
            {
                await _myFilesViewModel.RefreshFiles();
            }
            catch (Exception ex)
            {
                await LogHelper.Error(ex);
            }
        }

        /// <summary>
        /// 获取图片原始数据，优先按提取码取下载地址，失败时回退到图片直链
        /// </summary>
        private static async Task<byte[]?> GetOriginalImageBytesAsync(MyFileItemModel item)
        {
            var urls = new List<string>();
            if (item.PickCode.IsNotBlank())
            {
                var url = await GetDownloadUrlAsync(item.PickCode!);
                if (url.IsNotBlank())
                {
                    urls.Add(url!);
                }
            }
            if (item.OriginalUrl.IsNotBlank() && !urls.Contains(item.OriginalUrl!))
            {
                urls.Add(item.OriginalUrl!);
            }
            foreach (var url in urls)
            {
                var bytes = await DownloadBytesAsync(url);
                if (bytes is not null && bytes.Length > 0)
                {
                    return bytes;
                }
            }
            return null;
        }

        private static async Task<string?> GetDownloadUrlAsync(string pickCode)
        {
            try
            {
                var req = new RestRequest(ApiResource.OpenUfileDownurl);
                req.AddOrUpdateParameter("pick_code", pickCode);
                req.AlwaysMultipartFormData = true;
                var res = await App.ProApiClient.PostAsync(req);
                if (!res.IsSuccessful || res.Content.IsBlank())
                {
                    return null;
                }
                var dto = JsonSerializer.Deserialize<ProResponseDTO<Dictionary<string, OpenUfileDownurlDTO?>?>>(res.Content);
                return dto is { State: true, Data.Count: > 0 } ? dto.Data.First().Value?.Url?.Url : null;
            }
            catch (Exception ex)
            {
                await LogHelper.Error(ex);
                return null;
            }
        }

        private static async Task<byte[]?> DownloadBytesAsync(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return null;
            }
            try
            {
                using var res = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseContentRead);
                if (!res.IsSuccessStatusCode)
                {
                    return null;
                }
                return await res.Content.ReadAsByteArrayAsync();
            }
            catch (Exception ex)
            {
                await LogHelper.Error(ex);
                return null;
            }
        }

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(60),
            };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36 Edg/140.0.0.0");
            return client;
        }

        private static (string Extension, string Description, Guid EncoderId) GetImageFormat(string? format)
        {
            return format?.ToUpperInvariant() switch
            {
                "PNG" => (".png", "PNG 图片", BitmapEncoder.PngEncoderId),
                "BMP" => (".bmp", "BMP 图片", BitmapEncoder.BmpEncoderId),
                "TIF" => (".tif", "TIF 图片", BitmapEncoder.TiffEncoderId),
                _ => (".jpg", "JPEG 图片", BitmapEncoder.JpegEncoderId),
            };
        }

        /// <summary>
        /// 解码图片数据并按指定编码器格式写入目标文件
        /// </summary>
        private static async Task ConvertAndSaveImageAsync(byte[] bytes, StorageFile file, Guid encoderId)
        {
            using var source = new InMemoryRandomAccessStream();
            using (var writer = new DataWriter(source))
            {
                writer.WriteBytes(bytes);
                await writer.StoreAsync();
                writer.DetachStream();
            }
            source.Seek(0);
            var decoder = await BitmapDecoder.CreateAsync(source);
            // JPEG 不支持透明通道
            var alphaMode = encoderId == BitmapEncoder.JpegEncoderId ? BitmapAlphaMode.Ignore : BitmapAlphaMode.Premultiplied;
            using var bitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, alphaMode);
            using var target = await file.OpenAsync(FileAccessMode.ReadWrite);
            target.Size = 0;
            var encoder = await BitmapEncoder.CreateAsync(encoderId, target);
            encoder.SetSoftwareBitmap(bitmap);
            await encoder.FlushAsync();
        }
    }
}
