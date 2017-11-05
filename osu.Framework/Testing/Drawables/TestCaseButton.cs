// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Testing.Drawables
{
    internal class TestCaseButton : ClickableContainer
    {
        private readonly Box box;
        private readonly Container text;

        public readonly Type TestType;

        public bool Current
        {
            set
            {
                const float transition_duration = 100;

                if (value)
                {
                    box.FadeColour(new Color4(220, 220, 220, 255), transition_duration);
                    text.FadeColour(Color4.Black, transition_duration);
                }
                else
                {
                    box.FadeColour(new Color4(140, 140, 140, 255), transition_duration);
                    text.FadeColour(Color4.White, transition_duration);
                }
            }
        }

        public TestCaseButton(Type test)
        {
            Masking = true;

            TestType = test;

            CornerRadius = 5;
            RelativeSizeAxes = Axes.X;
            Size = new Vector2(1, 60);

            TestCase tempTestCase = (TestCase)Activator.CreateInstance(test);

            AddRange(new Drawable[]
            {
                box = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = new Color4(140, 140, 140, 255),
                    Alpha = 0.7f
                },
                text = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding
                    {
                        Left = 4,
                        Right = 4,
                        Bottom = 2,
                    },
                    Children = new[]
                    {
                        new SpriteText
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Text = tempTestCase.Name,
                        },
                        new SpriteText
                        {
                            Anchor = Anchor.BottomLeft,
                            Origin = Anchor.BottomLeft,
                            Text = tempTestCase.Description,
                            TextSize = 15,
                            AutoSizeAxes = Axes.Y,
                            RelativeSizeAxes = Axes.X,
                        }
                    }
                }
            });
        }

        protected override bool OnHover(InputState state)
        {
            box.FadeTo(1, 150);
            return true;
        }

        protected override void OnHoverLost(InputState state)
        {
            box.FadeTo(0.7f, 150);
        }
    }
}
