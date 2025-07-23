// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using osuTK;

namespace osu.Framework.Graphics.UserInterface
{
    public partial class TextAreaCaret : CompositeDrawable
    {
        public void DisplayAt(Vector2 position, float lineHeight)
        {
            foreach (var c in InternalChildren)
                c.Expire();

            AddInternal(new Box
            {
                Position = position,
                Origin = Anchor.TopCentre,
                Size = new Vector2(1.5f, lineHeight)
            });
        }

        public void DisplayRange(IEnumerable<RectangleF> selectionRects)
        {
            foreach (var c in InternalChildren)
                c.Expire();

            foreach (var rect in selectionRects)
            {
                AddInternal(new Box
                {
                    Position = rect.TopLeft,
                    Size = rect.Size,
                    Alpha = 0.5f,
                });
            }
        }

        protected override bool ComputeIsMaskedAway(RectangleF maskingBounds) => false;
    }
}
