// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Rendering
{
    public enum DepthTestFunction
    {
        /// <summary>
        /// The depth test always fails.
        /// </summary>
        Never,

        /// <summary>
        /// The depth test passes when the incoming value is less than the value in the depth buffer.
        /// </summary>
        LessThan,

        /// <summary>
        /// The depth test passes when the incoming value is less than or equal to the value in the depth buffer.
        /// </summary>
        LessThanOrEqual,

        /// <summary>
        /// The depth test passes when the incoming value equals the value in the depth buffer.
        /// </summary>
        Equal,

        /// <summary>
        /// The depth test passes when the incoming value is greater than or equal to the value in the depth buffer.
        /// </summary>
        GreaterThanOrEqual,

        /// <summary>
        /// The depth test passes when the incoming value is greater than the value in the depth buffer.
        /// </summary>
        Greater,

        /// <summary>
        /// The depth test passes when the incoming value is not equal to the value in the depth buffer.
        /// </summary>
        NotEqual,

        /// <summary>
        /// The depth test always passes.
        /// </summary>
        Always,
    }
}
