// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;
using osu.Framework.Logging;
using SixLabors.ImageSharp;
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

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint RegisterClipboardFormatW(IntPtr lpszFormat);

        [DllImport("User32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EmptyClipboard();

        [DllImport("User32.dll", SetLastError = true)]
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

        private readonly Dictionary<string, uint> customFormats = new Dictionary<string, uint>();

        public override string? GetText()
        {
            return getClipboard(cf_unicodetext, bytes => Encoding.Unicode.GetString(bytes).TrimEnd('\0'));
        }

        public override Image<TPixel>? GetImage<TPixel>()
        {
            return getClipboard(cf_dib, bytes =>
            {
                byte[] buff = new byte[bytes.Length + bitmap_file_header_length];

                bmp_header_field.CopyTo(buff, 0);
                bytes.CopyTo(buff, bitmap_file_header_length);

                return Image.Load<TPixel>(buff);
            });
        }

        public override string? GetCustom(string mimeType)
        {
            uint formatIdentifier = getFormatIdentifier(mimeType);
            if (formatIdentifier == 0)
                return null;

            string? value = getClipboard(formatIdentifier, bytes => Encoding.Unicode.GetString(bytes).TrimEnd('\0'));
            if (value != null)
                return value;

            /*
             * Web browsers store clipboard entries with custom mime types differently, so we will need to check if an equivalent entry has been created.
             * https://github.com/w3c/editing/blob/gh-pages/docs/clipboard-pickling/explainer.md#os-interaction-format-naming
             */
            uint webCustomFormatMapIdentifier = getFormatIdentifier("Web Custom Format Map");

            if (webCustomFormatMapIdentifier != 0)
            {
                var webCustomFormatMap = getClipboard(webCustomFormatMapIdentifier, bytes =>
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(
                        Encoding.ASCII.GetString(bytes))
                );

                if (webCustomFormatMap?[mimeType] != null)
                {
                    uint webCustomFormatIdentifier = getFormatIdentifier(webCustomFormatMap[mimeType]);

                    if (webCustomFormatIdentifier != 0)
                    {
                        return getClipboard(webCustomFormatIdentifier, bytes => Encoding.UTF8.GetString(bytes).TrimEnd('\0'));
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Retrieves the identifier for to a given clipboard format, and registers a new format with the Win32 API if needed.
        /// </summary>
        /// <param name="formatName">Name of the clipboard format</param>
        /// <returns>Identifier of the created format. Will return 0 if registering the format failed.</returns>
        private uint getFormatIdentifier(string formatName)
        {
            if (customFormats.TryGetValue(formatName, out uint format))
            {
                return format;
            }

            IntPtr source = Marshal.StringToHGlobalUni(formatName);

            uint createdFormat = RegisterClipboardFormatW(source);

            GlobalFree(source);

            if (createdFormat == 0)
            {
                int error = Marshal.GetLastWin32Error();
                Logger.Log($"Failed to register clipboard format \"{formatName}\" with Win32 API with error code ${error}.", level: LogLevel.Error);

                return 0;
            }

            customFormats[formatName] = createdFormat;

            return createdFormat;
        }

        public override bool SetData(ClipboardData data)
        {
            if (data.IsEmpty())
            {
                return false;
            }

            var clipboardEntries = new List<ClipboardEntry>();

            if (data.Text != null)
                clipboardEntries.Add(createTextEntryUtf16(data.Text, cf_unicodetext));

            if (data.Image != null)
                clipboardEntries.Add(createImageEntry(data.Image));

            foreach (var entry in data.CustomFormatValues)
            {
                uint formatIdentifier = getFormatIdentifier(entry.Key);

                if (formatIdentifier == 0)
                    return false;

                clipboardEntries.Add(createTextEntryUtf16(entry.Value, formatIdentifier));
            }

            if (data.CustomFormatValues.Count > 0)
            {
                /*
                 * Required for compatibility with browser clipboard https://github.com/w3c/editing/blob/gh-pages/docs/clipboard-pickling/explainer.md
                 * Clipboard entries are stored in a predefined range of clipboard format names, on Windows being `Web Custom Format<n>`, where `n` is 0-indexed
                 * and incrementing with each custom mime type.
                 * To resolve the original mime types, a special clipboard entry is required (`Web Custom Format Map` on Windows) that maps the original mime types
                 * to the actual clipboard entries:
                 * ```json
                 * {
                 *   "text/foo": "Web Custom Format0",
                 *   "text/bar": "Web Custom Format1"
                 *  }
                 * ```
                 *
                 * This limitation is in place to prevent websites from creating arbitrary amounts of clipboard formats (Notably on Windows, the number of clipboard
                 * formats is limited to about 16,000), as well as allowing unicode values in clipboard format names on MacOS.
                 */

                var webCustomFormatMap = new Dictionary<string, string>();

                var customEntries = data.CustomFormatValues.ToList();

                for (int i = 0; i < customEntries.Count; i++)
                {
                    string formatName = customEntries[i].Key;
                    string content = customEntries[i].Value;

                    string webCustomFormatName = $"Web Custom Format{i}";

                    webCustomFormatMap[formatName] = webCustomFormatName;

                    uint webCustomFormatIdentifier = getFormatIdentifier(webCustomFormatName);
                    if (webCustomFormatIdentifier == 0)
                        return false;

                    clipboardEntries.Add(createTextEntryUtf8(content, webCustomFormatIdentifier));
                }

                uint webCustomFormatMapIdentifier = getFormatIdentifier("Web Custom Format Map");

                if (webCustomFormatMapIdentifier == 0)
                    return false;

                clipboardEntries.Add(
                    createTextEntryUtf8(
                        JsonConvert.SerializeObject(webCustomFormatMap),
                        webCustomFormatMapIdentifier
                    )
                );
            }

            return setClipboard(clipboardEntries);
        }

        private ClipboardEntry createTextEntryUtf16(string text, uint format)
        {
            int bytes = (text.Length + 1) * 2;
            IntPtr source = Marshal.StringToHGlobalUni(text);

            return new ClipboardEntry(source, bytes, format);
        }

        private ClipboardEntry createTextEntryUtf8(string text, uint format)
        {
            int bytes = Encoding.UTF8.GetByteCount(text);
            byte[] buffer = new byte[bytes + 1];

            Encoding.UTF8.GetBytes(text, 0, text.Length, buffer, 0);
            IntPtr source = Marshal.AllocHGlobal(buffer.Length);
            Marshal.Copy(buffer, 0, source, buffer.Length);

            return new ClipboardEntry(source, bytes, format);
        }

        private ClipboardEntry createImageEntry(Image image)
        {
            byte[] array;

            using (var stream = new MemoryStream())
            {
                var encoder = image.Configuration.ImageFormatsManager.GetEncoder(BmpFormat.Instance);
                image.Save(stream, encoder);
                array = stream.ToArray().Skip(bitmap_file_header_length).ToArray();
            }

            IntPtr unmanagedPointer = Marshal.AllocHGlobal(array.Length);
            Marshal.Copy(array, 0, unmanagedPointer, array.Length);

            return new ClipboardEntry(unmanagedPointer, array.Length, cf_dib);
        }

        private readonly struct ClipboardEntry
        {
            public readonly IntPtr Pointer;
            public readonly int Bytes;
            public readonly uint Format;

            public ClipboardEntry(IntPtr pointer, int bytes, uint format)
            {
                Pointer = pointer;
                Bytes = bytes;
                Format = format;
            }
        }

        private static bool setClipboard(List<ClipboardEntry> entries)
        {
            if (entries.Count == 0)
            {
                return false;
            }

            bool success = true;

            try
            {
                if (!OpenClipboard(IntPtr.Zero))
                {
                    int error = Marshal.GetLastWin32Error();
                    Logger.Log($"Failed to open clipboard with Win32 API with error code {error}", level: LogLevel.Error);

                    return false;
                }

                if (!EmptyClipboard())
                {
                    int error = Marshal.GetLastWin32Error();
                    Logger.Log($"Failed to empty clipboard with Win32 API with error code {error}", level: LogLevel.Error);

                    return false;
                }

                foreach (var entry in entries)
                {
                    // IMPORTANT: SetClipboardData requires memory that was acquired with GlobalAlloc using GMEM_MOVABLE.
                    IntPtr hGlobal = GlobalAlloc(ghnd, (UIntPtr)entry.Bytes);

                    try
                    {
                        IntPtr target = GlobalLock(hGlobal);
                        if (target == IntPtr.Zero)
                            return false;

                        try
                        {
                            unsafe
                            {
                                Buffer.MemoryCopy((void*)entry.Pointer, (void*)target, entry.Bytes, entry.Bytes);
                            }
                        }
                        finally
                        {
                            if (target != IntPtr.Zero)
                                GlobalUnlock(target);

                            Marshal.FreeHGlobal(entry.Pointer);
                        }

                        if (SetClipboardData(entry.Format, hGlobal).ToInt64() != 0)
                        {
                            // IMPORTANT: SetClipboardData takes ownership of hGlobal upon success.
                            hGlobal = IntPtr.Zero;
                        }
                        else
                        {
                            int error = Marshal.GetLastWin32Error();
                            Logger.Log($"Failed to set clipboard data with Win32 API with error code {error}", level: LogLevel.Error);

                            success = false;
                        }
                    }
                    finally
                    {
                        if (hGlobal != IntPtr.Zero)
                            GlobalFree(hGlobal);
                    }
                }
            }
            finally
            {
                if (!CloseClipboard())
                {
                    int error = Marshal.GetLastWin32Error();
                    Logger.Log($"Failed to close clipboard with Win32 API with error code {error}", level: LogLevel.Error);

                    success = false;
                }
            }

            return success;
        }

        private static T? getClipboard<T>(uint format, Func<byte[], T> transform)
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
