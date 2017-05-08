// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
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

            TestContainer c;
            Add(c = new TestContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.Both,
                Size = new Vector2(0.8f),
                RelativeCoordinateSpace = new Vector2(50),
                Children = new Drawable[]
                {
                    new AlwaysRelativeContainer
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
                    // Diagonal
                    new DescriptiveBox { Position = new Vector2(0) },
                    new DescriptiveBox { Position = new Vector2(25) },
                    new DescriptiveBox { Position = new Vector2(50) },
                    new DescriptiveBox { Position = new Vector2(75) },
                    new DescriptiveBox { Position = new Vector2(100) },
                    // Centre crosshair
                    new DescriptiveBox
                    {
                        Anchor = Anchor.Centre
                    },
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
                    }
                }
            });

            AddStep("25 coordinate space", () => c.TransformRelativeCoordinateSpaceTo(new Vector2(25), 200));
            AddStep("50 coordinate space", () => c.TransformRelativeCoordinateSpaceTo(new Vector2(50), 200));
            AddStep("75 coordinate space", () => c.TransformRelativeCoordinateSpaceTo(new Vector2(75), 200));
            AddStep("100 coordinate space", () => c.TransformRelativeCoordinateSpaceTo(new Vector2(100), 200));
            AddStep("150 coordinate space", () => c.TransformRelativeCoordinateSpaceTo(new Vector2(150), 200));
            AddStep("200 coordinate space", () => c.TransformRelativeCoordinateSpaceTo(new Vector2(200), 200));
        }

        private class TestContainer : Container
        {
            public override void Add(Drawable drawable)
            {
                base.Add(drawable);
            }

            public void TransformRelativeCoordinateSpaceTo(Vector2 newCoordinateSpace, double duration = 0, EasingTypes easing = EasingTypes.None)
            {
                TransformTo(() => RelativeCoordinateSpace, newCoordinateSpace, duration, easing, new TransformRelativeCoordinateSpace());
            }

            private class TransformRelativeCoordinateSpace : TransformVector
            {
                public override void Apply(Drawable d)
                {
                    base.Apply(d);

                    var c = d as TestContainer;
                    c.RelativeCoordinateSpace = CurrentValue;
                }
            }
        }

        private class AlwaysRelativeContainer : Container
        {
            public AlwaysRelativeContainer()
            {
                RelativeSizeAxes = Axes.Both;
                RelativePositionAxes = Axes.Both;
            }

            protected override void Update()
            {
                Size = Parent.RelativeCoordinateSpace;
            }
        }

        private class DescriptiveBox : Container
        {
            private SpriteText descriptionText;

            public DescriptiveBox()
            {
                Origin = Anchor.Centre;
                RelativePositionAxes = Axes.Both;

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