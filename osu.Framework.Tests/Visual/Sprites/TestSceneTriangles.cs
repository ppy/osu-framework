// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Tests.Visual.Containers;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Sprites
{
    public class TestSceneTriangles : FrameworkTestScene
    {
        private readonly Container testContainer;

        public TestSceneTriangles()
        {
            Add(testContainer = new Container
            {
                RelativeSizeAxes = Axes.Both,
            });

            string[] testNames = { @"Bounding box / input" };

            for (int i = 0; i < testNames.Length; i++)
            {
                int test = i;
                AddStep(testNames[i], delegate { loadTest(test); });
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

            Triangle triangle;

            switch (testType)
            {
                case 0:
                    Container box;

                    testContainer.Add(box = new InfofulBoxAutoSize
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre
                    });

                    addCornerMarkers(box);

                    box.AddRange(new[]
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

    internal class DraggableTriangle : Triangle
    {
        public bool AllowDrag = true;

        protected override void OnDrag(DragEvent e)
        {
            if (!AllowDrag) return;

            Position += e.Delta;
        }

        protected override bool OnDragStart(DragStartEvent e) => AllowDrag;
    }
}
