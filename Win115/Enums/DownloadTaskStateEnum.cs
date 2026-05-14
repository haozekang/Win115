using System;
using System.Collections.Generic;
using System.Text;

namespace Win115.Enums
{
    public enum DownloadTaskStateEnum
    {
        Queued,       // 已入队
        Downloading,  // 下载中
        Paused,       // 已暂停
        Completed,    // 已完成
        Failed,       // 失败
        Canceled      // 已取消
    }
}
