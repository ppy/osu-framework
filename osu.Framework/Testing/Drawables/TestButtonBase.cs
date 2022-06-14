// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;
using osuTK.Graphics;
using Container = osu.Framework.Graphics.Containers.Container;

namespace osu.Framework.Testing.Drawables
{
    internal abstract class TestButtonBase : ClickableContainer, IFilterable
    {
        public IEnumerable<LocalisableString> FilterTerms => text.Children.OfType<IHasFilterTerms>().SelectMany(c => c.FilterTerms);

        private bool matchingFilter = true;

        public bool MatchingFilter
        {
            get => matchingFilter;
            set
            {
                matchingFilter = value;
                updateVisibility();
            }
        }

        public bool FilteringActive { get; set; }

        private readonly Container content;
        private readonly TextFlowContainer text;
        public readonly Type TestType;

        public const float LEFT_TEXT_PADDING = 16;

        protected const float TRANSITION_DURATION = 100;

        protected override Container<Drawable> Content => content;

        private TestButtonBase()
        {
            AutoSizeAxes = Axes.Y;
            RelativeSizeAxes = Axes.X;

            Padding = new MarginPadding { Bottom = 3 };

            InternalChildren = new Drawable[]
            {
                new Container
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Children = new Drawable[]
                    {
                        new SafeAreaContainer
                        {
                            SafeAreaOverrideEdges = Edges.Left,
                            RelativeSizeAxes = Axes.Both,
                            Child = content = new Container { RelativeSizeAxes = Axes.Both },
                        },
                        text = new TextFlowContainer(s => s.Font = FrameworkFont.Condensed.With(size: 14f))
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Padding = new MarginPadding
                            {
                                Top = 4,
                                Left = LEFT_TEXT_PADDING,
                                Right = 4 + LEFT_TEXT_PADDING,
                                Bottom = 5,
                            },
                        }
                    }
                },
            };
        }

        protected TestButtonBase(Type test)
            : this()
        {
            TestType = test;
            text.AddText(TestScene.RemovePrefix(test.Name));

            string description = test.GetCustomAttribute<DescriptionAttribute>()?.Description;

            if (description != null)
            {
                text.NewLine();
                text.AddText(description, t =>
                {
                    t.Font = t.Font.With("Roboto", 11);
                    t.Colour = FrameworkColour.Yellow;
                });
            }
        }

        protected TestButtonBase(string header)
            : this()
        {
            text.AddText(header);
        }

        private bool collapsed;

        public virtual bool Collapsed
        {
            set
            {
                if (collapsed == value) return;

                collapsed = value;
                updateVisibility();
            }
        }

        private void updateVisibility()
        {
            if (FilteringActive)
            {
                if (matchingFilter)
                    Show();
                else
                    Hide();
            }
            else
            {
                if (Current || !collapsed)
                    Show();
                else
                    Hide();
            }
        }

        private bool current;

        public virtual bool Current
        {
            get => current;
            set
            {
                if (current == value)
                    return;

                current = value;

                text.FadeColour(value ? Color4.Black : Color4.White, TRANSITION_DURATION);
                updateVisibility();
            }
        }
    }
}
