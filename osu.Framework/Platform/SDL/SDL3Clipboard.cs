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

        private unsafe class ClipboardImageData
        {
            public byte* Buffer;
            public nuint Length;
        }

        private static readonly ClipboardImageData png_data = new ClipboardImageData();

        public override bool SetImage(Image image)
        {
            MemoryStream pngStream = new MemoryStream();
            image.SaveAsPng(pngStream);
            byte[] pngBuffer = pngStream.GetBuffer();

            unsafe
            {
                fixed (byte* pngBufferPtr = pngBuffer)
                {
                    png_data.Buffer = pngBufferPtr;
                    png_data.Length = (nuint)pngBuffer.Length;

                    byte[] pngMimeType = Encoding.UTF8.GetBytes("image/png");

                    fixed (byte* pngMimeTypePtr = pngMimeType)
                    {
                        SDL3.SDL_SetClipboardData(&clipboardDataCallback, null, (nint)null, &pngMimeTypePtr, 1);
                    }
                }
            }

            return false;
        }

        [UnmanagedCallersOnly(EntryPoint = "clipboardDataCallback", CallConvs = [typeof(CallConvCdecl)])]
        private static unsafe nint clipboardDataCallback(nint userdata, byte* mimeType, nuint* length)
        {
            string mimeTypeStr = new string((sbyte*)mimeType);

            if (mimeTypeStr == "image/png")
            {
                byte* rawBuffer = (byte*)SDL3.SDL_malloc(png_data.Length);

                byte* buffer = png_data.Buffer;
                for (nuint i = 0; i < png_data.Length; i++)
                    rawBuffer[i] = buffer[i];

                *length = png_data.Length;
                return (nint)rawBuffer;
            }

            return 0;
        }
    }
}
