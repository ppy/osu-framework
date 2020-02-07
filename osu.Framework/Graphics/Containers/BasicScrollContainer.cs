// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Shapes;
using osuTK;

namespace osu.Framework.Graphics.Containers
{
    public class BasicScrollContainer : BasicScrollContainer<Drawable>
    {
        public BasicScrollContainer(Direction scrollDirection = Direction.Vertical)
            : base(scrollDirection)
        {
        }
    }

    public class BasicScrollContainer<T> : ScrollContainer<T>
        where T : Drawable
    {
        public BasicScrollContainer(Direction scrollDirection = Direction.Vertical)
            : base(scrollDirection)
        {
        }

        protected override ScrollbarContainer CreateScrollbar(Direction direction) => new BasicScrollbar(direction);

        protected internal class BasicScrollbar : ScrollbarContainer
        {
            private const float dim_size = 8;

            public BasicScrollbar(Direction direction)
                : base(direction)
            {
                Child = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = FrameworkColour.YellowGreen
                };
            }

            public override void ResizeTo(float val, int duration = 0, Easing easing = Easing.None)
            {
                Vector2 size = new Vector2(dim_size)
                {
                    [(int)ScrollDirection] = val
                };
                this.ResizeTo(size, duration, easing);
            }
        }
    }
}
