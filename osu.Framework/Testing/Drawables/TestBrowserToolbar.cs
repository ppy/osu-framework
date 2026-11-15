// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing.Drawables.Sections;
using osu.Framework.Utils;
using osuTK.Graphics;

namespace osu.Framework.Testing.Drawables
{
    internal partial class TestBrowserToolbar : CompositeDrawable
    {
        [BackgroundDependencyLoader]
        private void load(TestBrowser browser)
        {
            const float section_padding = 10;

            InternalChildren = new Drawable[]
            {
                new SafeAreaContainer
                {
                    SafeAreaOverrideEdges = Edges.Top | Edges.Right,
                    RelativeSizeAxes = Axes.Both,
                    Child = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = FrameworkColour.GreenDark,
                    },
                },
                new GridContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding(section_padding),
                    ColumnDimensions = new[]
                    {
                        new Dimension(GridSizeMode.AutoSize),
                        new Dimension(),
                        new Dimension(maxSize: 300),
                        new Dimension(GridSizeMode.AutoSize),
                        new Dimension(GridSizeMode.AutoSize),
                    },
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            new ToolbarRunAllStepsSection { RelativeSizeAxes = Axes.Y },
                            new ToolbarRateSection { RelativeSizeAxes = Axes.Both },
                            new ToolbarVolumeSection { RelativeSizeAxes = Axes.Both },
                            new Container
                            {
                                RelativeSizeAxes = Axes.Y,
                                AutoSizeAxes = Axes.X,
                                Margin = new MarginPadding { Horizontal = section_padding },
                                Children = new Drawable[]
                                {
                                    new Container //Backdrop of the record section
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Padding = new MarginPadding(-section_padding),
                                        Child = new Box
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Colour = FrameworkColour.GreenDarker,
                                        },
                                    },
                                    new ToolbarRecordSection { RelativeSizeAxes = Axes.Y },
                                }
                            },
                            new Container
                            {
                                RelativeSizeAxes = Axes.Y,
                                AutoSizeAxes = Axes.X,
                                Margin = new MarginPadding { Left = section_padding },
                                Children = new Drawable[]
                                {
                                    new Container //Backdrop of the bg section
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Padding = new MarginPadding(-section_padding),
                                        Child = new Box
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Colour = FrameworkColour.GreenDarker.Darken(0.5f),
                                        },
                                    },
                                    new BasicButton
                                    {
                                        Text = "bg",
                                        RelativeSizeAxes = Axes.Y,
                                        Width = 40,
                                        Action = () => browser.CurrentTest.ChangeBackgroundColour(
                                            new ColourInfo
                                            {
                                                TopLeft = new Color4(RNG.NextSingle(1), RNG.NextSingle(1), RNG.NextSingle(1), 1),
                                                TopRight = new Color4(RNG.NextSingle(1), RNG.NextSingle(1), RNG.NextSingle(1), 1),
                                                BottomLeft = new Color4(RNG.NextSingle(1), RNG.NextSingle(1), RNG.NextSingle(1), 1),
                                                BottomRight = new Color4(RNG.NextSingle(1), RNG.NextSingle(1), RNG.NextSingle(1), 1)
                                            }
                                        )
                                    },
                                }
                            }
                        }
                    },
                }
            };
        }
    }
}
