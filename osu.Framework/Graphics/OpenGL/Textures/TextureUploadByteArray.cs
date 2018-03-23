// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Runtime.InteropServices;
using osu.Framework.Allocation;

namespace osu.Framework.Graphics.OpenGL.Textures
{
    // todo: this can probably be removed, as we have RawTextures that support byte[] now.
    public class TextureUploadByteArray : TextureUpload
    {
        public readonly byte[] Data;

        private GCHandle handle;

        private readonly BufferStack<byte> bufferStack;

        public TextureUploadByteArray(int size, BufferStack<byte> bufferStack = null)
        {
            this.bufferStack = bufferStack;
            Data = this.bufferStack?.ReserveBuffer(size) ?? new byte[size];
        }

        public TextureUploadByteArray(byte[] data)
        {
            Data = data;
        }

        public override IntPtr GetPointer()
        {
            if (handle.IsAllocated)
                return handle.AddrOfPinnedObject();

            if (Data == null || Data.Length == 0) return IntPtr.Zero;

            handle = GCHandle.Alloc(Data, GCHandleType.Pinned);
            return handle.AddrOfPinnedObject();
        }

        protected override void Dispose(bool disposing)
        {
            if (handle.IsAllocated) handle.Free();
            bufferStack?.FreeBuffer(Data);
        }
    }
}
