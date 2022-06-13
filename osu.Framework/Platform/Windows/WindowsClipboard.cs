// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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

        // The bitmap file header should not be included in clipboard.
        // See https://docs.microsoft.com/en-us/windows/win32/dataxchg/standard-clipboard-formats for more details.
        private const int bitmap_file_header_length = 14;

        private static readonly byte[] bmp_header_field = { 0x42, 0x4D };

        public override string GetText()
        {
            return getClipboard(cf_unicodetext, bytes => Encoding.Unicode.GetString(bytes).TrimEnd('\0'));
        }

        public override void SetText(string selectedText)
        {
            int bytes = (selectedText.Length + 1) * 2;
            var source = Marshal.StringToHGlobalUni(selectedText);

            setClipboard(source, bytes, cf_unicodetext);
        }

        public override Image<TPixel> GetImage<TPixel>()
        {
            return getClipboard(cf_dib, bytes =>
            {
                byte[] buff = new byte[bytes.Length + bitmap_file_header_length];

                bmp_header_field.CopyTo(buff, 0);
                bytes.CopyTo(buff, bitmap_file_header_length);

                return Image.Load<TPixel>(buff);
            });
        }

        public override bool SetImage(Image image)
        {
            byte[] array;

            using (var stream = new MemoryStream())
            {
                var encoder = image.GetConfiguration().ImageFormatsManager.FindEncoder(BmpFormat.Instance);
                image.Save(stream, encoder);
                array = stream.ToArray().Skip(bitmap_file_header_length).ToArray();
            }

            IntPtr unmanagedPointer = Marshal.AllocHGlobal(array.Length);
            Marshal.Copy(array, 0, unmanagedPointer, array.Length);

            return setClipboard(unmanagedPointer, array.Length, cf_dib);
        }

        private static bool setClipboard(IntPtr pointer, int bytes, uint format)
        {
            bool success = false;

            try
            {
                if (!OpenClipboard(IntPtr.Zero))
                    return false;

                EmptyClipboard();

                // IMPORTANT: SetClipboardData requires memory that was acquired with GlobalAlloc using GMEM_MOVABLE.
                var hGlobal = GlobalAlloc(ghnd, (UIntPtr)bytes);

                try
                {
                    var target = GlobalLock(hGlobal);
                    if (target == IntPtr.Zero)
                        return false;

                    try
                    {
                        unsafe
                        {
                            Buffer.MemoryCopy((void*)pointer, (void*)target, bytes, bytes);
                        }
                    }
                    finally
                    {
                        if (target != IntPtr.Zero)
                            GlobalUnlock(target);

                        Marshal.FreeHGlobal(pointer);
                    }

                    if (SetClipboardData(format, hGlobal).ToInt64() != 0)
                    {
                        // IMPORTANT: SetClipboardData takes ownership of hGlobal upon success.
                        hGlobal = IntPtr.Zero;
                        success = true;
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

            return success;
        }

        private static T getClipboard<T>(uint format, Func<byte[], T> transform)
        {
            if (!IsClipboardFormatAvailable(format))
                return default;

            try
            {
                if (!OpenClipboard(IntPtr.Zero))
                    return default;

                IntPtr handle = GetClipboardData(format);
                if (handle == IntPtr.Zero)
                    return default;

                IntPtr pointer = IntPtr.Zero;

                try
                {
                    pointer = GlobalLock(handle);

                    if (pointer == IntPtr.Zero)
                        return default;

                    int size = GlobalSize(handle);
                    byte[] buff = new byte[size];

                    Marshal.Copy(pointer, buff, 0, size);

                    return transform(buff);
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
    }
}
