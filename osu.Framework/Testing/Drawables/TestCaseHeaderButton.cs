// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK.Graphics;

namespace osu.Framework.Testing.Drawables
{
    internal class TestCaseHeaderButton : TestCaseButton
    {
        private SpriteText headerSprite;
        private Box leftBox, bgBox;
        private const float left_box_width = LEFT_TEXT_PADDING / 2;

        public TestCaseHeaderButton(string header)
            : base(header)
        { }

        public TestCaseHeaderButton(Type type)
            : base(type)
        { }

        [BackgroundDependencyLoader]
        private void load()
        {
            AddRange(new Drawable[]
            {
                bgBox = new Box
                {
                    Depth = 1,
                    Colour = new Color4(57, 110, 102, 255),
                    RelativeSizeAxes = Axes.Both
                },
                leftBox = new Box
                {
                    Depth = 1,
                    Colour = new Color4(128, 164, 108, 255),
                    Width = left_box_width,
                    RelativeSizeAxes = Axes.Y,
                },
                headerSprite = new SpriteText
                {
                    Font = new FontUsage(size: 20),
                    Colour = Color4.LightGray,
                    Padding = new MarginPadding { Left = left_box_width, Right = 5 },
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                }
            });
        }

        public override bool Current
        {
            set
            {
                base.Current = value;

                const float transition_duration = 100;

                if (value)
                {

                }
            }
        }

        public override void Hide() => headerSprite.Text = "...";

        public override void Show() => headerSprite.Text = string.Empty;
    }
}
