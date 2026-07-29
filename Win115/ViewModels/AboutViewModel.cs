using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.Controls;
using Tanovo.ExtensionMethods;
using Win115.Models;

namespace Win115.ViewModels
{
    public partial class AboutViewModel : ObservableRecipient
    {
        [ObservableProperty]
        public partial UserInfoModel User { get; set; }

        [ObservableProperty]
        public partial MarkdownConfig MdConfig { get; set; } = new MarkdownConfig();

        [ObservableProperty]
        public partial string AboutMdText { get; set; }

        public AboutViewModel(UserInfoModel user)
        {
            User = user;

            AboutMdText = @"
# 115 Plus

基于 [115 开放平台](https://open.115.com/) 开发的第三方开源 Windows 桌面客户端，使用 **.NET 10** 和 **WinUI 3** 构建。

项目开源地址：https://github.com/haozekang/Win115

## 获取与更新

115 Plus 已上架 Microsoft Store，支持 x64 与 ARM64 架构。

程序启动后会在后台检查新版本。发现更新时，“关于”菜单会显示红色提示，也可以使用页面顶部的按钮手动检查并前往 Microsoft Store 更新。

## 主要功能

- 文件和文件夹浏览、搜索、排序、复制、移动、重命名与删除
- 列表和网格视图切换，图片快速预览与视频在线播放
- 文件及文件夹递归下载，支持暂停、恢复与失败重试
- 文件及文件夹上传，支持秒传检测、OSS 分片上传与失败重试
- 云下载任务管理、回收站管理和下载配额查看
- 深色与浅色主题跟随系统
- 系统托盘后台运行，可配置关闭按钮行为
- 传输任务持久化，重启程序后可继续管理

## 开发状态

项目仍在持续开发，以下能力尚在规划中：

- [ ] 播放进度记忆与恢复
- [ ] 视频字幕
- [ ] BT 种子解析下载
- [ ] 跨平台客户端

## 致谢

项目在界面设计和交互上参考了 [115-plus-desktop](https://github.com/lvzhenbo/115-plus-desktop)，感谢相关作者的贡献与支持。

## 许可证

本项目采用 MIT License 开源。
".StringTrim();
        }
    }
}
