// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osuTK;

namespace osu.Framework.Tests.Visual.Platform
{
    /// <summary>
    /// Visualises a bindable from <see cref="FrameworkConfigManager"/>, showing its name, value, and flashing it on change.
    /// </summary>
    public partial class FrameworkConfigVisualiser<TValue> : FillFlowContainer
    {
        private readonly FrameworkSetting lookup;
        private readonly SpriteText valueText;

        private Bindable<TValue> bindable = null!;

        public FrameworkConfigVisualiser(FrameworkSetting lookup)
        {
            this.lookup = lookup;
            Spacing = new Vector2(8);
            AutoSizeAxes = Axes.Y;
            RelativeSizeAxes = Axes.X;
            Direction = FillDirection.Horizontal;
            Children = new Drawable[]
            {
                new SpriteText
                {
                    Font = FrameworkFont.Condensed,
                    Text = $"{lookup}:"
                },
                valueText = new SpriteText
                {
                    Font = FrameworkFont.Condensed,
                }
            };
        }

        [BackgroundDependencyLoader]
        private void load(FrameworkConfigManager config)
        {
            bindable = config.GetBindable<TValue>(lookup);
            updateText();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            bindable.BindValueChanged(_ => Scheduler.AddOnce(() =>
            {
                updateText();
                this.FlashColour(Colour4.Yellow, 1000, Easing.OutQuart);
            }));
        }

        private void updateText() => valueText.Text = bindable.ToString() ?? "<null>";
    }
}
