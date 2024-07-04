// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Rendering
{
    public enum RenderBufferFormat
    {
        /// <summary>
        /// 16-bit depth format.
        /// </summary>
        D16,

        /// <summary>
        /// 32-bit depth format.
        /// </summary>
        D32,

        /// <summary>
        /// 24-bit depth + 8-bit stencil format.
        /// </summary>
        D24S8,

        /// <summary>
        /// 32-bit depth + 8-bit stencil format.
        /// </summary>
        D32S8
    }
}
