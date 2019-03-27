// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK.Graphics;

namespace osu.Framework.Testing.Drawables
{
    internal class TestCaseSubButton : TestCaseButton
    {
        private Container boxContainer;
        private const float left_box_width = LEFT_TEXT_PADDING / 2;

        public TestCaseSubButton(Type test)
            : base(test)
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            AddRange(new Drawable[]
            {
                boxContainer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Left = left_box_width, Right = left_box_width },
                    Alpha = 0,
                    Child = new Box
                    {
                        Colour = new Color4(128, 164, 108, 255),
                        RelativeSizeAxes = Axes.Both,
                    },
                },
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
                    boxContainer.FadeIn(transition_duration);
                }
                else
                {
                    boxContainer.FadeOut(transition_duration);
                }
            }
        }
    }
}
