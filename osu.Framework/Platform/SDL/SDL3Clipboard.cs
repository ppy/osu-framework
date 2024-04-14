// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
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

        public override void SetText(string text) => SDL3.SDL_SetClipboardText(Encoding.UTF8.GetBytes(text));

        public override Image<TPixel>? GetImage<TPixel>()
        {
            byte[] buffer;

            unsafe
            {
                // It appears that SDL_GetClipboardData still returns the clipboard data
                // even if it doesn't have the mimetype that we requested.
                // So instead we ensure it's at least not text before trying to get it.
                if (SDL3.SDL_HasClipboardText() == SDL3.SDL_TRUE)
                    return null;

                nuint size = 0;
                byte* data = (byte*)SDL3.SDL_GetClipboardData("image/png\0"u8, &size);
                if (data == null)
                    return null;

                buffer = new byte[size];
                for (nuint i = 0; i < size; i++)
                    buffer[i] = data[i];

                SDL3.SDL_free(data);
            }

            return Image.Load<TPixel>(buffer);
        }

        private unsafe struct ClipboardImageData
        {
            public byte* RawBuffer;
            public nuint Length;
        }

        public override bool SetImage(Image image)
        {
            MemoryStream pngStream = new MemoryStream();
            image.SaveAsPng(pngStream);
            byte[] buffer = pngStream.GetBuffer();

            unsafe
            {
                ClipboardImageData* pngData = (ClipboardImageData*)SDL3.SDL_malloc((nuint)sizeof(ClipboardImageData));

                byte* rawBuffer = (byte*)SDL3.SDL_malloc((nuint)buffer.Length);

                for (int i = 0; i < buffer.Length; i++)
                    rawBuffer[i] = buffer[i];

                pngData->RawBuffer = rawBuffer;
                pngData->Length = (nuint)buffer.Length;

                fixed (byte* pngMimeTypePtr = "image/png\0"u8)
                {
                    int status = SDL3.SDL_SetClipboardData(&clipboardDataCallback, &clipboardCleanupCallback, (nint)pngData, &pngMimeTypePtr, 1);
                    return status == 0;
                }
            }
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static unsafe nint clipboardDataCallback(nint userdata, byte* mimeType, nuint* length)
        {
            ClipboardImageData* pngData = (ClipboardImageData*)userdata;

            fixed (byte* pngMimetypeStr = "image/png\0"u8)
            {
                if (SDL3.SDL_strcmp(mimeType, pngMimetypeStr) == 0)
                {
                    *length = pngData->Length;
                    return (nint)pngData->RawBuffer;
                }
            }

            return 0;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static unsafe void clipboardCleanupCallback(nint userdata)
        {
            ClipboardImageData* pngData = (ClipboardImageData*)userdata;

            SDL3.SDL_free(pngData->RawBuffer);
            SDL3.SDL_free(pngData);
        }
    }
}
