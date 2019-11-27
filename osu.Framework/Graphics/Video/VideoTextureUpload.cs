// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Textures;
using osuTK.Graphics.ES30;
using FFmpeg.AutoGen;

namespace osu.Framework.Graphics.Video
{
    public unsafe class VideoTextureUpload : ArrayPoolTextureUpload
    {
        public AVFrame* Frame;

        private readonly FFmpegFuncs.AvFrameFreeDelegate freeFrame;

        public override PixelFormat Format => PixelFormat.Red;

        /// <summary>
        /// Sets the frame cotaining the data to be uploaded
        /// </summary>
        /// <param name="frame">The libav frame to upload.</param>
        /// <param name="free">A function to free the frame on disposal.</param>
        public VideoTextureUpload(AVFrame* frame, FFmpegFuncs.AvFrameFreeDelegate free)
            : base(0, 0)
        {
            Frame = frame;
            freeFrame = free;
        }

        #region IDisposable Support

        protected override void Dispose(bool disposing)
        {
            fixed (AVFrame** ptr = &Frame)
                freeFrame(ptr);

            base.Dispose(disposing);
        }

        #endregion
    }
}
