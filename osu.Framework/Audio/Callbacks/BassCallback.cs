// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using osu.Framework.Allocation;

namespace osu.Framework.Audio.Callbacks
{
    /// <summary>
    /// Abstract class that provides an optional pinned <see cref="ObjectHandle{T}"/>, used for callbacks to
    /// the Bass library.
    /// </summary>
    public abstract class BassCallback : IDisposable
    {
        private ObjectHandle<BassCallback> handle;

        protected BassCallback()
        {
            if (!RuntimeFeature.IsDynamicCodeCompiled)
                handle = new ObjectHandle<BassCallback>(this, GCHandleType.Normal);
        }

        /// <summary>
        /// The callback handle, or <see cref="IntPtr.Zero"/> if the object is not pinned.
        /// </summary>
        public IntPtr Handle => handle.Handle;

        #region IDisposable Support

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                handle.Dispose();
                disposedValue = true;
            }
        }

        ~BassCallback()
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
