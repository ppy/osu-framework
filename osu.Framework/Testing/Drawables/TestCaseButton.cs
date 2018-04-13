// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.ComponentModel;
using System.Reflection;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input;
using OpenTK.Graphics;
using System.Collections.Generic;
using System.Linq;

namespace osu.Framework.Testing.Drawables
{
    internal class TestCaseButton : ClickableContainer, IFilterable
    {
        public IEnumerable<string> FilterTerms => text.Children.OfType<IHasFilterTerms>().SelectMany(c => c.FilterTerms);

        public bool MatchingFilter
        {
            set
            {
                if (value)
                    Show();
                else
                    Hide();
            }
        }

        private readonly Box box;
        private readonly TextFlowContainer text;

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
            AutoSizeAxes = Axes.Y;

            AddRange(new Drawable[]
            {
                box = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = new Color4(140, 140, 140, 255),
                    Alpha = 0.7f
                },
                text = new TextFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Padding = new MarginPadding
                    {
                        Left = 4,
                        Right = 4,
                        Bottom = 2,
                    },
                }
            });

            text.AddText(test.Name.Replace("TestCase", ""));

            var description = test.GetCustomAttribute<DescriptionAttribute>()?.Description;
            if (description != null)
            {
                text.NewLine();
                text.AddText(description, t => t.TextSize = 15);
            }
        }

        protected override bool OnHover(InputState state)
        {
            box.FadeTo(1, 150);
            return true;
        }

        protected override void OnHoverLost(InputState state)
        {
            box.FadeTo(0.7f, 150);
            base.OnHoverLost(state);
        }
    }
}
