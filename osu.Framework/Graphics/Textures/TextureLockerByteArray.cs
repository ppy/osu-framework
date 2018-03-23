// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Graphics.Textures
{
    public class TextureLockerByteArray : ITextureLocker
    {
        private GCHandle handle;

        public IntPtr DataPointer => handle.AddrOfPinnedObject();

        public TextureLockerByteArray(byte[] bytes)
        {
            handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        }

        public void Dispose()
        {
            handle.Free();
        }
    }
}
