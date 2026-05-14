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

        public static string FormatDownloadSpeed(long? bytes)
        {
            return FormatFileSize(bytes) + "/s";
        }
    }
}
