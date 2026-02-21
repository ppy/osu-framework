// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;

namespace osu.Framework.Testing.Drawables.Sections
{
    public partial class ToolbarVolumeSection : ToolbarSection
    {
        [BackgroundDependencyLoader]
        private void load(FrameworkConfigManager config)
        {
            Padding = new MarginPadding { Right = 10 };

            BasicSliderBar<double> volumeSlider;
            SpriteText volumeText;
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
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Padding = new MarginPadding(5) { Right = 0 },
                            Text = "Volume:",
                            Font = FrameworkFont.Condensed
                        },
                        clickableReset = new ClickableContainer
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            AutoSizeAxes = Axes.Both,
                            Child = volumeText = new SpriteText
                            {
                                Padding = new MarginPadding(5),
                                Width = 50,
                                Colour = FrameworkColour.Yellow,
                                Font = FrameworkFont.Condensed
                            },
                        },
                        volumeSlider = new BasicSliderBar<double>
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            RelativeSizeAxes = Axes.Both,
                            Current = config.GetBindable<double>(FrameworkSetting.VolumeUniversal),
                        }
                    }
                }
            };

            volumeSlider.Current.BindValueChanged(e => volumeText.Text = e.NewValue.ToString("0%"), true);
            clickableReset.Action = () => volumeSlider.Current.Value = 0.25f;
        }
    }
}
