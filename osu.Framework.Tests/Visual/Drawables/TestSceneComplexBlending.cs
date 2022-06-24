// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Logging;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Drawables
{
    public class TestSceneComplexBlending : FrameworkTestScene
    {
        private readonly Dropdown<string> colourModeDropdown;
        private readonly Dropdown<BlendingEquation> colourEquation;
        private readonly Dropdown<BlendingEquation> alphaEquation;
        private readonly BufferedContainer foregroundContainer;

        private readonly FillFlowContainer blendingSrcContainer;
        private readonly FillFlowContainer blendingDestContainer;
        private readonly FillFlowContainer blendingAlphaSrcContainer;
        private readonly FillFlowContainer blendingAlphaDestContainer;

        private readonly Dropdown<BlendingType> blendingSrcDropdown;
        private readonly Dropdown<BlendingType> blendingDestDropdown;
        private readonly Dropdown<BlendingType> blendingAlphaSrcDropdown;
        private readonly Dropdown<BlendingType> blendingAlphaDestDropdown;

        private readonly FillFlowContainer settingsBox;

        private readonly Dictionary<string, BlendingParameters> blendingModes = new Dictionary<string, BlendingParameters>
        {
            { "Inherit", BlendingParameters.Inherit },
            { "Additive", BlendingParameters.Additive },
            { "Mixture", BlendingParameters.Mixture },
            { "Custom", BlendingParameters.Mixture },
        };

        private bool inCustomMode;

        public TestSceneComplexBlending()
        {
            Children = new Drawable[]
            {
                settingsBox = new FillFlowContainer
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
                                colourModeDropdown = new BasicDropdown<string> { Width = 200 }
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

            blendingSrcContainer = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 5),
                Children = new Drawable[]
                {
                    new SpriteText { Text = "Custom: Source" },
                    blendingSrcDropdown = new BasicDropdown<BlendingType> { Width = 200 }
                },
            };

            blendingDestContainer = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 5),
                Children = new Drawable[]
                {
                    new SpriteText { Text = "Custom: Destination" },
                    blendingDestDropdown = new BasicDropdown<BlendingType> { Width = 200 }
                },
            };

            blendingAlphaSrcContainer = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 5),
                Children = new Drawable[]
                {
                    new SpriteText { Text = "Custom: Alpha Source" },
                    blendingAlphaSrcDropdown = new BasicDropdown<BlendingType> { Width = 200 }
                },
            };

            blendingAlphaDestContainer = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 5),
                Children = new Drawable[]
                {
                    new SpriteText { Text = "Custom: Alpha Destination" },
                    blendingAlphaDestDropdown = new BasicDropdown<BlendingType> { Width = 200 }
                },
            };

            colourModeDropdown.Items = blendingModes.Keys;
            colourEquation.Items = (BlendingEquation[])Enum.GetValues(typeof(BlendingEquation));
            alphaEquation.Items = (BlendingEquation[])Enum.GetValues(typeof(BlendingEquation));

            blendingSrcDropdown.Items = (BlendingType[])Enum.GetValues(typeof(BlendingType));
            blendingDestDropdown.Items = (BlendingType[])Enum.GetValues(typeof(BlendingType));
            blendingAlphaSrcDropdown.Items = (BlendingType[])Enum.GetValues(typeof(BlendingType));
            blendingAlphaDestDropdown.Items = (BlendingType[])Enum.GetValues(typeof(BlendingType));

            colourModeDropdown.Current.Value = "Mixture";
            colourEquation.Current.Value = foregroundContainer.Blending.RGBEquation;
            alphaEquation.Current.Value = foregroundContainer.Blending.AlphaEquation;

            blendingSrcDropdown.Current.Value = BlendingType.SrcAlpha;
            blendingDestDropdown.Current.Value = BlendingType.OneMinusSrcAlpha;
            blendingAlphaSrcDropdown.Current.Value = BlendingType.One;
            blendingAlphaDestDropdown.Current.Value = BlendingType.One;

            colourModeDropdown.Current.ValueChanged += _ => updateBlending();
            colourEquation.Current.ValueChanged += _ => updateBlending();
            alphaEquation.Current.ValueChanged += _ => updateBlending();
            blendingSrcDropdown.Current.ValueChanged += _ => updateBlending();
            blendingDestDropdown.Current.ValueChanged += _ => updateBlending();
            blendingAlphaSrcDropdown.Current.ValueChanged += _ => updateBlending();
            blendingAlphaDestDropdown.Current.ValueChanged += _ => updateBlending();
        }

        private void switchToCustomBlending()
        {
            settingsBox.Add(blendingSrcContainer);
            settingsBox.Add(blendingDestContainer);
            settingsBox.Add(blendingAlphaSrcContainer);
            settingsBox.Add(blendingAlphaDestContainer);
        }

        private void switchOffCustomBlending()
        {
            settingsBox.Remove(blendingSrcContainer);
            settingsBox.Remove(blendingDestContainer);
            settingsBox.Remove(blendingAlphaSrcContainer);
            settingsBox.Remove(blendingAlphaDestContainer);
        }

        private void updateBlending()
        {
            if (colourModeDropdown.Current.Value == "Custom")
            {
                if (!inCustomMode)
                    switchToCustomBlending();

                var blending = new BlendingParameters
                {
                    Source = blendingSrcDropdown.Current.Value,
                    Destination = blendingDestDropdown.Current.Value,
                    SourceAlpha = blendingAlphaSrcDropdown.Current.Value,
                    DestinationAlpha = blendingAlphaDestDropdown.Current.Value,
                    RGBEquation = colourEquation.Current.Value,
                    AlphaEquation = alphaEquation.Current.Value
                };

                Logger.Log("Changed blending mode to: " + blending, LoggingTarget.Runtime, LogLevel.Debug);

                foregroundContainer.Blending = blending;

                inCustomMode = true;
            }
            else
            {
                if (inCustomMode)
                    switchOffCustomBlending();

                var blending = blendingModes[colourModeDropdown.Current.Value];

                blending.RGBEquation = colourEquation.Current.Value;
                blending.AlphaEquation = alphaEquation.Current.Value;

                Logger.Log("Changed blending mode to: " + blending, LoggingTarget.Runtime, LogLevel.Debug);

                foregroundContainer.Blending = blending;

                inCustomMode = false;
            }
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
