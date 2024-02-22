// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.ComponentModel;
using osu.Framework.Graphics.OpenGL;

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

        /// <summary>
        /// Uses <see cref="GLRenderer"/>.
        /// </summary>
        [Description("OpenGL")]
        OpenGL,

        // Can be removed 20240820
        [Obsolete]
        [Description("OpenGL (Legacy)")]
        OpenGLLegacy,
    }
}
