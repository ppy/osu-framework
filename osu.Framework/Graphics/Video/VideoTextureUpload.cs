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
        public AVFrame* Frame => framePtr.Value;

        private readonly VideoDecoder.Frame framePtr;

        public ReadOnlySpan<Rgba32> Data => ReadOnlySpan<Rgba32>.Empty;

        public int Level => 0;

        public RectangleI Bounds { get; set; }

        public PixelFormat Format => PixelFormat.Red;

        /// <summary>
        /// Sets the frame containing the data to be uploaded.
        /// </summary>
        /// <param name="frame">The frame to upload.</param>
        internal VideoTextureUpload(VideoDecoder.Frame frame)
        {
            framePtr = frame;
        }

        #region IDisposable Support

        public void Dispose()
        {
            framePtr.Return();
        }

        #endregion
    }
}
