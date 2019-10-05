// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual.Containers
{
    public class TestSceneScrollContainer : TestScene
    {
        public ScrollContainer<Drawable> ScrollContainer { get; set; }

        public class ClampedScrollbarScrollContainer : BasicScrollContainer
        {
            internal new ScrollbarContainer Scrollbar => base.Scrollbar;

            protected override ScrollbarContainer CreateScrollbar(Direction direction) => new ClampedScrollbar(direction);

            private class ClampedScrollbar : BasicScrollbar
            {
                protected internal override float MinimumDimSize => 250;

                public ClampedScrollbar(Direction direction)
                    : base(direction)
                {
                }
            }
        }
    }
}
