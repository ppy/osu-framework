// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Runtime.InteropServices;
using osu.Framework.Allocation;
using OpenTK.Graphics.ES30;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Textures;

namespace osu.Framework.Graphics.OpenGL.Textures
{
    //todo: this can probably be removed
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

    public class TextureUploadRawTexture : TextureUpload
    {
        private readonly IRawTexture data;

        public TextureUploadRawTexture(IRawTexture data)
        {
            this.data = data;
        }

        private ITextureLocker locker;

        public override IntPtr GetPointer() => (locker = data.ObtainLock()).DataPointer;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            locker?.Dispose();
        }
    }

    public abstract class TextureUpload : IDisposable
    {
        public int Level;
        public PixelFormat Format = PixelFormat.Rgba;
        public RectangleI Bounds;

        public abstract IntPtr GetPointer();

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
        }

        ~TextureUpload()
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
