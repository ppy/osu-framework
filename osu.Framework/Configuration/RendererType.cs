// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel;

namespace osu.Framework.Configuration
{
    public enum RendererType
    {
        [Description("Automatic")]
        Automatic,

        [Description("Metal")]
        Metal,

        [Description("Vulkan")]
        Vulkan,

        [Description("Direct3D 11")]
        Direct3D11,

        [Description("OpenGL")]
        OpenGL,

        [Description("OpenGL (Legacy)")]
        OpenGLLegacy,
    }
}
