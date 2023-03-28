// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Shaders
{
    internal class VeldridTextureUniformLayout : VeldridUniformLayout
    {
        public readonly bool HasAuxData;

        public VeldridTextureUniformLayout(int set, ResourceLayout layout, bool hasAuxData)
            : base(set, layout)
        {
            HasAuxData = hasAuxData;
        }
    }
}
