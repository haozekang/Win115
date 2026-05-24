using Microsoft.UI.Xaml.Documents;
using Org.BouncyCastle.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Win115.Helpers
{
    public static class StringHelper
    {
        /// <summary>
        /// 将字节数格式化为易读的文件大小字符串。
        /// 例如：1024 -> 1 KB，1536 -> 1.5 KB
        /// </summary>
        /// <param name="bytes">文件大小（单位：Byte）</param>
        /// <returns>格式化后的字符串</returns>
        public static string FormatFileSize(long? bytes)
        {
            if (bytes is null)
                throw new ArgumentOutOfRangeException(nameof(bytes), "文件大小不能为null");
            if (bytes < 0)
                throw new ArgumentOutOfRangeException(nameof(bytes), "文件大小不能为负数");

            string[] units = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };

            double size = bytes.Value;
            int unitIndex = 0;

            while (size >= 1024 && unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }

            // B 不显示小数，其它单位最多保留两位小数，并自动去掉末尾的 0
            return unitIndex == 0
                ? $"{bytes} B"
                : $"{size:0.##} {units[unitIndex]}";
        }

        public static string FormatTimeSpan(TimeSpan? time, int maxUnits = 2)
        {
            if (time is null || time == TimeSpan.Zero)
            {
                return string.Empty;
            }
            var abs = time.Value.Duration();
            var parts = new List<string>(4);

            if (abs.Days > 0) parts.Add($"{abs.Days}天");
            if (abs.Hours > 0) parts.Add($"{abs.Hours}小时");
            if (abs.Minutes > 0) parts.Add($"{abs.Minutes}分");
            if (abs.Seconds > 0) parts.Add($"{abs.Seconds}秒");

            if (parts.Count == 0)
            {
                return string.Empty;
            }
            if (parts.Count > maxUnits) parts = parts.Take(maxUnits).ToList();

            return string.Concat(parts);
        }

        public static string FormatDownloadSpeed(long? bytes)
        {
            return FormatFileSize(bytes) + "/s";
        }
    }
}
