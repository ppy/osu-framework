// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osuTK;

namespace osu.Framework.Graphics.UserInterface
{
    public abstract class DirectorySelectorItem : CompositeDrawable
    {
        private readonly string displayName;

        /// <summary>
        /// Gets or sets the font size of this <see cref="DirectorySelectorItem"/>'s icon and text.
        /// </summary>
        protected const float FONT_SIZE = 16;

        /// <summary>
        /// The display name of this <see cref="DirectorySelectorItem"/> to fallback to when a display name is not provided.
        /// </summary>
        protected abstract string FallbackName { get; }

        /// <summary>
        /// The icon of this <see cref="DirectorySelectorItem"/> to use.
        /// </summary>
        protected abstract IconUsage? Icon { get; }

        protected FillFlowContainer Flow;

        protected DirectorySelectorItem(string displayName = null)
        {
            this.displayName = displayName;
        }

        /// <summary>
        /// Creates the sprite text to be used for the item text.
        /// </summary>
        protected virtual SpriteText CreateSpriteText() => new SpriteText();

        /// <summary>
        /// Called when this <see cref="DirectorySelectorItem"/> is a representation of a hidden item.
        /// Used to customize the appearance of hidden items.
        /// </summary>
        protected virtual void ApplyHiddenState() => Alpha = 0.5f;

        [BackgroundDependencyLoader]
        private void load()
        {
            AutoSizeAxes = Axes.Both;

            InternalChild = Flow = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Margin = new MarginPadding { Vertical = 2, Horizontal = 5 },
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(5),
            };

            if (Icon.HasValue)
            {
                Flow.Add(new SpriteIcon
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Icon = Icon.Value,
                    Size = new Vector2(FONT_SIZE)
                });
            }

            Flow.Add(CreateSpriteText().With(text =>
            {
                text.Anchor = Anchor.CentreLeft;
                text.Origin = Anchor.CentreLeft;
                text.Text = displayName ?? FallbackName;
            }));
        }
    }
}
