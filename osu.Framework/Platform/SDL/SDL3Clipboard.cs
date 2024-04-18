// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using SDL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

namespace osu.Framework.Platform.SDL
{
    public class SDL3Clipboard : Clipboard
    {
        // SDL cannot differentiate between string.Empty and no text (eg. empty clipboard or an image)
        // doesn't matter as text editors don't really allow copying empty strings.
        // assume that empty text means no text.
        public override string? GetText() => SDL3.SDL_HasClipboardText() == SDL_bool.SDL_TRUE ? SDL3.SDL_GetClipboardText() : null;

        public override void SetText(string text) => SDL3.SDL_SetClipboardText(text);

        private static readonly ImageFormatManager image_format_manager = SixLabors.ImageSharp.Configuration.Default.ImageFormatsManager;

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

                byte* data = null;
                nuint size = 0;

                foreach (IImageFormat? imageFormat in image_format_manager.ImageFormats)
                {
                    foreach (string mimeType in imageFormat.MimeTypes)
                    {
                        fixed (byte* zmimeType = Encoding.UTF8.GetBytes(mimeType + '\0'))
                        {
                            data = (byte*)SDL3.SDL_GetClipboardData(zmimeType, &size);
                        }

                        if (data != null)
                            break;
                    }

                    if (data != null)
                        break;
                }

                if (data == null)
                    return null;

                buffer = new byte[size];
                Marshal.Copy((nint)data, buffer, 0, (int)size);

                SDL3.SDL_free(data);
            }

            return Image.Load<TPixel>(buffer);
        }

        // Used by the clipboard callbacks to access the image that has to be copied.
        // When SetImage is called, the previous image is not immediately cleaned up
        // by the cleanup callback (it is called only after we create the next image),
        // which is why there are two of them to allow some overlap.
        private static readonly Image?[] cb_images = { null, null };
        private static int currentCbImageIdx;

        public override bool SetImage(Image image)
        {
            unsafe
            {
                currentCbImageIdx = currentCbImageIdx == 0 ? 1 : 0;
                cb_images[currentCbImageIdx] = image;

                fixed (byte* pngMimeTypePtr = "image/png\0"u8)
                {
                    int status = SDL3.SDL_SetClipboardData(&clipboardDataCallback, &clipboardCleanupCallback, currentCbImageIdx, &pngMimeTypePtr, 1);
                    return status == 0;
                }
            }
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static unsafe nint clipboardDataCallback(nint userdata, byte* mimetype, nuint* length)
        {
            Image? image = cb_images[(int)userdata];
            if (image == null)
                return 0;

            string? mimeType = SDL3.PtrToStringUTF8(mimetype);
            if (mimeType == null)
                return 0;

            image_format_manager.TryFindFormatByMimeType(mimeType, out IImageFormat? imageFormat);
            if (imageFormat == null)
                return 0;

            MemoryStream imgStream = new MemoryStream();
            image.Save(imgStream, imageFormat);
            byte[] buffer = imgStream.GetBuffer();

            byte* rawBuffer = (byte*)SDL3.SDL_malloc((nuint)buffer.Length);
            Marshal.Copy(buffer, 0, (nint)rawBuffer, buffer.Length);

            *length = (nuint)buffer.Length;
            return (nint)rawBuffer;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static void clipboardCleanupCallback(nint userdata)
        {
            cb_images[(int)userdata] = null;
        }
    }
}
