﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osuTK.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Lines;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osuTK.Input;
using osu.Framework.Utils;
using osuTK;

namespace osu.Framework.Tests.Visual.Drawables
{
    [System.ComponentModel.Description("Approximate a hand-drawn path with minimal B-spline control points")]
    public partial class TestSceneInteractivePathDrawing : FrameworkTestScene
    {
        private readonly Path rawDrawnPath;
        private readonly Path approximatedDrawnPath;
        private readonly Path controlPointPath;
        private readonly Container controlPointViz;
        private readonly BoundingBoxVizualizer bbViz;

        private readonly IncrementalBSplineBuilder bSplineBuilder = new IncrementalBSplineBuilder();

        public TestSceneInteractivePathDrawing()
        {
            Child = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    rawDrawnPath = new Path
                    {
                        Colour = Color4.DeepPink,
                        PathRadius = 5,
                    },
                    approximatedDrawnPath = new Path
                    {
                        Colour = Color4.Blue,
                        PathRadius = 3,
                    },
                    controlPointPath = new Path
                    {
                        Colour = Color4.LightGreen,
                        PathRadius = 1,
                        Alpha = 0.5f,
                    },
                    controlPointViz = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Alpha = 0.5f,
                    },
                    bbViz = new BoundingBoxVizualizer
                    {
                        RelativeSizeAxes = Axes.Both,
                    }
                }
            };

            AddStep("Reset path", () =>
            {
                bSplineBuilder.Clear();
                updateViz();
            });

            AddSliderStep($"{nameof(bSplineBuilder.Degree)}", 1, 4, 3, v =>
            {
                bSplineBuilder.Degree = v;
                updateViz();
            });
            AddSliderStep($"{nameof(bSplineBuilder.Tolerance)}", 0f, 3f, 2f, v =>
            {
                bSplineBuilder.Tolerance = v;
                updateViz();
            });
            AddSliderStep($"{nameof(bSplineBuilder.CornerThreshold)}", 0f, 1f, 0.4f, v =>
            {
                bSplineBuilder.CornerThreshold = v;
                updateViz();
            });
            AddSliderStep($"{nameof(approximatedDrawnPath.StartProgress)}", 0f, 1f, 0f, v =>
            {
                approximatedDrawnPath.StartProgress = v;
            });

            AddSliderStep($"{nameof(approximatedDrawnPath.EndProgress)}", 0f, 1f, 1f, v =>
            {
                approximatedDrawnPath.EndProgress = v;
            });
        }

        [BackgroundDependencyLoader]
        private void load(IRenderer renderer)
        {
            bbViz.Texture = renderer.WhitePixel;
        }

        private void updateControlPointsViz()
        {
            controlPointPath.Vertices = bSplineBuilder.ControlPoints.SelectMany(o => o).ToArray();
            controlPointViz.Clear();

            foreach (var segment in bSplineBuilder.ControlPoints)
            {
                foreach (var cp in segment)
                {
                    controlPointViz.Add(new Box
                    {
                        Origin = Anchor.Centre,
                        Size = new Vector2(10),
                        Position = cp,
                        Colour = Color4.LightGreen,
                    });
                }
            }
        }

        protected override bool OnDragStart(DragStartEvent e)
        {
            if (e.Button == MouseButton.Left)
            {
                bSplineBuilder.Clear();
                bSplineBuilder.AddLinearPoint(rawDrawnPath.ToLocalSpace(ToScreenSpace(e.MousePosition)));
                return true;
            }

            return false;
        }

        private void updateViz()
        {
            rawDrawnPath.Vertices = bSplineBuilder.GetInputPath();
            approximatedDrawnPath.Vertices = bSplineBuilder.OutputPath;

            updateControlPointsViz();
        }

        protected override void Update()
        {
            base.Update();
            bbViz.Boxes = approximatedDrawnPath.BoundingBoxes();
        }

        protected override void OnDrag(DragEvent e)
        {
            bSplineBuilder.AddLinearPoint(rawDrawnPath.ToLocalSpace(ToScreenSpace(e.MousePosition)));
            updateViz();
        }

        protected override void OnDragEnd(DragEndEvent e)
        {
            if (e.Button == MouseButton.Left)
                bSplineBuilder.Finish();

            base.OnDragEnd(e);
        }

        private partial class BoundingBoxVizualizer : Sprite
        {
            private List<RectangleF> boxes = new List<RectangleF>();

            public List<RectangleF> Boxes
            {
                get => boxes;
                set
                {
                    boxes = value;
                    Invalidate(Invalidation.DrawNode);
                }
            }

            public BoundingBoxVizualizer()
            {
                RelativeSizeAxes = Axes.Both;
            }

            protected override DrawNode CreateDrawNode() => new BoundingBoxDrawNode(this);

            private class BoundingBoxDrawNode : SpriteDrawNode
            {
                public new BoundingBoxVizualizer Source => (BoundingBoxVizualizer)base.Source;

                public BoundingBoxDrawNode(BoundingBoxVizualizer source)
                    : base(source)
                {
                }

                private readonly List<RectangleF> boxes = new List<RectangleF>();

                public override void ApplyState()
                {
                    base.ApplyState();

                    boxes.Clear();
                    boxes.AddRange(Source.boxes);
                }

                protected override void Blit(IRenderer renderer)
                {
                    ColourInfo colourInfo = DrawColourInfo.Colour;
                    colourInfo.ApplyChild(Color4.Red);

                    foreach (var box in boxes)
                    {
                        var drawQuad = new Quad(
                            Vector2Extensions.Transform(box.TopLeft, DrawInfo.Matrix),
                            Vector2Extensions.Transform(box.TopRight, DrawInfo.Matrix),
                            Vector2Extensions.Transform(box.TopLeft + new Vector2(0, 1), DrawInfo.Matrix),
                            Vector2Extensions.Transform(box.TopRight + new Vector2(0, 1), DrawInfo.Matrix)
                        );

                        renderer.DrawQuad(Texture, drawQuad, colourInfo);

                        drawQuad = new Quad(
                            Vector2Extensions.Transform(box.BottomLeft - new Vector2(0, 1), DrawInfo.Matrix),
                            Vector2Extensions.Transform(box.BottomRight - new Vector2(0, 1), DrawInfo.Matrix),
                            Vector2Extensions.Transform(box.BottomLeft, DrawInfo.Matrix),
                            Vector2Extensions.Transform(box.BottomRight, DrawInfo.Matrix)
                        );

                        renderer.DrawQuad(Texture, drawQuad, colourInfo);

                        drawQuad = new Quad(
                            Vector2Extensions.Transform(box.TopLeft, DrawInfo.Matrix),
                            Vector2Extensions.Transform(box.TopLeft + new Vector2(1, 0), DrawInfo.Matrix),
                            Vector2Extensions.Transform(box.BottomLeft, DrawInfo.Matrix),
                            Vector2Extensions.Transform(box.BottomLeft + new Vector2(1, 0), DrawInfo.Matrix)
                        );

                        renderer.DrawQuad(Texture, drawQuad, colourInfo);

                        drawQuad = new Quad(
                            Vector2Extensions.Transform(box.TopRight - new Vector2(1, 0), DrawInfo.Matrix),
                            Vector2Extensions.Transform(box.TopRight, DrawInfo.Matrix),
                            Vector2Extensions.Transform(box.BottomRight - new Vector2(1, 0), DrawInfo.Matrix),
                            Vector2Extensions.Transform(box.BottomRight, DrawInfo.Matrix)
                        );

                        renderer.DrawQuad(Texture, drawQuad, colourInfo);
                    }
                }

                protected internal override bool CanDrawOpaqueInterior => false;
            }
        }
    }
}
