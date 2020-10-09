// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osuTK;

namespace osu.Framework.Graphics.UserInterface
{
    public abstract class DirectoryListingItem : CompositeDrawable
    {
        private readonly string displayName;
        public const float HEIGHT = 20;
        protected const float FONT_SIZE = 16;
        protected abstract string FallbackName { get; }
        protected abstract IconUsage? Icon { get; }
        protected FillFlowContainer Flow;

        protected DirectoryListingItem(string displayName = null)
        {
            this.displayName = displayName;
        }

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

            Flow.Add(new SpriteText
            {
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
                Text = displayName ?? FallbackName,
                Font = FrameworkFont.Regular.With(size: FONT_SIZE)
            });
        }
    }
}