// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Graphics.Textures
{
    public class TextureLockerByteArray : ITextureLocker
    {
        // ReSharper disable once FieldCanBeMadeReadOnly.Local (impure warning if readonly)
        private GCHandle? handle;

        public IntPtr DataPointer => handle?.AddrOfPinnedObject() ?? IntPtr.Zero;

        public TextureLockerByteArray(byte[] bytes)
        {
            if (bytes.Length != 0)
                handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        }

        ~TextureLockerByteArray()
        {
            Dispose();
        }

        protected bool IsDisposed;

        protected virtual void Dispose(bool isDisposing)
        {
            IsDisposed = true;

            if (handle?.IsAllocated == true) handle?.Free();
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
