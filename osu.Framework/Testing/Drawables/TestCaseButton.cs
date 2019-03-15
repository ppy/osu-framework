// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osuTK.Graphics;
using Container = osu.Framework.Graphics.Containers.Container;

namespace osu.Framework.Testing.Drawables
{
    internal class TestCaseButton : ClickableContainer, IFilterable
    {
        public IEnumerable<string> FilterTerms => text.Children.OfType<IHasFilterTerms>().SelectMany(c => c.FilterTerms);

        private bool matchingFilter = true;

        public bool MatchingFilter
        {
            set
            {
                matchingFilter = value;

                updateVisibility();
            }
        }

        private bool collapsed;

        public bool Collapsed
        {
            set
            {
                collapsed = value;
                updateVisibility();
            }
        }

        private void updateVisibility()
        {
            if (collapsed || !matchingFilter)
                Hide();
            else
                Show();
        }

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
                    box.FadeColour(new Color4(90, 90, 90, 255), transition_duration);
                    text.FadeColour(Color4.White, transition_duration);
                }
            }
        }

        protected override Container<Drawable> Content => content;

        private readonly Box box;
        private readonly Container content;
        private readonly TextFlowContainer text;
        public readonly Type TestType;

        private TestCaseButton()
        {
            AutoSizeAxes = Axes.Y;
            RelativeSizeAxes = Axes.X;

            Padding = new MarginPadding { Bottom = 3 };

            InternalChildren = new Drawable[]
            {
                box = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = new Color4(140, 140, 140, 255),
                    Alpha = 0.7f
                },
                content = new Container
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Child = text = new TextFlowContainer
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
                },
            };
        }

        public TestCaseButton(Type test)
            : this()
        {
            TestType = test;
            text.AddText(test.Name.Replace("TestCase", ""));

            var description = test.GetCustomAttribute<DescriptionAttribute>()?.Description;
            if (description != null)
            {
                text.NewLine();
                text.AddText(description, t => t.Font = t.Font.With(size: 15));
            }
        }

        protected TestCaseButton(string header)
            : this()
        {
            text.AddText(header);
        }

        protected override bool Handle(PositionalEvent e)
        {
            switch (e)
            {
                case HoverEvent _:
                    box.FadeTo(1, 150);
                    return true;

                case HoverLostEvent _:
                    box.FadeTo(0.7f, 150);
                    return false;

                default:
                    return base.Handle(e);
            }
        }
    }
}
