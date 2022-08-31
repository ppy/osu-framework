// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel;
using osu.Framework.Graphics.Rendering;

namespace osu.Framework.Platform
{
    /// <summary>
    /// The graphics backend represented by the <see cref="IRenderer"/>.
    /// </summary>
    public enum GraphicsBackend
    {
        [Description("OpenGL")]
        OpenGL,

        [Description("Metal")]
        Metal,

        [Description("Vulkan")]
        Vulkan,

        [Description("Direct3D 11")]
        Direct3D11,
    }
}
