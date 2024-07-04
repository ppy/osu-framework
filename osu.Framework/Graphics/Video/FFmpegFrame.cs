// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Diagnostics;
using FFmpeg.AutoGen;

namespace osu.Framework.Graphics.Video
{
    internal sealed unsafe class FFmpegFrame : IDisposable
    {
        public readonly AVFrame* Pointer;

        public AVPixelFormat PixelFormat
        {
            get => (AVPixelFormat)Pointer->format;
            set => Pointer->format = (int)value;
        }

        private readonly FFmpegFuncs ffmpeg;
        private readonly Action<FFmpegFrame> returnDelegate;

        internal FFmpegFrame(FFmpegFuncs ffmpeg, Action<FFmpegFrame> returnDelegate = null)
        {
            Pointer = ffmpeg.av_frame_alloc();

            this.ffmpeg = ffmpeg;
            this.returnDelegate = returnDelegate;
        }

        public void Return()
        {
            Debug.Assert(Pointer != null);

            if (returnDelegate != null)
                returnDelegate(this);
            else
                Dispose();
        }

        public void Dispose()
        {
            if (Pointer == null)
                return;

            fixed (AVFrame** ptr = &Pointer)
                ffmpeg.av_frame_free(ptr);
        }
    }
}
