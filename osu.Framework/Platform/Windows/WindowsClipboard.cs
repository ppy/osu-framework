// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats.Bmp;

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

        private const uint cf_dib = 8U;
        private const uint cf_unicodetext = 13U;

        private const int gmem_movable = 0x02;
        private const int gmem_zeroinit = 0x40;
        private const int ghnd = gmem_movable | gmem_zeroinit;

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

                // IMPORTANT: SetClipboardData requires memory that was acquired with GlobalAlloc using GMEM_MOVABLE.
                var hGlobal = GlobalAlloc(ghnd, (UIntPtr)bytes);

                try
                {
                    var target = GlobalLock(hGlobal);
                    if (target == IntPtr.Zero)
                        return;

                    try
                    {
                        unsafe
                        {
                            Buffer.MemoryCopy((void*)source, (void*)target, bytes, bytes);
                        }
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

        public override void SetImage(Image image)
        {
            byte[] array;

            using (var stream = new MemoryStream())
            {
                var encoder = image.GetConfiguration().ImageFormatsManager.FindEncoder(BmpFormat.Instance);
                image.Save(stream, encoder);
                array = stream.ToArray().Skip(14).ToArray();
            }

            try
            {
                if (!OpenClipboard(IntPtr.Zero))
                    return;

                EmptyClipboard();

                IntPtr unmanagedPointer = Marshal.AllocHGlobal(array.Length);
                Marshal.Copy(array, 0, unmanagedPointer, array.Length);

                var hGlobal = GlobalAlloc(ghnd, (UIntPtr)array.Length);

                try
                {
                    var target = GlobalLock(hGlobal);
                    if (target == IntPtr.Zero)
                        return;

                    try
                    {
                        unsafe
                        {
                            Buffer.MemoryCopy((void*)unmanagedPointer, (void*)target, array.Length, array.Length);
                        }
                    }
                    finally
                    {
                        if (target != IntPtr.Zero)
                            GlobalUnlock(target);

                        Marshal.FreeHGlobal(unmanagedPointer);
                    }

                    if (SetClipboardData(cf_dib, hGlobal).ToInt64() != 0)
                    {
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
