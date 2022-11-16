// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK;

namespace osu.Framework.Graphics.Shapes
{
    /// <summary>
    /// A triangle which has all side lengths and angles equal.
    /// </summary>
    public class EquilateralTriangle : Triangle
    {
        /// <summary>
        /// For equilateral triangles, height = cos(30) * sidelength = ~0.866 * sidelength.
        /// This is applied to the side length of the triangle to determine the height.
        /// </summary>
        private const float sidelength_to_height_factor = 0.866f;

        /// <summary>
        /// The size of this triangle.
        /// <para>
        /// When setting the size, the Y-value is ignored (use <see cref="Height"/> if you desire a specific height instead).
        /// </para>
        /// </summary>
        public override Vector2 Size => new Vector2(base.Size.X, base.Size.X * sidelength_to_height_factor);

        /// <summary>
        /// Sets the height of the triangle, adjusting the width as appropriate.
        /// </summary>
        public override float Height
        {
            get => Width * sidelength_to_height_factor;
            set => Size = new Vector2(value / sidelength_to_height_factor);
        }
    }
}
