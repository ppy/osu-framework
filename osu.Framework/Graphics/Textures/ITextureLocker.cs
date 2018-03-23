// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Graphics.Textures
{
    /// <summary>
    /// Provides access to unmanaged texture data, as well as a method for freeing underlying resources after usage.
    /// </summary>
    public interface ITextureLocker : IDisposable
    {
        IntPtr DataPointer { get; }
    }
}
