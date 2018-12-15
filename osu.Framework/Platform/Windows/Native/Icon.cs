// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Platform.Windows.Native
{
    internal class Icon : IDisposable
    {
        public IntPtr Handle { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        internal Icon(IntPtr handle, int width, int height)
        {
            Handle = handle;
            Width = width;
            Height = height;
        }

        ~Icon()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (Handle != IntPtr.Zero)
                DestroyIcon(Handle);
            Handle = IntPtr.Zero;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);
    }
}
