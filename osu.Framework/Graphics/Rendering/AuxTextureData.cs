// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.InteropServices;
using osu.Framework.Graphics.Shaders.Types;

namespace osu.Framework.Graphics.Rendering
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct AuxTextureData
    {
        public UniformBool IsFrameBufferTexture;
        private readonly UniformPadding12 pad1;
    }
}
