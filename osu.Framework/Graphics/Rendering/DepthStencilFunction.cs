// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Rendering
{
    public enum DepthStencilFunction
    {
        /// <summary>
        /// The test always fails.
        /// </summary>
        Never,

        /// <summary>
        /// The test passes when the incoming value is less than the value in the buffer.
        /// </summary>
        LessThan,

        /// <summary>
        /// The test passes when the incoming value is less than or equal to the value in the buffer.
        /// </summary>
        LessThanOrEqual,

        /// <summary>
        /// The test passes when the incoming value equals the value in the buffer.
        /// </summary>
        Equal,

        /// <summary>
        /// The test passes when the incoming value is greater than or equal to the value in the buffer.
        /// </summary>
        GreaterThanOrEqual,

        /// <summary>
        /// The test passes when the incoming value is greater than the value in the buffer.
        /// </summary>
        GreaterThan,

        /// <summary>
        /// The test passes when the incoming value is not equal to the value in the buffer.
        /// </summary>
        NotEqual,

        /// <summary>
        /// The test always passes.
        /// </summary>
        Always,
    }
}
