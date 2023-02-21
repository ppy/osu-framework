// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel;

namespace osu.Framework.Platform
{
    /// <summary>
    /// The graphics surface of an <see cref="IWindow"/>.
    /// </summary>
    public enum GraphicsSurfaceType
    {
        /// <summary>
        /// An OpenGL graphics surface. The window must implement <see cref="IOpenGLGraphicsSurface"/>.
        /// </summary>
        [Description("OpenGL")]
        OpenGL,

        /// <summary>
        /// An OpenGL graphics surface with the compatibility profile. The window must implement <see cref="IOpenGLGraphicsSurface"/>.
        /// </summary>
        /// <remarks>
        /// This may be removed in the future.
        /// </remarks>
        [Description("OpenGL (compatibility profile)")]
        OpenGLCompat,

        /// <summary>
        /// A Metal graphics surface. The window must implement <see cref="IMetalGraphicsSurface"/>.
        /// </summary>
        [Description("Metal")]
        Metal,

        /// <summary>
        /// A Vulkan graphics surface.
        /// </summary>
        [Description("Vulkan")]
        Vulkan,

        /// <summary>
        /// A Direct3D11 graphics surface.
        /// </summary>
        [Description("Direct3D 11")]
        Direct3D11,
    }

    public static class GraphicsSurfaceTypeExtensions
    {
        public static bool IsOpenGL(this GraphicsSurfaceType type) => type == GraphicsSurfaceType.OpenGL || type == GraphicsSurfaceType.OpenGLCompat;
    }
}
