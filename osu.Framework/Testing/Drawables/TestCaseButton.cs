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
using osu.Framework.Graphics.Sprites;
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

        public virtual bool Current
        {
            set
            {
                const float transition_duration = 100;

                if (value)
                {
                    this.TransformTo(nameof(contentPadding), 0f, transition_duration);
                    text.FadeColour(Color4.Black, transition_duration);
                }
                else
                {
                    this.TransformTo(nameof(contentPadding), LEFT_TEXT_PADDING, transition_duration);
                    text.FadeColour(Color4.White, transition_duration);
                }
            }
        }

        private float contentPadding
        {
            get => content.Padding.Right;
            set => content.Padding = new MarginPadding { Right = value };
        }

        protected override Container<Drawable> Content => content;

        private readonly Box box;
        private readonly Container content;
        private readonly TextFlowContainer text;
        public readonly Type TestType;

        public const float LEFT_TEXT_PADDING = 16;

        private TestCaseButton()
        {
            AutoSizeAxes = Axes.Y;
            RelativeSizeAxes = Axes.X;

            Padding = new MarginPadding { Bottom = 3 };

            InternalChildren = new Drawable[]
            {
                content = new Container
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Padding = new MarginPadding { Right = LEFT_TEXT_PADDING },
                    Child = text = new TextFlowContainer(s => s.Font = new FontUsage("RobotoCondensed", weight: "Regular", size: 14f))
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Padding = new MarginPadding
                        {
                            Top = 4,
                            Left = LEFT_TEXT_PADDING,
                            Right = 4,
                            Bottom = 5,
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

        protected override bool OnHover(HoverEvent e)
        {
            //box.FadeTo(1, 150);
            return true;
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            //box.FadeTo(0.7f, 150);
            base.OnHoverLost(e);
        }
    }
}
