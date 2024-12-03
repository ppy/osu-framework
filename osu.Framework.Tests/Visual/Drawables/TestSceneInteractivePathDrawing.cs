// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Graphics;
using osuTK.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Lines;
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
                }
            };

            updateViz();
            OnUpdate += _ => updateViz();

            AddStep("Reset path", () =>
            {
                bSplineBuilder.Clear();
            });

            AddSliderStep($"{nameof(bSplineBuilder.Degree)}", 1, 4, 3, v =>
            {
                bSplineBuilder.Degree = v;
            });
            AddSliderStep($"{nameof(bSplineBuilder.Tolerance)}", 0f, 3f, 2f, v =>
            {
                bSplineBuilder.Tolerance = v;
            });
            AddSliderStep($"{nameof(bSplineBuilder.CornerThreshold)}", 0f, 1f, 0.4f, v =>
            {
                bSplineBuilder.CornerThreshold = v;
            });
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

        protected override void OnDrag(DragEvent e)
        {
            bSplineBuilder.AddLinearPoint(rawDrawnPath.ToLocalSpace(ToScreenSpace(e.MousePosition)));
        }

        protected override void OnDragEnd(DragEndEvent e)
        {
            if (e.Button == MouseButton.Left)
                bSplineBuilder.Finish();

            base.OnDragEnd(e);
        }
    }
}
