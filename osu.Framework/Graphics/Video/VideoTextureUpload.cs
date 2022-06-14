// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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
        public AVFrame* Frame => ffmpegFrame.Pointer;

        public int GetPlaneWidth(uint plane)
        {
            return (plane == 0) ? Frame->width : (Frame->width + 1) / 2;
        }

        public int GetPlaneHeight(uint plane)
        {
            return (plane == 0) ? Frame->height : (Frame->height + 1) / 2;
        }

        public ReadOnlySpan<Rgba32> Data => ReadOnlySpan<Rgba32>.Empty;

        public int Level => 0;

        public RectangleI Bounds { get; set; }

        public PixelFormat Format => PixelFormat.Red;

        private readonly FFmpegFrame ffmpegFrame;

        /// <summary>
        /// Sets the frame containing the data to be uploaded.
        /// </summary>
        /// <param name="ffmpegFrame">The frame to upload.</param>
        internal VideoTextureUpload(FFmpegFrame ffmpegFrame)
        {
            this.ffmpegFrame = ffmpegFrame;
        }

        #region IDisposable Support

        public void Dispose()
        {
            ffmpegFrame.Return();
        }

        #endregion
    }
}
