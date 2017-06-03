// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Graphics.UserInterface
{
    public class ContextMenuItem : MenuItem
    {
        private readonly SpriteText text;

        private readonly Container contentContainer;

        public new float DrawWidth
        {
            get
            {
                return contentContainer.DrawWidth;
            }
        }

        public ContextMenuItem(string title)
        {
            Children = new Drawable[]
            {
                contentContainer = new Container
                {
                    AutoSizeAxes = Axes.Both,
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Children = new Drawable[]
                    {
                        text = new SpriteText
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            TextSize = 17,
                            Text = title,
                        },
                    }
                }
            };
        }
    }
}
