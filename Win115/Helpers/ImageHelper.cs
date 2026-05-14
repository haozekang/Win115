using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace Win115.Helpers
{
    public static class ImageHelper
    {
        public static async Task<BitmapImage> ConvertToBitmapImageAsync(byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentNullException(nameof(data), "图像数据不能为空");

            var bitmapImage = new BitmapImage();

            // InMemoryRandomAccessStream 实现了 IDisposable，使用 using 确保释放
            using var stream = new InMemoryRandomAccessStream();

            // DataWriter 实现了 IDisposable，使用 using 确保释放
            using (var writer = new DataWriter(stream))
            {
                writer.WriteBytes(data);

                // FlushAsync 将缓冲区数据写入流
                await writer.StoreAsync();

                // DetachStream 防止 DataWriter Dispose 时关闭底层流
                writer.DetachStream();
            }

            // 重置流位置到起始处
            stream.Seek(0);

            // SetSourceAsync 从流中异步加载图像
            await bitmapImage.SetSourceAsync(stream);

            return bitmapImage;
        }

        public static async Task<BitmapImage> ToBitmapImageAsync(Bitmap bitmap)
        {
            using MemoryStream ms = new();

            // 保存到内存流（PNG 格式）
            bitmap.Save(ms, ImageFormat.Png);
            ms.Position = 0;

            BitmapImage bitmapImage = new();

            // WinUI3 必须使用 RandomAccessStream
            using var ras = ms.AsRandomAccessStream();
            await bitmapImage.SetSourceAsync(ras);

            return bitmapImage;
        }
    }
}
