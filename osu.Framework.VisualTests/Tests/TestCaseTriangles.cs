// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics;
using osu.Framework.Input;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.GameModes.Testing;
using Triangle = osu.Framework.Graphics.Sprites.Triangle;

namespace osu.Framework.VisualTests.Tests
{
    class TestCaseTriangles : TestCase
    {
        public override string Name => @"Triangles";
        public override string Description => @"Various scenarios which potentially challenge triangles.";

        private Container testContainer;

        public override void Reset()
        {
            base.Reset();

            Add(testContainer = new Container()
            {
                RelativeSizeAxes = Axes.Both,
            });

            string[] testNames = new[]
            {
                @"Bounding box / input",
            };

            for (int i = 0; i < testNames.Length; i++)
            {
                int test = i;
                AddButton(testNames[i], delegate { loadTest(test); });
            }

            loadTest(0);
            addCrosshair();
        }

        private void addCrosshair()
        {
            Add(new Box
            {
                Colour = Color4.Black,
                Size = new Vector2(22, 4),
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre
            });

            Add(new Box
            {
                Colour = Color4.Black,
                Size = new Vector2(4, 22),
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre
            });

            Add(new Box
            {
                Colour = Color4.WhiteSmoke,
                Size = new Vector2(20, 2),
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre
            });

            Add(new Box
            {
                Colour = Color4.WhiteSmoke,
                Size = new Vector2(2, 20),
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre
            });
        }

        private void loadTest(int testType)
        {
            testContainer.Clear();

            Container box;
            Triangle triangle;

            switch (testType)
            {
                case 0:
                    testContainer.Add(box = new InfofulBoxAutoSize
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre
                    });

                    addCornerMarkers(box);

                    box.Add(new[]
                    {
                        new DraggableTriangle
                        {
                            //chameleon = true,
                            Position = new Vector2(0, 0),
                            Size = new Vector2(25, 25),
                            Origin = Anchor.Centre,
                            Anchor = Anchor.Centre,
                            Colour = Color4.Blue,
                        },
                        triangle = new DraggableTriangle
                        {
                            Size = new Vector2(250, 250),
                            Alpha = 0.5f,
                            Origin = Anchor.Centre,
                            Anchor = Anchor.Centre,
                            Colour = Color4.DarkSeaGreen,
                        }
                    });

                    triangle.OnUpdate += delegate { triangle.Rotation += 0.05f; };
                    break;
            }

#if DEBUG
            //if (toggleDebugAutosize.State)
            //    testContainer.Children.FindAll(c => c.HasAutosizeChildren).ForEach(c => c.AutoSizeDebug = true);
#endif
        }

        private void addCornerMarkers(Container box, int size = 50, Color4? colour = null)
        {
            box.Add(new DraggableTriangle
            {
                //chameleon = true,
                Size = new Vector2(size, size),
                Origin = Anchor.TopLeft,
                Anchor = Anchor.TopLeft,
                AllowDrag = false,
                Depth = -2,
                Colour = colour ?? Color4.Red,
            });

            box.Add(new DraggableTriangle
            {
                //chameleon = true,
                Size = new Vector2(size, size),
                Origin = Anchor.TopRight,
                Anchor = Anchor.TopRight,
                AllowDrag = false,
                Depth = -2,
                Colour = colour ?? Color4.Red,
            });

            box.Add(new DraggableTriangle
            {
                //chameleon = true,
                Size = new Vector2(size, size),
                Origin = Anchor.BottomLeft,
                Anchor = Anchor.BottomLeft,
                AllowDrag = false,
                Depth = -2,
                Colour = colour ?? Color4.Red,
            });

            box.Add(new DraggableTriangle
            {
                //chameleon = true,
                Size = new Vector2(size, size),
                Origin = Anchor.BottomRight,
                Anchor = Anchor.BottomRight,
                AllowDrag = false,
                Depth = -2,
                Colour = colour ?? Color4.Red,
            });
        }
    }

    class DraggableTriangle : Triangle
    {
        public bool AllowDrag = true;
        public override bool HandleInput => true;

        protected override bool OnDrag(InputState state)
        {
            if (!AllowDrag) return false;

            Position += state.Mouse.Delta;
            return true;
        }

        protected override bool OnDragEnd(InputState state)
        {
            return true;
        }

        protected override bool OnDragStart(InputState state) => AllowDrag;
    }
}
