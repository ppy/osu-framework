// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;

namespace osu.Framework.Testing.Drawables.Sections
{
    public class ToolbarRateSection : ToolbarSection
    {
        [BackgroundDependencyLoader]
        private void load(TestBrowser browser)
        {
            Padding = new MarginPadding { Horizontal = 5 };

            BasicSliderBar<double> rateAdjustSlider;
            SpriteText rateText;
            ClickableContainer clickableReset;

            InternalChild = new GridContainer
            {
                RelativeSizeAxes = Axes.Both,
                ColumnDimensions = new[]
                {
                    new Dimension(GridSizeMode.AutoSize),
                    new Dimension(GridSizeMode.AutoSize),
                    new Dimension(),
                },
                Content = new[]
                {
                    new Drawable[]
                    {
                        new SpriteText
                        {
                            Padding = new MarginPadding(5) { Right = 0 },
                            Text = "Rate:",
                            Font = FrameworkFont.Condensed
                        },
                        clickableReset = new ClickableContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Child = rateText = new SpriteText
                            {
                                Padding = new MarginPadding(5),
                                Width = 45,
                                Colour = FrameworkColour.Yellow,
                                Font = FrameworkFont.Condensed
                            },
                        },
                        rateAdjustSlider = new BasicSliderBar<double>
                        {
                            RelativeSizeAxes = Axes.Both,
                            Current = browser.PlaybackRate
                        },
                    }
                }
            };

            rateAdjustSlider.Current.BindValueChanged(e => rateText.Text = e.NewValue.ToString("0%"), true);
            clickableReset.Action = () => rateAdjustSlider.Current.SetDefault();
        }
    }
}
