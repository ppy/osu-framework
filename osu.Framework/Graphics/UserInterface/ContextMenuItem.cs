// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Graphics.UserInterface
{
    public class ContextMenuItem : MenuItem
    {
        /// <summary>
        /// The draw width of the text of this <see cref="ContextMenuItem"/>.
        /// </summary>
        public float TextDrawWidth => text.DrawWidth;

        /// <summary>
        /// The draw width of the content of this <see cref="ContextMenuItem"/>. This does not include <see cref="TextDrawWidth"/>.
        /// </summary>
        public float ContentDrawWidth => content.DrawWidth;

        protected override Container<Drawable> Content => content;

        private readonly Container text;
        private readonly FillFlowContainer content;

        /// <summary>
        /// Creates a new <see cref="ContextMenuItem"/>.
        /// </summary>
        /// <param name="title">The text to be displayed in this <see cref="ContextMenuItem"/>.</param>
        public ContextMenuItem(string title)
        {
            base.Content.Add(new Drawable[]
            {
                text = CreateTextContainer(title),
                content = new FillFlowContainer
                {
                    Direction = FillDirection.Horizontal,
                    AutoSizeAxes = Axes.Both,
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                }
            });
        }

        /// <summary>
        /// Creates a new container with text which will be displayed at the centre-left of this <see cref="ContextMenuItem"/>.
        /// </summary>
        /// <param name="title">The text to be displayed in this <see cref="ContextMenuItem"/>.</param>
        protected virtual Container CreateTextContainer(string title) => new Container
        {
            AutoSizeAxes = Axes.Both,
            Anchor = Anchor.CentreLeft,
            Origin = Anchor.CentreLeft,
            Child = new SpriteText
            {
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
                TextSize = 17,
                Text = title,
            }
        };
    }
}
