// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Sprites;
using osuTK.Graphics;

namespace osu.Framework.Graphics.UserInterface
{
    public class BasicTabControl<T> : TabControl<T>
    {
        protected override Dropdown<T> CreateDropdown()
            => new BasicDropdown<T>();

        protected override TabItem<T> CreateTabItem(T value)
            => new BasicTabItem(value);

        public class BasicTabItem : TabItem<T>
        {
            private readonly SpriteText text;

            public override bool IsRemovable => true;

            public BasicTabItem(T value) : base(value)
            {
                AutoSizeAxes = Axes.Both;

                Add(text = new SpriteText
                {
                    Margin = new MarginPadding(2),
                    Text = value.ToString(),
                    TextSize = 18
                });
            }

            protected override void OnActivated()
                => text.Colour = Color4.MediumPurple;

            protected override void OnDeactivated()
                => text.Colour = Color4.White;
        }
    }
}
