// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
            BasicSliderBar<double> rateAdjustSlider;
            SpriteText rateText;

            InternalChild = new GridContainer
            {
                RelativeSizeAxes = Axes.Both,
                ColumnDimensions = new[]
                {
                    new Dimension(GridSizeMode.AutoSize),
                    new Dimension(GridSizeMode.Distributed),
                    new Dimension(GridSizeMode.AutoSize),
                },
                Content = new[]
                {
                    new Drawable[]
                    {
                        new SpriteText
                        {
                            Padding = new MarginPadding(5),
                            Text = "Rate:"
                        },
                        rateAdjustSlider = new BasicSliderBar<double>
                        {
                            RelativeSizeAxes = Axes.Both,
                            Current = browser.PlaybackRate
                        },
                        rateText = new SpriteText
                        {
                            Padding = new MarginPadding(5),
                            Width = 60,
                        },
                    }
                }
            };

            rateAdjustSlider.Current.BindValueChanged(v => rateText.Text = v.ToString("0%"), true);
        }
    }
}
