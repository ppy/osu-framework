// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseBlending : TestCase
    {
        private readonly Dropdown<BlendingMode> colourModeDropdown;
        private readonly Dropdown<BlendingEquation> colourEquation;
        private readonly Dropdown<BlendingEquation> alphaEquation;
        private readonly BufferedContainer foregroundContainer;

        public TestCaseBlending()
        {
            Children = new Drawable[]
            {
                new FillFlowContainer
                {
                    Name = "Settings",
                    AutoSizeAxes = Axes.Both,
                    Y = 50,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 20),
                    Children = new Drawable[]
                    {
                        new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(0, 5),
                            Children = new Drawable[]
                            {
                                new SpriteText { Text = "Blending mode" },
                                colourModeDropdown = new BasicDropdown<BlendingMode> { Width = 200 }
                            }
                        },
                        new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(0, 5),
                            Children = new Drawable[]
                            {
                                new SpriteText { Text = "Blending equation (colour)" },
                                colourEquation = new BasicDropdown<BlendingEquation> { Width = 200 }
                            }
                        },
                        new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(0, 5),
                            Children = new Drawable[]
                            {
                                new SpriteText { Text = "Blending equation (alpha)" },
                                alphaEquation = new BasicDropdown<BlendingEquation> { Width = 200 }
                            }
                        }
                    }
                },
                new SpriteText
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Text = "Behind background"
                },
                new BufferedContainer
                {
                    Name = "Background",
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.Both,
                    FillMode = FillMode.Fit,
                    Size = new Vector2(0.85f),
                    Masking = true,
                    Children = new Drawable[]
                    {
                        new GradientPart(0, Color4.Orange, Color4.Yellow),
                        new GradientPart(1, Color4.Yellow, Color4.Green),
                        new GradientPart(2, Color4.Green, Color4.Cyan),
                        new GradientPart(3, Color4.Cyan, Color4.Blue),
                        new GradientPart(4, Color4.Blue, Color4.Violet),
                        foregroundContainer = new BufferedContainer
                        {
                            Name = "Foreground",
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            RelativeSizeAxes = Axes.Both,
                            Alpha = 0.8f,
                            Children = new[]
                            {
                                new Circle
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    RelativePositionAxes = Axes.Both,
                                    RelativeSizeAxes = Axes.Both,
                                    Size = new Vector2(0.45f),
                                    Y = -0.15f,
                                    Colour = Color4.Cyan
                                },
                                new Circle
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    RelativePositionAxes = Axes.Both,
                                    RelativeSizeAxes = Axes.Both,
                                    Size = new Vector2(0.45f),
                                    X = -0.15f,
                                    Colour = Color4.Magenta
                                },
                                new Circle
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    RelativePositionAxes = Axes.Both,
                                    RelativeSizeAxes = Axes.Both,
                                    Size = new Vector2(0.45f),
                                    X = 0.15f,
                                    Colour = Color4.Yellow
                                },
                            }
                        },
                    }
                },
            };

            colourModeDropdown.Items = Enum.GetNames(typeof(BlendingMode)).Select(n => new KeyValuePair<string, BlendingMode>(n, (BlendingMode)Enum.Parse(typeof(BlendingMode), n)));
            colourEquation.Items = Enum.GetNames(typeof(BlendingEquation)).Select(n => new KeyValuePair<string, BlendingEquation>(n, (BlendingEquation)Enum.Parse(typeof(BlendingEquation), n)));
            alphaEquation.Items = Enum.GetNames(typeof(BlendingEquation)).Select(n => new KeyValuePair<string, BlendingEquation>(n, (BlendingEquation)Enum.Parse(typeof(BlendingEquation), n)));

            colourModeDropdown.Current.Value = foregroundContainer.Blending.Mode;
            colourEquation.Current.Value = foregroundContainer.Blending.RGBEquation;
            alphaEquation.Current.Value = foregroundContainer.Blending.AlphaEquation;

            colourModeDropdown.Current.ValueChanged += v => updateBlending();
            colourEquation.Current.ValueChanged += v => updateBlending();
            alphaEquation.Current.ValueChanged += v => updateBlending();
        }

        private void updateBlending()
        {
            foregroundContainer.Blending = new BlendingParameters
            {
                Mode = colourModeDropdown.Current,
                RGBEquation = colourEquation.Current,
                AlphaEquation = alphaEquation.Current
            };
        }

        private class GradientPart : Box
        {
            public GradientPart(int index, Color4 start, Color4 end)
            {
                RelativeSizeAxes = Axes.Both;
                RelativePositionAxes = Axes.Both;
                Width = 1 / 5f; // Assume 5 gradients
                X = 1 / 5f * index;

                Colour = ColourInfo.GradientHorizontal(start, end);
            }
        }
    }
}
