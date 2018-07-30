// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Textures;
using System;

namespace osu.Framework.Graphics.Video
{
    /// <summary>
    /// Represents a frame decoded from a video.
    /// </summary>
    public class DecodedFrame : IDisposable
    {
        /// <summary>
        /// The timestamp of the frame in the video it was decoded from.
        /// </summary>
        public double Time { get; set; }

        /// <summary>
        /// The texture that represents the decoded frame.
        /// </summary>
        public Texture Texture { get; set; }

        private bool isDisposed = false;

        #region Disposal

        ~DecodedFrame()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
                return;

            isDisposed = true;

            Texture?.Dispose();
            Texture = null;
        }

        #endregion
    }
}
