// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Graphics.UserInterface
{
    public class ContextMenuItem : MenuItem
    {
        private readonly Container textContainer;
        private readonly Container contentContainer;

        protected virtual Container CreateTextContainer(string title) => new Container
        {
            AutoSizeAxes = Axes.Both,
            Anchor = Anchor.CentreLeft,
            Origin = Anchor.CentreLeft,
            Children = new Drawable[]
            {
                new SpriteText
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    TextSize = 17,
                    Text = title,
                }
            }
        };

        protected virtual Container CreateContentContainer() => new Container
        {
            AutoSizeAxes = Axes.Both,
            Anchor = Anchor.CentreRight,
            Origin = Anchor.CentreRight,
            Children = new Drawable[]
            {
                new SpriteText
                {
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    TextSize = 17,
                    Text = @"test",
                }
            }
        };

        public new float DrawWidth => textContainer.DrawWidth + contentContainer.DrawWidth;

        public ContextMenuItem(string title)
        {
            Add(textContainer = CreateTextContainer(title));
            Add(contentContainer = CreateContentContainer());
        }
    }
}
