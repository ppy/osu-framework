// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Lines;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Utils;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Drawables
{
    public partial class TestScenePiecewiseLinearToBSpline : GridTestScene
    {
        private int numControlPoints = 5;
        private int degree = 2;
        private int numTestPoints = 100;
        private int maxIterations = 100;
        private float learningRate = 8;
        private float b1 = 0.8f;
        private float b2 = 0.99f;

        private readonly List<DoubleApproximatedPathTest> doubleApproximatedPathTests = new List<DoubleApproximatedPathTest>();

        public TestScenePiecewiseLinearToBSpline()
            : base(2, 2)
        {
            doubleApproximatedPathTests.Add(new DoubleApproximatedPathTest(PathApproximator.BezierToPiecewiseLinear));
            Cell(0).AddRange(new[]
            {
                createLabel(nameof(PathApproximator.BezierToPiecewiseLinear)),
                doubleApproximatedPathTests[^1],
            });

            doubleApproximatedPathTests.Add(new DoubleApproximatedPathTest(PathApproximator.CatmullToPiecewiseLinear));
            Cell(1).AddRange(new[]
            {
                createLabel(nameof(PathApproximator.CatmullToPiecewiseLinear)),
                doubleApproximatedPathTests[^1],
            });

            doubleApproximatedPathTests.Add(new DoubleApproximatedPathTest(PathApproximator.CircularArcToPiecewiseLinear));
            Cell(2).AddRange(new[]
            {
                createLabel(nameof(PathApproximator.CircularArcToPiecewiseLinear)),
                doubleApproximatedPathTests[^1],
            });

            doubleApproximatedPathTests.Add(new DoubleApproximatedPathTest(PathApproximator.LagrangePolynomialToPiecewiseLinear));
            Cell(3).AddRange(new[]
            {
                createLabel(nameof(PathApproximator.LagrangePolynomialToPiecewiseLinear)),
                doubleApproximatedPathTests[^1],
            });

            AddSliderStep($"{nameof(numControlPoints)}", 3, 25, 5, v =>
            {
                numControlPoints = v;
                updateTests();
            });

            AddSliderStep($"{nameof(degree)}", 1, 5, 3, v =>
            {
                degree = v;
                updateTests();
            });

            AddSliderStep($"{nameof(numTestPoints)}", 10, 200, 100, v =>
            {
                numTestPoints = v;
                updateTests();
            });

            AddSliderStep($"{nameof(maxIterations)}", 0, 200, 10, v =>
            {
                maxIterations = v;
                updateTests();
            });

            AddSliderStep($"{nameof(learningRate)}", 0, 10, 8f, v =>
            {
                learningRate = v;
                updateTests();
            });

            AddSliderStep($"{nameof(b1)}", 0, 0.999f, 0.8f, v =>
            {
                b1 = v;
                updateTests();
            });

            AddSliderStep($"{nameof(b2)}", 0, 0.999f, 0.99f, v =>
            {
                b2 = v;
                updateTests();
            });

            AddStep("Enable optimization", () =>
            {
                foreach (var test in doubleApproximatedPathTests)
                    test.OptimizePath = true;
                updateTests();
            });
        }

        private void updateTests()
        {
            foreach (var test in doubleApproximatedPathTests)
            {
                test.NumControlPoints = numControlPoints;
                test.Degree = degree;
                test.NumTestPoints = numTestPoints;
                test.MaxIterations = maxIterations;
                test.LearningRate = learningRate;
                test.B1 = b1;
                test.B2 = b2;
                test.UpdatePath();
            }
        }

        private Drawable createLabel(string text) => new SpriteText
        {
            Text = text + "ToBSpline",
            Font = new FontUsage(size: 20),
            Colour = Color4.White,
        };

        public delegate List<Vector2> ApproximatorFunc(ReadOnlySpan<Vector2> controlPoints);

        private partial class DoubleApproximatedPathTest : Container
        {
            private readonly Vector2[] inputPath;

            public int NumControlPoints { get; set; }

            public int Degree { get; set; }

            public int NumTestPoints { get; set; }

            public int MaxIterations { get; set; }

            public float LearningRate { get; set; }

            public float B1 { get; set; }

            public float B2 { get; set; }

            public bool OptimizePath { get; set; }

            private readonly Path approximatedDrawnPath;
            private readonly Path controlPointPath;
            private readonly Container controlPointViz;

            public DoubleApproximatedPathTest(ApproximatorFunc approximator)
            {
                Vector2[] points = new Vector2[5];
                points[0] = new Vector2(50, 250);
                points[1] = new Vector2(150, 230);
                points[2] = new Vector2(100, 150);
                points[3] = new Vector2(200, 80);
                points[4] = new Vector2(250, 50);

                AutoSizeAxes = Axes.None;
                RelativeSizeAxes = Axes.Both;
                inputPath = approximator(points).ToArray();

                Children = new Drawable[]
                {
                    new Path
                    {
                        Colour = Color4.White,
                        PathRadius = 2,
                        Vertices = inputPath,
                    },
                    approximatedDrawnPath = new Path
                    {
                        Colour = Color4.Magenta,
                        PathRadius = 2,
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
                };
            }

            public void UpdatePath()
            {
                if (!OptimizePath) return;

                var controlPoints = PathApproximator.PiecewiseLinearToBSpline(inputPath, NumControlPoints, Degree, NumTestPoints, MaxIterations, LearningRate, B1, B2);
                approximatedDrawnPath.Vertices = PathApproximator.BSplineToPiecewiseLinear(controlPoints.ToArray(), Degree);
                controlPointPath.Vertices = controlPoints;
                controlPointViz.Clear();

                foreach (var cp in controlPoints)
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
    }
}
