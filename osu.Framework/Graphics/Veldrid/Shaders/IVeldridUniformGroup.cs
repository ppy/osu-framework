// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;

namespace osu.Framework.Graphics.Veldrid.Shaders
{
    internal interface IVeldridUniformGroup
    {
        IReadOnlyList<VeldridUniformInfo> Uniforms { get; }
    }
}
