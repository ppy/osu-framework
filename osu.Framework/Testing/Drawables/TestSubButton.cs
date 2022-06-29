// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;

namespace osu.Framework.Testing.Drawables
{
    internal class TestSubButton : TestButtonBase
    {
        private readonly int indentLevel;

        private Container boxContainer;
        private const float left_box_width = LEFT_TEXT_PADDING / 2;

        public TestSubButton(Type test, int indentLevel = 0)
            : base(test)
        {
            this.indentLevel = indentLevel;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Add(boxContainer = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Padding = new MarginPadding { Left = indentLevel * left_box_width, Right = left_box_width },
                Alpha = 0,
                Child = new Box
                {
                    Colour = FrameworkColour.YellowGreen,
                    RelativeSizeAxes = Axes.Both,
                },
            });
        }

        public override bool Current
        {
            set
            {
                base.Current = value;
                boxContainer.FadeTo(value ? 1 : 0, TRANSITION_DURATION);
            }
        }
    }
}
