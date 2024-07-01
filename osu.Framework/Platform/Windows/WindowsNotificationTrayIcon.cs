// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using osu.Framework.Logging;

namespace osu.Framework.Platform.Windows
{
    /// <summary>
    /// A windows specific notification tray icon, 
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal partial class WindowsNotificationTrayIcon : NotificationTrayIcon
    {
        internal IWindowsWindow window = null!;

        private NOTIFYICONDATAW inner;

        internal WindowsNotificationTrayIcon(string text, Action? onClick, IWindow win)
        {

            if (win is not IWindowsWindow w)
            {
                throw new PlatformNotSupportedException();
            }

            window = w;
            Text = text;
            OnClick = onClick;

            NotifyIconFlags flags = NotifyIconFlags.NIF_MESSAGE | NotifyIconFlags.NIF_ICON | NotifyIconFlags.NIF_TIP | NotifyIconFlags.NIF_SHOWTIP;
            IntPtr iconHandle = IntPtr.Zero;
            IntPtr hwnd;

            if (window is SDL3WindowsWindow w3)
            {
                hwnd = w3.WindowHandle;
                if (w3.largeIcon is not null)
                {
                    iconHandle = w3.largeIcon.Handle;
                }
                else if (w3.smallIcon is not null)
                {
                    iconHandle = w3.smallIcon.Handle;
                }
            }
            else if (window is SDL2WindowsWindow w2)
            {
                hwnd = w2.WindowHandle;
                if (w2.largeIcon is not null)
                {
                    iconHandle = w2.largeIcon.Handle;
                }
                else if (w2.smallIcon is not null)
                {
                    iconHandle = w2.smallIcon.Handle;
                }
            }
            else
            {
                throw new PlatformNotSupportedException("Invalid windowing backend");
            }

            inner = new NOTIFYICONDATAW
            {
                cbSize = Marshal.SizeOf(inner),
                uFlags = flags,
                hIcon = iconHandle,
                hWnd = hwnd,
                szTip = text,
                uCallbackMessage = TRAYICON
            };

            bool ret = Shell_NotifyIconW(NotifyIconAction.NIM_ADD, ref inner);

            inner.uTimeoutOrVersion = NOTIFYICON_VERSION_4;

            Shell_NotifyIconW(NotifyIconAction.NIM_SETVERSION, ref inner);

            if (!ret)
            {
                int err = Marshal.GetLastWin32Error();
                Logger.Log($"Error {err} while creating notification tray icon", LoggingTarget.Runtime, LogLevel.Error);
            }
        }

        public override void Dispose()
        {
            bool ret = Shell_NotifyIconW(NotifyIconAction.NIM_DELETE, ref inner);

            if (!ret)
            {
                int err = Marshal.GetLastWin32Error();
                Logger.Log($"Error {err} while removing notification tray icon", LoggingTarget.Runtime, LogLevel.Error);
            }
        }

        private const int NOTIFYICON_VERSION_4 = 4;

        internal const int TRAYICON = 0x0400 + 1024;
        internal const int WM_LBUTTONUP = 0x0202;
        internal const int WM_RBUTTONUP = 0x0205;
        internal const int WM_MBUTTONUP = 0x0208;
        internal const int NIN_SELECT = 0x400;

        internal static bool IsClick(long lParam)
        {
            switch ((short)lParam)
            {
                case WM_LBUTTONUP:
                case WM_RBUTTONUP:
                case WM_MBUTTONUP:
                case NIN_SELECT:
                    return true;
                default:
                    return false;
            }
        }

        [Flags]
        internal enum NotifyIconAction : uint
        {
            NIM_ADD = 0x00000000,
            NIM_DELETE = 0x00000002,
            NIM_SETVERSION = 0x00000004,
        }

        [DllImport("shell32.dll")]
        internal static extern bool Shell_NotifyIconW(NotifyIconAction dwMessage, [In] ref NOTIFYICONDATAW pnid);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct NOTIFYICONDATAW
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uID;
        public NotifyIconFlags uFlags;
        public int uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public int dwState;
        public int dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public int uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public int dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

        [Flags]
        public enum NotifyIconFlags : uint
        {
            NIF_MESSAGE = 0x00000001,
            NIF_ICON = 0x00000002,
            NIF_TIP = 0x00000004,
            NIF_STATE = 0x00000008,
            NIF_INFO = 0x00000010,
            NIF_GUID = 0x00000020,
            NIF_SHOWTIP = 0x00000080
        }

        internal enum ToolTipIcon
        {
            None = 0,
            Info = 1,
            Warning = 2,
            Error = 3
        }
    }
}
