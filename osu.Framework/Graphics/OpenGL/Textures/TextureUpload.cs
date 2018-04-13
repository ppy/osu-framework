// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using OpenTK.Graphics.ES30;
using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Graphics.OpenGL.Textures
{
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
