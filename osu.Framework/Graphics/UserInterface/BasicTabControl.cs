﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Graphics.UserInterface
{
    public class BasicTabControl<T> : TabControl<T>
    {
        protected override Dropdown<T> CreateDropdown()
            => new BasicTabControlDropdown();

        protected override TabItem<T> CreateTabItem(T value)
            => new BasicTabItem(value);

        public class BasicTabItem : TabItem<T>
        {
            private readonly SpriteText text;

            public BasicTabItem(T value)
                : base(value)
            {
                AutoSizeAxes = Axes.Both;

                Add(text = new SpriteText
                {
                    Margin = new MarginPadding(2),
                    Text = value.ToString(),
                    Font = new FontUsage(size: 18),
                });
            }

            protected override void OnActivated()
                => text.Colour = Colour4.MediumPurple;

            protected override void OnDeactivated()
                => text.Colour = Colour4.White;
        }

        public class BasicTabControlDropdown : BasicDropdown<T>
        {
            public BasicTabControlDropdown()
            {
                Menu.Anchor = Anchor.TopRight;
                Menu.Origin = Anchor.TopRight;
                Header.Anchor = Anchor.TopRight;
                Header.Origin = Anchor.TopRight;
            }
        }
    }
}
