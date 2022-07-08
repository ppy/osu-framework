// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Testing.Drawables
{
    internal class TestButton : TestButtonBase
    {
        private SpriteIcon icon;
        private Container leftBoxContainer;
        private const float left_box_width = LEFT_TEXT_PADDING / 2;

        public TestButton(string header)
            : base(header)
        {
        }

        public TestButton(Type type)
            : base(type)
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            AddRange(new Drawable[]
            {
                new Box
                {
                    Colour = FrameworkColour.Green,
                    RelativeSizeAxes = Axes.Both
                },
                leftBoxContainer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Width = 0,
                    Padding = new MarginPadding { Right = -left_box_width },
                    Child = new Box
                    {
                        Colour = FrameworkColour.YellowGreen,
                        RelativeSizeAxes = Axes.Both,
                    },
                },
                icon = new SpriteIcon
                {
                    Size = new Vector2(10),
                    Icon = FontAwesome.Solid.ChevronDown,
                    Colour = Color4.White,
                    Margin = new MarginPadding { Right = left_box_width + 5 },
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

                icon.FadeColour(value ? Color4.Black : Color4.White, 100);

                if (value)
                {
                    leftBoxContainer.ResizeWidthTo(1, TRANSITION_DURATION);
                    leftBoxContainer.TransformTo(nameof(Padding), new MarginPadding { Right = left_box_width }, TRANSITION_DURATION);
                    Content.TransformTo(nameof(Padding), new MarginPadding { Right = 0f }, TRANSITION_DURATION);
                }
                else
                {
                    leftBoxContainer.ResizeWidthTo(0, TRANSITION_DURATION);
                    leftBoxContainer.TransformTo(nameof(Padding), new MarginPadding { Right = -left_box_width }, TRANSITION_DURATION);
                    Content.TransformTo(nameof(Padding), new MarginPadding { Right = LEFT_TEXT_PADDING }, TRANSITION_DURATION);
                }
            }
        }

        public override bool Collapsed
        {
            set
            {
                icon.Icon = value ? FontAwesome.Solid.ChevronDown : FontAwesome.Solid.ChevronUp;
                base.Collapsed = value;
            }
        }

        public override void Show()
        {
        }

        public override void Hide()
        {
        }
    }
}
