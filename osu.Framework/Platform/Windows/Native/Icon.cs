// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Platform.Windows.Native
{
    internal class Icon : IDisposable
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        private bool disposed;

        public IntPtr Handle { get; private set; }

        public readonly int Width;

        public readonly int Height;

        internal Icon(IntPtr handle, int width, int height)
        {
            Handle = handle;
            Width = width;
            Height = height;
        }

        ~Icon()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (Handle != IntPtr.Zero)
            {
                DestroyIcon(Handle);
                Handle = IntPtr.Zero;
            }

            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
