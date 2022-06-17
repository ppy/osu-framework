// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Graphics.Containers;

namespace osu.Framework.Graphics.Shapes
{
    /// <summary>
    /// A simple <see cref="CircularContainer"/> with a fill using a <see cref="Box"/>. Can be coloured using the <see cref="Drawable.Colour"/> property.
    /// </summary>
    public class Circle : CircularContainer
    {
        public Circle()
        {
            Masking = true;

            AddInternal(new Box { RelativeSizeAxes = Axes.Both });
        }
    }
}
