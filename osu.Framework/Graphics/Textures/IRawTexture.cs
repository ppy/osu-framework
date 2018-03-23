// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using PixelFormat = OpenTK.Graphics.ES30.PixelFormat;

namespace osu.Framework.Graphics.Textures
{
    public interface IRawTexture : IDisposable
    {
        ITextureLocker ObtainLock();

        int Width { get; }
        int Height { get; }

        PixelFormat PixelFormat { get; }
    }
}
