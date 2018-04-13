// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace osu.Framework.Platform.Windows
{
    public class WindowsClipboard : Clipboard
    {
        [DllImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsClipboardFormatAvailable(uint format);

        [DllImport("User32.dll")]
        private static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport("user32.dll")]
        private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

        [DllImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll")]
        private static extern bool EmptyClipboard();

        [DllImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseClipboard();

        [DllImport("Kernel32.dll")]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

        [DllImport("Kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("Kernel32.dll")]
        private static extern int GlobalSize(IntPtr hMem);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalFree(IntPtr hMem);

        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

        private const uint cf_unicodetext = 13U;

        public override string GetText()
        {
            if (!IsClipboardFormatAvailable(cf_unicodetext))
                return null;

            try
            {
                if (!OpenClipboard(IntPtr.Zero))
                    return null;

                IntPtr handle = GetClipboardData(cf_unicodetext);
                if (handle == IntPtr.Zero)
                    return null;

                IntPtr pointer = IntPtr.Zero;

                try
                {
                    pointer = GlobalLock(handle);

                    if (pointer == IntPtr.Zero)
                        return null;

                    int size = GlobalSize(handle);
                    byte[] buff = new byte[size];

                    Marshal.Copy(pointer, buff, 0, size);

                    return Encoding.Unicode.GetString(buff).TrimEnd('\0');
                }
                finally
                {
                    if (pointer != IntPtr.Zero)
                        GlobalUnlock(handle);
                }
            }
            finally
            {
                CloseClipboard();
            }
        }

        public override void SetText(string selectedText)
        {
            try
            {
                if (!OpenClipboard(IntPtr.Zero))
                    return;

                EmptyClipboard();

                uint bytes = ((uint)selectedText.Length + 1) * 2;

                var source = Marshal.StringToHGlobalUni(selectedText);

                const int gmem_movable = 0x0002;
                const int gmem_zeroinit = 0x0040;
                const int ghnd = gmem_movable | gmem_zeroinit;

                // IMPORTANT: SetClipboardData requires memory that was acquired with GlobalAlloc using GMEM_MOVABLE.
                var hGlobal = GlobalAlloc(ghnd, (UIntPtr)bytes);

                try
                {
                    var target = GlobalLock(hGlobal);
                    if (target == IntPtr.Zero)
                        return;

                    try
                    {
                        CopyMemory(target, source, bytes);
                    }
                    finally
                    {
                        if (target != IntPtr.Zero)
                            GlobalUnlock(target);

                        Marshal.FreeHGlobal(source);
                    }

                    if (SetClipboardData(cf_unicodetext, hGlobal).ToInt64() != 0)
                    {
                        // IMPORTANT: SetClipboardData takes ownership of hGlobal upon success.
                        hGlobal = IntPtr.Zero;
                    }
                }
                finally
                {
                    if (hGlobal != IntPtr.Zero)
                        GlobalFree(hGlobal);
                }
            }
            finally
            {
                CloseClipboard();
            }
        }
    }
}
