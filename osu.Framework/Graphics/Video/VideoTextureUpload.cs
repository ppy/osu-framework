// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Primitives;
using osuTK.Graphics.ES30;
using FFmpeg.AutoGen;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Graphics.Textures
{
    public unsafe class VideoTextureUpload : ITextureUpload
    {
        public ReadOnlySpan<Rgba32> Data => Span<Rgba32>.Empty;

        public AVFrame* Frame;

        /// <summary>
        /// The target mipmap level to upload into.
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// The texture format for this upload.
        /// </summary>
        public PixelFormat Format => PixelFormat.Red;

        /// <summary>
        /// The target bounds for this upload. If not specified, will assume to be (0, 0, width, height).
        /// </summary>
        public RectangleI Bounds { get; set; }

        /// <summary>
        /// Sets the frame cotaining the data to be uploaded
        /// </summary>
        /// <param name="frame">The libav frame to upload.</param>
        public VideoTextureUpload(AVFrame* frame)
        {
            Frame = frame;
        }

        // ReSharper disable once ConvertToAutoPropertyWithPrivateSetter
        public bool HasBeenUploaded => disposed;

        #region IDisposable Support

#pragma warning disable IDE0032 // Use auto property
        private bool disposed;
#pragma warning restore IDE0032 // Use auto property

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                disposed = true;
                fixed (AVFrame** ptr = &Frame)
                    ffmpeg.av_frame_free(ptr);
            }
        }

        ~VideoTextureUpload()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
