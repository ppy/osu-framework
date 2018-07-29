// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;

namespace osu.Framework.Testing.Drawables
{
    public class ToolbarRateSection : CompositeDrawable
    {
        private readonly Bindable<double> rate = new Bindable<double>();

        [BackgroundDependencyLoader]
        private void load(TestBrowser.RateBindable rate)
        {
            this.rate.BindTo(rate);

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
                        },
                        rateText = new SpriteText
                        {
                            Padding = new MarginPadding(5),
                        },
                    }
                }
            };

            rateAdjustSlider.Current.BindTo(rate);
            rateAdjustSlider.Current.BindValueChanged(v => rateText.Text = v.ToString("0%"), true);
        }
    }
}
