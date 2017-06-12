// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Graphics.UserInterface
{
    public class ContextMenuItem : MenuItem
    {
        private readonly Container textContainer;

        /// <summary>
        /// Creates an autosize container which will be positioned at the centre-right of the <see cref="ContextMenuItem"/>.
        /// </summary>
        private readonly FillFlowContainer сontentContainer;

        /// <summary>
        /// Draw width of <see cref="textContainer"/>
        /// </summary>
        public float TextDrawWidth => textContainer.DrawWidth;

        /// <summary>
        /// Draw width of <see cref="сontentContainer"/>
        /// </summary>
        public float ContentDrawWidth => сontentContainer.DrawWidth;

        /// <summary>
        /// Creates a new container with text which will be displayed at the centre-left of this item.
        /// </summary>
        /// <param name="title">The text displayed on this container</param>
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

        protected override Container<Drawable> Content => сontentContainer;

        public ContextMenuItem(string title)
        {
            base.Content.Add(new Drawable[]
            {
                textContainer = CreateTextContainer(title),
                сontentContainer = new FillFlowContainer
                {
                    Direction = FillDirection.Horizontal,
                    AutoSizeAxes = Axes.Both,
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                }
            });
        }
    }
}
