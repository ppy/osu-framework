// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.InteropServices;
using osu.Framework.Graphics.Shaders.Types;

namespace osu.Framework.Graphics.Rendering
{
    // sh_GlobalUniforms.h
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct GlobalUniformData
    {
        public UniformBool BackbufferDraw;
        public UniformBool IsDepthRangeZeroToOne;
        public UniformBool IsClipSpaceYInverted;
        public UniformBool IsUvOriginTopLeft;

        public UniformMatrix4 ProjMatrix;

        public UniformInt WrapModeS;
        public UniformInt WrapModeT;
        private readonly UniformPadding8 pad1;
    }
}
