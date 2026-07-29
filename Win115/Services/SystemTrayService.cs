using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Win115.Services
{
    internal sealed class SystemTrayService : IDisposable
    {
        private const int IconId = 1;
        private const uint NimAdd = 0;
        private const uint NimDelete = 2;
        private const uint NifMessage = 1;
        private const uint NifIcon = 2;
        private const uint NifTip = 4;
        private const uint RestoreCommand = 1;
        private const uint ExitCommand = 2;
        private const int GwlpWndProc = -4;

        private readonly nint _windowHandle;
        private readonly WindowProc _windowProc;
        private readonly nint _originalWindowProc;
        private readonly uint _callbackMessage;
        private readonly uint _taskbarCreatedMessage;
        private readonly nint _iconHandle;
        private NotifyIconData _iconData;
        private bool _disposed;

        public event Action? RestoreRequested;
        public event Action? ExitRequested;

        public SystemTrayService(nint windowHandle)
        {
            _windowHandle = windowHandle;
            _windowProc = WndProc;
            _originalWindowProc = SetWindowLongPtr(_windowHandle, GwlpWndProc, Marshal.GetFunctionPointerForDelegate(_windowProc));
            _callbackMessage = RegisterWindowMessage("Win115.SystemTray.Callback");
            _taskbarCreatedMessage = RegisterWindowMessage("TaskbarCreated");
            var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "favicon.ico");
            _iconHandle = LoadImage(0, iconPath, 1, 0, 0, 0x10);
            AddIcon();
        }

        private void AddIcon()
        {
            _iconData = new NotifyIconData
            {
                Size = (uint)Marshal.SizeOf<NotifyIconData>(),
                WindowHandle = _windowHandle,
                Id = IconId,
                Flags = NifMessage | NifIcon | NifTip,
                CallbackMessage = _callbackMessage,
                IconHandle = _iconHandle,
                Tip = "115 Plus"
            };
            ShellNotifyIcon(NimAdd, ref _iconData);
        }

        private nint WndProc(nint hwnd, uint message, nint wParam, nint lParam)
        {
            if (message == _taskbarCreatedMessage)
            {
                AddIcon();
            }
            else if (message == _callbackMessage)
            {
                var mouseMessage = unchecked((uint)lParam.ToInt64());
                if (mouseMessage is 0x0202 or 0x0203)
                {
                    RestoreRequested?.Invoke();
                }
                else if (mouseMessage == 0x0205)
                {
                    ShowContextMenu();
                }
            }
            return CallWindowProc(_originalWindowProc, hwnd, message, wParam, lParam);
        }

        private void ShowContextMenu()
        {
            var menu = CreatePopupMenu();
            if (menu == 0) return;

            AppendMenu(menu, 0, RestoreCommand, "打开主窗口");
            AppendMenu(menu, 0x0800, 0, string.Empty);
            AppendMenu(menu, 0, ExitCommand, "退出");
            GetCursorPos(out var cursor);
            SetForegroundWindow(_windowHandle);
            var command = TrackPopupMenu(menu, 0x0102, cursor.X, cursor.Y, 0, _windowHandle, 0);
            DestroyMenu(menu);

            if (command == RestoreCommand) RestoreRequested?.Invoke();
            else if (command == ExitCommand) ExitRequested?.Invoke();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            ShellNotifyIcon(NimDelete, ref _iconData);
            if (_iconHandle != 0) DestroyIcon(_iconHandle);
            if (_originalWindowProc != 0) SetWindowLongPtr(_windowHandle, GwlpWndProc, _originalWindowProc);
        }

        private delegate nint WindowProc(nint hwnd, uint message, nint wParam, nint lParam);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NotifyIconData
        {
            public uint Size;
            public nint WindowHandle;
            public uint Id;
            public uint Flags;
            public uint CallbackMessage;
            public nint IconHandle;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string Tip;
            public uint State;
            public uint StateMask;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Info;
            public uint TimeoutOrVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string InfoTitle;
            public uint InfoFlags;
            public Guid GuidItem;
            public nint BalloonIcon;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Point { public int X; public int Y; }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "Shell_NotifyIconW")] private static extern bool ShellNotifyIcon(uint message, ref NotifyIconData data);
        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "LoadImageW")] private static extern nint LoadImage(nint instance, string name, uint type, int width, int height, uint load);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] private static extern nint SetWindowLongPtr(nint hwnd, int index, nint newLong);
        [DllImport("user32.dll", EntryPoint = "CallWindowProcW")] private static extern nint CallWindowProc(nint previous, nint hwnd, uint message, nint wParam, nint lParam);
        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegisterWindowMessageW")] private static extern uint RegisterWindowMessage(string value);
        [DllImport("user32.dll")] private static extern nint CreatePopupMenu();
        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "AppendMenuW")] private static extern bool AppendMenu(nint menu, uint flags, uint id, string text);
        [DllImport("user32.dll")] private static extern uint TrackPopupMenu(nint menu, uint flags, int x, int y, int reserved, nint hwnd, nint rect);
        [DllImport("user32.dll")] private static extern bool DestroyMenu(nint menu);
        [DllImport("user32.dll")] private static extern bool DestroyIcon(nint icon);
        [DllImport("user32.dll")] private static extern bool GetCursorPos(out Point point);
        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint hwnd);
    }
}
