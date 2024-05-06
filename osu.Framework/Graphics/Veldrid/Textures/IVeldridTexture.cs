// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Graphics.Rendering;

namespace osu.Framework.Graphics.Veldrid.Textures
{
    internal interface IVeldridTexture : INativeTexture
    {
        IReadOnlyList<VeldridTextureResources> GetResourceList();
    }
}
