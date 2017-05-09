// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Transforms;
using osu.Framework.Testing;
using osu.Framework.Timing;

namespace osu.Framework.VisualTests.Tests
{
    public class TestCaseCoordinateSpaces : TestCase
    {
        private const int coordinate_space_step = 25;
        private const int coordinate_space_grid_tests = 8;
        private const double scroll_time = 2000;

        public override void Reset()
        {
            base.Reset();

            for (int i = 1; i <= coordinate_space_grid_tests; i++)
            {
                int tempI = i;
                AddStep($"{coordinate_space_step * tempI} coordinate space", () => loadGridTest(tempI));
            }

            AddStep("Scrolling test", loadScrollingTest);
            AddWaitStep((int)Math.Ceiling(scroll_time / TimePerAction) + 2);
        }

        private void loadGridTest(int caseNumber)
        {
            Clear();

            if (caseNumber <= coordinate_space_grid_tests)
            {
                TestContainer c;
                Add(c = new TestContainer
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.Both,
                    Size = new Vector2(0.8f),
                    RelativeCoordinateSpace = new Vector2(coordinate_space_step * caseNumber),
                    Children = new Drawable[]
                    {
                        new CoordinateRelativeContainer
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
            }
        }

        private void loadScrollingTest()
        {
            const float duration = (float)scroll_time / 4;

            Clear();

            Box scrollingBox;
            Add(new TestContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.Y,
                Size = new Vector2(100, 0.8f),
                RelativeCoordinateSpace = new Vector2(1, (float)scroll_time - duration),
                Masking = true,
                Clock = new FramedClock(),
                Children = new Drawable[]
                {
                    new CoordinateRelativeContainer
                    {
                        Name = "Background",
                        RelativeSizeAxes = Axes.Both,
                        Children = new[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = Color4.White,
                                Alpha = 0.1f
                            }
                        }
                    },
                    scrollingBox = new TimeScrollingBox
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.BottomCentre,
                        RelativePositionAxes = Axes.Y,
                        RelativeSizeAxes = Axes.Both,
                        Size = new Vector2(1, duration)
                    }
                }
            });
        }

        private class TimeScrollingBox : Box
        {
            protected override void Update()
            {
                Y = (float)Clock.CurrentTime;
            }
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

        private class CoordinateRelativeContainer : Container
        {
            public CoordinateRelativeContainer()
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