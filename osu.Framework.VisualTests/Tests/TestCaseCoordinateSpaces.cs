// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osu.Framework.Timing;

namespace osu.Framework.VisualTests.Tests
{
    public class TestCaseCoordinateSpaces : TestCase
    {
        public override void Reset()
        {
            base.Reset();

            AddStep("0-1 space", () => loadCase(0));
            AddStep("0-150 space", () => loadCase(1));
            AddStep("50-200 space", () => loadCase(2));
            AddStep("150-(-50) space", () => loadCase(3));
            AddStep("0-300 space", () => loadCase(4));
            AddStep("-250-250 space", () => loadCase(4));
        }

        private void loadCase(int i)
        {
            Clear();

            HorizontalVisualiser h;
            Add(h = new HorizontalVisualiser
            {
                Size = new Vector2(200, 50),
                X = 150
            });

            switch (i)
            {
                case 0:
                    h.RelativeCoordinateSpace = new RectangleF(0, 0, 1, 1);
                    h.CreateMarkerAt(-0.1f);
                    h.CreateMarkerAt(0);
                    h.CreateMarkerAt(0.1f);
                    h.CreateMarkerAt(0.3f);
                    h.CreateMarkerAt(0.7f);
                    h.CreateMarkerAt(0.9f);
                    h.CreateMarkerAt(1f);
                    h.CreateMarkerAt(1.1f);
                    break;
                case 1:
                    h.RelativeCoordinateSpace = new RectangleF(0, 0, 150, 1);
                    h.CreateMarkerAt(0);
                    h.CreateMarkerAt(50);
                    h.CreateMarkerAt(100);
                    h.CreateMarkerAt(150);
                    h.CreateMarkerAt(200);
                    h.CreateMarkerAt(250);
                    break;
                case 2:
                    h.RelativeCoordinateSpace = new RectangleF(50, 0, 150, 1);
                    h.CreateMarkerAt(0);
                    h.CreateMarkerAt(50);
                    h.CreateMarkerAt(100);
                    h.CreateMarkerAt(150);
                    h.CreateMarkerAt(200);
                    h.CreateMarkerAt(250);
                    break;
                case 3:
                    h.RelativeCoordinateSpace = RectangleF.FromLTRB(150, 0, -50, 1);
                    h.CreateMarkerAt(0);
                    h.CreateMarkerAt(50);
                    h.CreateMarkerAt(100);
                    h.CreateMarkerAt(150);
                    h.CreateMarkerAt(200);
                    h.CreateMarkerAt(250);
                    break;
                case 4:
                    h.RelativeCoordinateSpace = RectangleF.FromLTRB(-250, 0, 250, 1);
                    h.CreateMarkerAt(-300);
                    h.CreateMarkerAt(-200);
                    h.CreateMarkerAt(-100);
                    h.CreateMarkerAt(0);
                    h.CreateMarkerAt(100);
                    h.CreateMarkerAt(200);
                    h.CreateMarkerAt(300);
                    break;
            }
        }

        private class HorizontalVisualiser : Visualiser
        {
            protected override void Update()
            {
                base.Update();

                Left.Text = $"X = {RelativeCoordinateSpace.Left.ToString()}";
                Right.Text = $"X = {RelativeCoordinateSpace.Right.ToString()}";
            }
        }

        private abstract class Visualiser : Container
        {
            public new RectangleF RelativeCoordinateSpace
            {
                get { return innerContainer.RelativeCoordinateSpace; }
                set { innerContainer.RelativeCoordinateSpace = value; }
            }

            private readonly Container innerContainer;

            protected readonly SpriteText Left;
            protected readonly SpriteText Right;

            public Visualiser()
            {
                Height = 50;

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        Name = "Left marker",
                        Colour = Color4.Gray,
                        RelativeSizeAxes = Axes.Y,
                    },
                    Left = new SpriteText
                    {
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.TopCentre,
                        Y = 6
                    },
                    new Box
                    {
                        Name = "Centre line",
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Colour = Color4.Gray,
                        RelativeSizeAxes = Axes.X
                    },
                    innerContainer = new Container
                    {
                        RelativeSizeAxes = Axes.Both
                    },
                    new Box
                    {
                        Name = "Right marker",
                        Anchor = Anchor.TopRight,
                        Origin = Anchor.TopRight,
                        Colour = Color4.Gray,
                        RelativeSizeAxes = Axes.Y
                    },
                    Right = new SpriteText
                    {
                        Anchor = Anchor.BottomRight,
                        Origin = Anchor.TopCentre,
                        Y = 6
                    },
                };
            }

            public void CreateMarkerAt(float x)
            {
                innerContainer.Add(new Container
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.Centre,
                    RelativePositionAxes = Axes.Both,
                    AutoSizeAxes = Axes.Both,
                    X = x,
                    Colour = Color4.Yellow,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            Name = "Centre marker horizontal",
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Size = new Vector2(8, 1)
                        },
                        new Box
                        {
                            Name = "Centre marker vertical",
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Size = new Vector2(1, 8)
                        },
                        new SpriteText
                        {
                            Anchor = Anchor.BottomCentre,
                            Origin = Anchor.TopCentre,
                            Y = 6,
                            BypassAutoSizeAxes = Axes.Both,
                            Text = x.ToString()
                        }
                    }
                });
            }
        }
    }
}