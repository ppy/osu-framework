// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Reflection;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Testing
{
    internal class Toolbar : CompositeDrawable
    {
        public BasicSliderBar<double> RateAdjustSlider;

        public BasicDropdown<Assembly> AssemblyDropdown;

        public BasicCheckbox RunAllSteps;

        [BackgroundDependencyLoader]
        private void load()
        {
            SpriteText playbackSpeedDisplay;
            InternalChildren = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.Black,
                },
                new Container
                {
                    Padding = new MarginPadding(10),
                    RelativeSizeAxes = Axes.Both,
                    Child = new GridContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        ColumnDimensions = new[]
                        {
                            new Dimension(GridSizeMode.AutoSize),
                            new Dimension(GridSizeMode.Distributed),
                        },
                        Content = new[]
                        {
                            new Drawable[]
                            {
                                new FillFlowContainer
                                {
                                    Spacing = new Vector2(5),
                                    Direction = FillDirection.Horizontal,
                                    RelativeSizeAxes = Axes.Y,
                                    AutoSizeAxes = Axes.X,
                                    Children = new Drawable[]
                                    {
                                        new SpriteText
                                        {
                                            Padding = new MarginPadding(5),
                                            Text = "Current Assembly:"
                                        },
                                        AssemblyDropdown = new BasicDropdown<Assembly>
                                        {
                                            Width = 300,
                                        },
                                        RunAllSteps = new BasicCheckbox
                                        {
                                            LabelText = "Run all steps",
                                            LabelPadding = new MarginPadding { Left = 5, Right = 10 },
                                            AutoSizeAxes = Axes.Y,
                                            Width = 140,
                                            Anchor = Anchor.CentreLeft,
                                            Origin = Anchor.CentreLeft,
                                        },
                                    }
                                },
                                new GridContainer
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
                                            RateAdjustSlider = new BasicSliderBar<double>
                                            {
                                                RelativeSizeAxes = Axes.Both,
                                                Colour = Color4.MediumPurple,
                                                SelectionColor = Color4.White,
                                            },
                                            playbackSpeedDisplay = new SpriteText
                                            {
                                                Padding = new MarginPadding(5),
                                            },
                                        }
                                    }
                                }
                            }
                        },
                    },
                },
            };

            RateAdjustSlider.Current.ValueChanged += v => playbackSpeedDisplay.Text = v.ToString("0%");
        }
    }
}
