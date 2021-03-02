// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Textures;
using osuTK.Graphics.ES30;
using FFmpeg.AutoGen;
using osu.Framework.Graphics.Primitives;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Graphics.Video
{
    public unsafe class VideoTextureUpload : ITextureUpload
    {
        public AVFrame* Frame;

        private readonly FFmpegFuncs.AvFrameFreeDelegate freeFrame;

        public ReadOnlySpan<Rgba32> Data => ReadOnlySpan<Rgba32>.Empty;
        public int Level => 0;
        public RectangleI Bounds { get; set; }
        public PixelFormat Format => PixelFormat.Red;

        /// <summary>
        /// Sets the frame cotaining the data to be uploaded
        /// </summary>
        /// <param name="frame">The libav frame to upload.</param>
        /// <param name="free">A function to free the frame on disposal.</param>
        public VideoTextureUpload(AVFrame* frame, FFmpegFuncs.AvFrameFreeDelegate free)
        {
            Frame = frame;
            freeFrame = free;
        }

        #region IDisposable Support

        public void Dispose()
        {
            fixed (AVFrame** ptr = &Frame)
                freeFrame(ptr);
        }

        #endregion
    }
}
