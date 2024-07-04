// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Rendering
{
    public enum StencilOperation
    {
        /// <summary>
        /// Set the stencil buffer to 0.
        /// </summary>
        Zero,

        /// <summary>
        /// Bitwise invert the stencil buffer.
        /// </summary>
        Invert,

        /// <summary>
        /// Do not change the stencil buffer.
        /// </summary>
        Keep,

        /// <summary>
        /// Replce the stenciil buffer with the <see cref="StencilInfo.TestValue"/>.
        /// </summary>
        Replace,

        /// <summary>
        /// Increase the stencil buffer by 1 if it's lower than the maximum value.
        /// </summary>
        Increase,

        /// <summary>
        /// Decrease the stencil buffer by 1 it's higher than 0.
        /// </summary>
        Decrease,

        /// <summary>
        /// Increase the stencil buffer by 1 and wrap to 0 if the result is above the maximum value.
        /// </summary>
        IncreaseWrap,

        /// <summary>
        /// Decrease the stencil buffer by 1 and wrap to maximum value if the result is below 0.
        /// </summary>
        DecreaseWrap
    }
}
