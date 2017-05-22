// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osu.Framework.Timing;

namespace osu.Framework.VisualTests.Tests
{
    public class TestCaseCoordinateSpaces : TestCase
    {
        private const float coordinate_space_step = 25;
        private const float coordinate_space_max = 200;
        private const double scroll_time = 2000;

        public override void Reset()
        {
            base.Reset();

            for (float c = coordinate_space_step; c <= coordinate_space_max; c += coordinate_space_step)
            {
                float tempC = c;
                AddStep($"{tempC} coordinate space", () => loadGridTest(tempC));
            }

            AddStep("Scrolling test", loadScrollingTest);
            AddWaitStep((int)Math.Ceiling(scroll_time / TimePerAction) + 2);
        }

        private void loadGridTest(float coordinateSpace)
        {
            Clear();

            Container c;
            Add(c = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.Both,
                Size = new Vector2(0.8f),
                RelativeCoordinateSpace = new Vector2(coordinateSpace),
                Children = new Drawable[]
                {
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Size = new Vector2(coordinateSpace),
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
                    // Centre crosshair
                    new Marker
                    {
                        Anchor = Anchor.Centre
                    },
                    new Marker
                    {
                        Anchor = Anchor.Centre,
                        Position = new Vector2(10, 0)
                    },
                    new Marker
                    {
                        Anchor = Anchor.Centre,
                        Position = new Vector2(-10, 0)
                    },
                    new Marker
                    {
                        Anchor = Anchor.Centre,
                        Position = new Vector2(0, 10)
                    },
                    new Marker
                    {
                        Anchor = Anchor.Centre,
                        Position = new Vector2(0, -10)
                    }
                }
            });

            for (float i = coordinate_space_max / 2; i >= 0; i -= coordinate_space_step)
            {
                c.Add(new Marker
                {
                    Anchor = Anchor.TopLeft,
                    Position = new Vector2(i)
                });

                c.Add(new Marker
                {
                    Anchor = Anchor.TopRight,
                    Position = new Vector2(-i, i)
                });

                c.Add(new Marker
                {
                    Anchor = Anchor.BottomLeft,
                    Position = new Vector2(i, -i)
                });

                c.Add(new Marker
                {
                    Anchor = Anchor.BottomRight,
                    Position = new Vector2(-i, -i)
                });
            }
        }

        private void loadScrollingTest()
        {
            const float duration = (float)scroll_time / 4;

            // The amount of time which the area that the box scrolls through spans.
            // Nothing special is done by subtracting the duration here - scrollingBox is BottomCentre origin with Height = duration
            // and it should be outside of the container at Y = scroll_time for the purpose of demonstrating this test
            const float time_span = (float)scroll_time - duration;

            Clear();

            Add(new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.Y,
                Size = new Vector2(100, 0.8f),
                RelativeCoordinateSpace = new Vector2(1, time_span),
                Masking = true,
                Clock = new FramedClock(),
                Children = new Drawable[]
                {
                    new Container
                    {
                        Name = "Background",
                        RelativeSizeAxes = Axes.Both,
                        Size = new Vector2(1, time_span),
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
                    new TimeScrollingBox
                    {
                        Name = "Scrolling box",
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.BottomCentre,
                        RelativePositionAxes = Axes.Y,
                        RelativeSizeAxes = Axes.Both,
                        Height = duration
                    }
                }
            });
        }

        /// <summary>
        /// A box that scrolls using the current time as its Y-position.
        /// </summary>
        private class TimeScrollingBox : Box
        {
            protected override void Update()
            {
                Y = (float)Clock.CurrentTime;
            }
        }

        /// <summary>
        /// A drawable which marks a point in a space with a description of its position.
        /// </summary>
        private class Marker : Container
        {
            private readonly SpriteText descriptionText;

            public Marker()
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