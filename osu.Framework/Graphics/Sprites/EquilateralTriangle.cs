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
        /// For equilateral triangles, height = cos(30) * sidelength = ~0.866.
        /// This is applied to the side length of the triangle to determine the height.
        /// </summary>
        private const float sidelength_scale_for_height = 0.866f;

        /// <summary>
        /// The size of this triangle.
        /// <para>
        /// When setting the size, the Y-value is ignored (use <see cref="Height"/> if you desire a specific height instead).
        /// </para>
        /// </summary>
        public override Vector2 Size => new Vector2(base.Size.X, base.Size.X * sidelength_scale_for_height);

        /// <summary>
        /// Sets the height of the triangle, adjusting the width as appropriate.
        /// </summary>
        public override float Height
        {
            get { return Width * sidelength_scale_for_height; }
            set { Size = new Vector2(value / sidelength_scale_for_height); }
        }
    }
}
