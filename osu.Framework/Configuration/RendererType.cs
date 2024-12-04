// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using osu.Framework.Graphics.OpenGL;

namespace osu.Framework.Configuration
{
    // todo: revisit when we have a way to exclude enum members from naming rules
    [SuppressMessage("ReSharper", "InconsistentNaming")]
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

        [Description("Metal (Experimental)")]
        Deferred_Metal = 32,

        [Description("Vulkan (Experimental)")]
        Deferred_Vulkan,

        [Description("Direct3D 11 (Experimental)")]
        Deferred_Direct3D11,

        [Description("OpenGL (Experimental)")]
        Deferred_OpenGL
    }
}
