using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static QRCoder.PayloadGenerator;

namespace Win115.Helpers
{
    public class QRCodeHelper
    {
        public static Task<BitmapImage> GenerateQRCodeFromBase64(string data)
        {
            string base64 = data.Split(',')[1];
            byte[] bytes = Convert.FromBase64String(base64);
            return ImageHelper.ConvertToBitmapImageAsync(bytes);
        }

        public static Task<BitmapImage> CreateQrCodeForUrl(string url)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);
            var qrCodeAsBitmap = qrCode.GetGraphic(20);
            return ImageHelper.ToBitmapImageAsync(qrCodeAsBitmap);
        }
    }
}
