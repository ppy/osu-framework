// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable
using System;

namespace osu.Framework.Graphics
{
    /// <summary>
    /// Represents colour component flags used for colour write mask during blending.
    /// </summary>
    [Flags]
    public enum BlendingMask
    {
        /// <summary>
        /// No colour component will be written to the framebuffer.
        /// </summary>
        None = 0,

        /// <summary>
        /// The red component will be written to the framebuffer.
        /// </summary>
        Red = 1,

        /// <summary>
        /// The green component will be written to the framebuffer.
        /// </summary>
        Green = 1 << 1,

        /// <summary>
        /// The blue component will be written to the framebuffer.
        /// </summary>
        Blue = 1 << 2,

        /// <summary>
        /// The alpha component will be written to the framebuffer.
        /// </summary>
        Alpha = 1 << 3,

        /// <summary>
        /// All colour components will be written to the framebuffer.
        /// </summary>
        All = Red | Green | Blue | Alpha,
    }
}
