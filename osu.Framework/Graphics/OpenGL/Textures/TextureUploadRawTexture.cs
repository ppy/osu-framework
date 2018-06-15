// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Textures;

namespace osu.Framework.Graphics.OpenGL.Textures
{
    public class TextureUploadRawTexture : TextureUpload
    {
        private readonly IRawTexture data;

        public TextureUploadRawTexture(IRawTexture data)
        {
            this.data = data;
            Bounds = new RectangleI(0, 0, data.Width, data.Height);
        }

        private ITextureLocker locker;

        public override IntPtr GetPointer() => (locker ?? (locker = data.ObtainLock())).DataPointer;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            locker?.Dispose();
        }
    }
}
