// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.InteropServices;
using osu.Framework.Graphics.Shaders.Types;

namespace osu.Framework.Graphics.Rendering
{
    /// <summary>
    /// Represents the parameters for the compute shader used in mipmap generation ("sh_mipmap.comp").
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal record struct ComputeMipmapGenerationParameters
    {
        public UniformVector4 Region;
        public UniformInt OutputWidth;
        private readonly UniformPadding12 pad1;
    }
}
