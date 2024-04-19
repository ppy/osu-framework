// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using osu.Framework.Allocation;
using osu.Framework.Logging;
using SDL;
using SixLabors.ImageSharp;

namespace osu.Framework.Platform.SDL
{
    public class SDL3Clipboard : Clipboard
    {
        // SDL cannot differentiate between string.Empty and no text (eg. empty clipboard or an image)
        // doesn't matter as text editors don't really allow copying empty strings.
        // assume that empty text means no text.
        public override string? GetText() => SDL3.SDL_HasClipboardText() == SDL_bool.SDL_TRUE ? SDL3.SDL_GetClipboardText() : null;

        public override void SetText(string text) => SDL3.SDL_SetClipboardText(text);

        public override Image<TPixel>? GetImage<TPixel>()
        {
            return null;
        }

        public override bool SetImage(Image image)
        {
            return false;
        }

        /// <summary>
        /// Decodes data from a native memory span. Return null or throw an exception if the data couldn't be decoded.
        /// </summary>
        /// <typeparam name="T">Type of decoded data.</typeparam>
        private delegate T? SpanDecoder<out T>(ReadOnlySpan<byte> span);

        private static unsafe bool tryGetData<T>(string mimeType, SpanDecoder<T> decoder, out T? data)
        {
            if (SDL3.SDL_HasClipboardData(mimeType) == SDL_bool.SDL_FALSE)
            {
                data = default;
                return false;
            }

            UIntPtr nativeSize;
            IntPtr pointer = SDL3.SDL_GetClipboardData(mimeType, &nativeSize);

            if (pointer == IntPtr.Zero)
            {
                Logger.Log($"Failed to get SDL clipboard data for {mimeType}. SDL error: {SDL3.SDL_GetError()}");
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
                SDL3.SDL_free(pointer);
            }
        }

        private static unsafe bool trySetData(string mimeType, Func<ReadOnlyMemory<byte>> dataProvider)
        {
            var callbackContext = new ClipboardCallbackContext(mimeType, dataProvider);
            var objectHandle = new ObjectHandle<ClipboardCallbackContext>(callbackContext, GCHandleType.Normal);

            // TODO: support multiple mime types in a single callback
            fixed (byte* ptr = Encoding.UTF8.GetBytes(mimeType + '\0'))
            {
                int ret = SDL3.SDL_SetClipboardData(&dataCallback, &cleanupCallback, objectHandle.Handle, &ptr, 1);

                if (ret < 0)
                {
                    objectHandle.Dispose();
                    Logger.Log($"Failed to set clipboard data callback. SDL error: {SDL3.SDL_GetError()}");
                }

                return ret == 0;
            }
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static unsafe IntPtr dataCallback(IntPtr userdata, byte* mimeType, UIntPtr* length)
        {
            var objectHandle = new ObjectHandle<ClipboardCallbackContext>(userdata);

            if (!objectHandle.GetTarget(out var context))
            {
                *length = 0;
                return IntPtr.Zero;
            }

            Debug.Assert(context.MimeType == SDL3.PtrToStringUTF8(mimeType));

            var memory = context.GetAndPinData();
            *length = (UIntPtr)memory.Length;
            return context.Address;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static void cleanupCallback(IntPtr userdata)
        {
            var objectHandle = new ObjectHandle<ClipboardCallbackContext>(userdata);

            if (objectHandle.GetTarget(out var context))
            {
                context.Dispose();
                objectHandle.FreeUnsafe();
            }
        }

        private class ClipboardCallbackContext : IDisposable
        {
            public readonly string MimeType;

            /// <summary>
            /// Provider of data suitable for the <see cref="MimeType"/>.
            /// </summary>
            /// <remarks>Called when another application requests that mime type from the OS clipboard.</remarks>
            private Func<ReadOnlyMemory<byte>> dataProvider;

            private MemoryHandle memoryHandle;

            /// <summary>
            /// Address of the <see cref="ReadOnlyMemory{T}"/> returned by the <see cref="dataProvider"/>.
            /// </summary>
            /// <remarks>Pinned and suitable for passing to unmanaged code.</remarks>
            public unsafe IntPtr Address => (IntPtr)memoryHandle.Pointer;

            public ClipboardCallbackContext(string mimeType, Func<ReadOnlyMemory<byte>> dataProvider)
            {
                MimeType = mimeType;
                this.dataProvider = dataProvider;
            }

            public ReadOnlyMemory<byte> GetAndPinData()
            {
                var data = dataProvider();
                dataProvider = null!;
                memoryHandle = data.Pin();
                return data;
            }

            public void Dispose()
            {
                memoryHandle.Dispose();
            }
        }
    }
}
