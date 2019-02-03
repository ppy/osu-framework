// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Reflection;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK.Graphics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using Container = osu.Framework.Graphics.Containers.Container;

namespace osu.Framework.Testing.Drawables
{
    internal class TestCaseButtonGroup : VisibilityContainer, IHasFilterableChildren
    {
        public IEnumerable<string> FilterTerms => header.FilterTerms;

        public bool MatchingFilter
        {
            set
            {
                header.MatchingFilter = value;
                Alpha = value ? 1 : 0;
            }
        }

        public IEnumerable<IFilterable> FilterableChildren => buttonFlow.Children;

        private readonly TestCaseButton header;
        private readonly SpriteText headerSprite;
        private readonly FillFlowContainer<TestCaseButton> buttonFlow;

        public readonly TestGroup Group;

        public Type Current
        {
            set
            {
                var contains = Group.TestTypes.Contains(value);
                header.Current = contains;
                if (contains) Show();

                if (Group.TestTypes.Length > 1)
                {
                    buttonFlow.Skip(1).ForEach(btn => btn.Current = btn.TestType == value);
                }
            }
        }

        public TestCaseButtonGroup(Action<Type> loadTest, TestGroup group)
        {
            var tests = group.TestTypes;
            if (tests.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(tests), tests.Length, "Type array must not be empty!");

            Group = group;

            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            Child = buttonFlow = new FillFlowContainer<TestCaseButton>
            {
                Direction = FillDirection.Vertical,
                AutoSizeAxes = Axes.Y,
                RelativeSizeAxes = Axes.X
            };

            if (tests.Length == 1)
            {
                var test = tests[0];
                buttonFlow.Add(header = new TestCaseButton(test));
                header.Action = () => loadTest(test);
            }
            else
            {
                buttonFlow.Add(header = new TestCaseButton(group.Name.Replace("TestCase", "")));
                header.Action = ToggleVisibility;
                header.Add(headerSprite = new SpriteText
                {
                    Text = "+",
                    TextSize = 40,
                    Margin = new MarginPadding { Right = 5, Top = -10 },
                    BypassAutoSizeAxes = Axes.Y,
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                });

                foreach (var test in tests)
                {
                    buttonFlow.Add(new TestCaseButton(test)
                    {
                        Anchor = Anchor.TopRight,
                        Origin = Anchor.TopRight,
                        Width = 0.9f,
                        Action = () => loadTest(test)
                    });
                }
            }
        }

        public override bool PropagatePositionalInputSubTree => true;

        protected override void PopIn()
        {
            if (Group.TestTypes.Length > 1)
            {
                headerSprite.Text = "-";
                buttonFlow.ForEach(d =>
                {
                    if (d != header) d.Collapsed = false;
                });
            }
        }

        protected override void PopOut()
        {
            if (Group.TestTypes.Length > 1)
            {
                headerSprite.Text = "+";
                buttonFlow.ForEach(d =>
                {
                    if (d != header) d.Collapsed = true;
                });
            }
        }

        public Type SelectFirst()
        {
            if (Group.TestTypes.Length == 1) return Group.TestTypes[0];

            for (int i = 1; i < buttonFlow.Count; i++)
            {
                if (buttonFlow[i].IsPresent)
                    return Group.TestTypes[i - 1];
            }

            return Group.TestTypes[0];
        }

        private class TestCaseButton : ClickableContainer, IFilterable
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
                if(collapsed || !matchingFilter)
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
                        box.FadeColour(new Color4(140, 140, 140, 255), transition_duration);
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
                Masking = true;
                AutoSizeAxes = Axes.Y;
                RelativeSizeAxes = Axes.X;
                CornerRadius = 5;

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
                    text.AddText(description, t => t.TextSize = 15);
                }
            }

            public TestCaseButton(string header)
                : this()
            {
                text.AddText(header);
            }

            protected override bool OnHover(HoverEvent e)
            {
                box.FadeTo(1, 150);
                return true;
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                box.FadeTo(0.7f, 150);
                base.OnHoverLost(e);
            }
        }

        public class TestGroup
        {
            public string Name { get; set; }
            public Type[] TestTypes { get; set; }
        }
    }
}
