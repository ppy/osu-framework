// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Transforms;
using osu.Framework.Testing;

namespace osu.Framework.VisualTests.Tests
{
    public class TestCaseCoordinateSpaces : TestCase
    {
        public override void Reset()
        {
            base.Reset();

            CoordinateSpaceContainer c;
            Add(c = new CoordinateSpaceContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.Both,
                Size = new Vector2(0.8f),
                CoordinateSpace = new Vector2(50),
                Children = new Drawable[]
                {
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Masking = true,
                        BorderColour = Color4.Green,
                        BorderThickness = 5,
                        Children = new[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Alpha = 0,
                                AlwaysPresent = true
                            },
                            new Box
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                RelativeSizeAxes = Axes.Y,
                                Width = 5,
                                Colour = Color4.Green
                            },
                            new Box
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                RelativeSizeAxes = Axes.X,
                                Height = 5,
                                Colour = Color4.Green
                            }
                        }
                    },
                    // Static diagonal
                    new DescriptiveBox { Position = new Vector2(0) },
                    new DescriptiveBox { Position = new Vector2(25) },
                    new DescriptiveBox { Position = new Vector2(50) },
                    new DescriptiveBox { Position = new Vector2(75) },
                    new DescriptiveBox { Position = new Vector2(100) },
                    // Static centre crosshair
                    new DescriptiveBox
                    {
                        Anchor = Anchor.Centre,
                        Position = new Vector2(10, 0)
                    },
                    new DescriptiveBox
                    {
                        Anchor = Anchor.Centre,
                        Position = new Vector2(-10, 0)
                    },
                    new DescriptiveBox
                    {
                        Anchor = Anchor.Centre,
                        Position = new Vector2(0, 10)
                    },
                    new DescriptiveBox
                    {
                        Anchor = Anchor.Centre,
                        Position = new Vector2(0, -10)
                    },
                    // Static relative
                    new DescriptiveBox
                    {
                        RelativePositionAxes = Axes.Both,
                        Anchor = Anchor.BottomLeft,
                    },
                    new DescriptiveBox
                    {
                        RelativePositionAxes = Axes.Both,
                        Anchor = Anchor.BottomLeft,
                        Position = new Vector2(0.125f, -0.125f)
                    },
                    new DescriptiveBox
                    {
                        RelativePositionAxes = Axes.Both,
                        Anchor = Anchor.BottomLeft,
                        Position = new Vector2(0.25f, -0.25f)
                    },
                    new DescriptiveBox
                    {
                        RelativePositionAxes = Axes.Both,
                        Anchor = Anchor.BottomLeft,
                        Position = new Vector2(0.375f, -0.375f)
                    },
                    new DescriptiveBox
                    {
                        Anchor = Anchor.Centre,
                    }
                }
            });

            AddStep("25 coordinate space", () => c.TransformCoordinateSpaceTo(new Vector2(25), 200));
            AddStep("50 coordinate space", () => c.TransformCoordinateSpaceTo(new Vector2(50), 200));
            AddStep("75 coordinate space", () => c.TransformCoordinateSpaceTo(new Vector2(75), 200));
            AddStep("100 coordinate space", () => c.TransformCoordinateSpaceTo(new Vector2(100), 200));
            AddStep("150 coordinate space", () => c.TransformCoordinateSpaceTo(new Vector2(150), 200));
            AddStep("200 coordinate space", () => c.TransformCoordinateSpaceTo(new Vector2(200), 200));
        }

        private class DescriptiveBox : Container
        {
            private SpriteText descriptionText;

            public DescriptiveBox()
            {
                Origin = Anchor.Centre;

                Size = new Vector2(32);

                Children = new Drawable[]
                {
                    new Box
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        RelativeSizeAxes = Axes.Y,
                        Width = 2,
                        Colour = Color4.Red
                    },
                    new Box
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        RelativeSizeAxes = Axes.X,
                        Height = 2,
                        Colour = Color4.Red
                    },
                    descriptionText = new SpriteText
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        BypassAutoSizeAxes = Axes.Both,
                        Y = -18,
                        TextSize = 18
                    }
                };
            }

            protected override void Update()
            {
                descriptionText.Text = $"{Anchor.ToString()} @ {Position.ToString()} (rel: {RelativePositionAxes.ToString()})";
            }
        }
    }
}