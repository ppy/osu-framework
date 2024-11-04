// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using osu.Framework.Allocation;
using osu.Framework.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using static SDL.SDL3;

namespace osu.Framework.Platform.SDL3
{
    public class SDL3Clipboard : Clipboard
    {
        /// <summary>
        /// Supported formats for decoding images from the clipboard.
        /// </summary>
        // It's possible for a format to not have a registered decoder, but all default formats will have one:
        // https://github.com/SixLabors/ImageSharp/discussions/1353#discussioncomment-9142056
        private static IEnumerable<string> supportedImageMimeTypes => SixLabors.ImageSharp.Configuration.Default.ImageFormats.SelectMany(f => f.MimeTypes);

        /// <summary>
        /// Format used for encoding (saving) images to the clipboard.
        /// </summary>
        private readonly IImageFormat imageFormat;

        public SDL3Clipboard(IImageFormat imageFormat)
        {
            this.imageFormat = imageFormat;
        }

        // SDL cannot differentiate between string.Empty and no text (eg. empty clipboard or an image)
        // doesn't matter as text editors don't really allow copying empty strings.
        // assume that empty text means no text.
        public override string? GetText() => SDL_HasClipboardText() ? SDL_GetClipboardText() : null;

        public override void SetText(string text) => SDL_SetClipboardText(text);

        public override Image<TPixel>? GetImage<TPixel>()
        {
            foreach (string mimeType in supportedImageMimeTypes)
            {
                if (tryGetData(mimeType, Image.Load<TPixel>, out var image))
                {
                    Logger.Log($"Decoded {mimeType} from clipboard.");
                    return image;
                }
            }

            return null;
        }

        public override bool SetImage(Image image)
        {
            ReadOnlyMemory<byte> memory;

            // we can't save the image in the callback as the caller owns the image and might dispose it from under us.

            using (var stream = new MemoryStream())
            {
                image.Save(stream, imageFormat);

                // The buffer is allowed to escape the lifetime of the MemoryStream.
                // https://learn.microsoft.com/en-us/dotnet/api/system.io.memorystream.getbuffer?view=net-8.0
                // "This method works when the memory stream is closed."
                memory = new ReadOnlyMemory<byte>(stream.GetBuffer(), 0, (int)stream.Length);
            }

            return trySetData(imageFormat.DefaultMimeType, () => memory);
        }

        /// <summary>
        /// Decodes data from a native memory span. Return null or throw an exception if the data couldn't be decoded.
        /// </summary>
        /// <typeparam name="T">Type of decoded data.</typeparam>
        private delegate T? SpanDecoder<out T>(ReadOnlySpan<byte> span);

        private static unsafe bool tryGetData<T>(string mimeType, SpanDecoder<T> decoder, out T? data)
        {
            if (!SDL_HasClipboardData(mimeType))
            {
                data = default;
                return false;
            }

            UIntPtr nativeSize;
            IntPtr pointer = SDL_GetClipboardData(mimeType, &nativeSize);

            if (pointer == IntPtr.Zero)
            {
                Logger.Log($"Failed to get SDL clipboard data for {mimeType}. SDL error: {SDL_GetError()}");
                data = default;
                return false;
            }

            try
            {
                var nativeMemory = new ReadOnlySpan<byte>((void*)pointer, (int)nativeSize);
                data = decoder(nativeMemory);
                return data != null;
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Failed to decode clipboard data for {mimeType}.");
                data = default;
                return false;
            }
            finally
            {
                SDL_free(pointer);
            }
        }

        private static unsafe bool trySetData(string mimeType, Func<ReadOnlyMemory<byte>> dataProvider)
        {
            var callbackContext = new ClipboardCallbackContext(mimeType, dataProvider);
            var objectHandle = new ObjectHandle<ClipboardCallbackContext>(callbackContext, GCHandleType.Normal);

            // TODO: support multiple mime types in a single callback
            fixed (byte* ptr = Encoding.UTF8.GetBytes(mimeType + '\0'))
            {
                if (!SDL_SetClipboardData(&dataCallback, &cleanupCallback, objectHandle.Handle, &ptr, 1))
                {
                    objectHandle.Dispose();
                    Logger.Log($"Failed to set clipboard data callback. SDL error: {SDL_GetError()}");
                    return false;
                }

                return true;
            }
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static unsafe IntPtr dataCallback(IntPtr userdata, byte* mimeType, UIntPtr* length)
        {
            using var objectHandle = new ObjectHandle<ClipboardCallbackContext>(userdata);

            if (!objectHandle.GetTarget(out var context) || context.MimeType != PtrToStringUTF8(mimeType))
            {
                *length = 0;
                return IntPtr.Zero;
            }

            context.EnsureDataValid();
            *length = context.DataLength;
            return context.Address;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static void cleanupCallback(IntPtr userdata)
        {
            using var objectHandle = new ObjectHandle<ClipboardCallbackContext>(userdata, true);

            if (objectHandle.GetTarget(out var context))
            {
                context.Dispose();
            }
        }

        private class ClipboardCallbackContext : IDisposable
        {
            public readonly string MimeType;

            /// <summary>
            /// Provider of data suitable for the <see cref="MimeType"/>.
            /// </summary>
            /// <remarks>Called when another application requests that mime type from the OS clipboard.</remarks>
            private Func<ReadOnlyMemory<byte>>? dataProvider;

            private MemoryHandle memoryHandle;

            /// <summary>
            /// Address of the <see cref="ReadOnlyMemory{T}"/> returned by the <see cref="dataProvider"/>.
            /// </summary>
            /// <remarks>Pinned and suitable for passing to unmanaged code.</remarks>
            public unsafe IntPtr Address => (IntPtr)memoryHandle.Pointer;

            /// <summary>
            /// Length of the <see cref="ReadOnlyMemory{T}"/> returned by the <see cref="dataProvider"/>.
            /// </summary>
            public UIntPtr DataLength { get; private set; }

            public ClipboardCallbackContext(string mimeType, Func<ReadOnlyMemory<byte>> dataProvider)
            {
                MimeType = mimeType;
                this.dataProvider = dataProvider;
            }

            public void EnsureDataValid()
            {
                if (dataProvider == null)
                {
                    Debug.Assert(Address != IntPtr.Zero);
                    Debug.Assert(DataLength != 0);
                    return;
                }

                var data = dataProvider();
                dataProvider = null!;
                DataLength = (UIntPtr)data.Length;
                memoryHandle = data.Pin();
            }

            public void Dispose()
            {
                memoryHandle.Dispose();
                DataLength = 0;
            }
        }
    }
}
