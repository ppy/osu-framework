// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;

namespace osu.Framework.Graphics.Sprites
{
    /// <summary>
    /// A triangle which has all side lengths and angles equal.
    /// </summary>
    public class EquilateralTriangle : Triangle
    {
        /// <summary>
        /// Scaling height by 0.866 results in equilateral triangles.
        /// </summary>
        private const float equilateral_length_scale = 0.866f;

        /// <summary>
        /// The side lengths of this triangle.
        /// <para>
        /// Note: The Y-value is ignored for equilateral triangles.
        /// </para>
        /// </summary>
        public override Vector2 Size => new Vector2(base.Size.X, base.Size.X * equilateral_length_scale);
    }
}
