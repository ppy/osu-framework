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
    public sealed unsafe class VideoTextureUpload : ITextureUpload
    {
        public readonly AVFrame* Frame;

        private readonly FFmpegFuncs.AvFrameFreeDelegate freeFrameDelegate;

        public ReadOnlySpan<Rgba32> Data => ReadOnlySpan<Rgba32>.Empty;

        public int Level => 0;

        public RectangleI Bounds { get; set; }

        public PixelFormat Format => PixelFormat.Red;

        /// <summary>
        /// Sets the frame containing the data to be uploaded.
        /// </summary>
        /// <param name="frame">The frame to upload.</param>
        /// <param name="freeFrameDelegate">A function to free the frame on disposal.</param>
        internal VideoTextureUpload(AVFrame* frame, FFmpegFuncs.AvFrameFreeDelegate freeFrameDelegate)
        {
            Frame = frame;
            this.freeFrameDelegate = freeFrameDelegate;
        }

        #region IDisposable Support

        public void Dispose()
        {
            fixed (AVFrame** ptr = &Frame)
                freeFrameDelegate(ptr);
        }

        #endregion
    }
}
