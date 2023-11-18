// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Lines;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Utils;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Drawables
{
    public partial class TestScenePiecewiseLinearToBezier : GridTestScene
    {
        private int numControlPoints;
        private int numTestPoints;
        private int maxIterations;

        private readonly List<DoubleApproximatedPathTest> doubleApproximatedPathTests = new List<DoubleApproximatedPathTest>();

        public TestScenePiecewiseLinearToBezier()
            : base(2, 2)
        {
            doubleApproximatedPathTests.Add(new DoubleApproximatedPathTest(PathApproximator.BezierToPiecewiseLinear, numControlPoints, numTestPoints, maxIterations));
            Cell(0).AddRange(new[]
            {
                createLabel(nameof(PathApproximator.BezierToPiecewiseLinear)),
                new ApproximatedPathTest(PathApproximator.BezierToPiecewiseLinear),
                doubleApproximatedPathTests[^1],
            });

            doubleApproximatedPathTests.Add(new DoubleApproximatedPathTest(PathApproximator.CatmullToPiecewiseLinear, numControlPoints, numTestPoints, maxIterations));
            Cell(1).AddRange(new[]
            {
                createLabel(nameof(PathApproximator.CatmullToPiecewiseLinear)),
                new ApproximatedPathTest(PathApproximator.CatmullToPiecewiseLinear),
                doubleApproximatedPathTests[^1],
            });

            doubleApproximatedPathTests.Add(new DoubleApproximatedPathTest(PathApproximator.CircularArcToPiecewiseLinear, numControlPoints, numTestPoints, maxIterations));
            Cell(2).AddRange(new[]
            {
                createLabel(nameof(PathApproximator.CircularArcToPiecewiseLinear)),
                new ApproximatedPathTest(PathApproximator.CircularArcToPiecewiseLinear),
                doubleApproximatedPathTests[^1],
            });

            doubleApproximatedPathTests.Add(new DoubleApproximatedPathTest(PathApproximator.LagrangePolynomialToPiecewiseLinear, numControlPoints, numTestPoints, maxIterations));
            Cell(3).AddRange(new[]
            {
                createLabel(nameof(PathApproximator.LagrangePolynomialToPiecewiseLinear)),
                new ApproximatedPathTest(PathApproximator.LagrangePolynomialToPiecewiseLinear),
                doubleApproximatedPathTests[^1],
            });

            AddSliderStep($"{nameof(numControlPoints)}", 3, 25, 5, v =>
            {
                numControlPoints = v;
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
                test.NumTestPoints = numTestPoints;
                test.MaxIterations = maxIterations;
                test.UpdatePath();
            }
        }

        private Drawable createLabel(string text) => new SpriteText
        {
            Text = text,
            Font = new FontUsage(size: 20),
            Colour = Color4.White,
        };

        public delegate List<Vector2> ApproximatorFunc(ReadOnlySpan<Vector2> controlPoints);

        private partial class ApproximatedPathTest : SmoothPath
        {
            public ApproximatedPathTest(ApproximatorFunc approximator)
            {
                Vector2[] points = new Vector2[5];
                points[0] = new Vector2(50, 250);
                points[1] = new Vector2(150, 230);
                points[2] = new Vector2(100, 150);
                points[3] = new Vector2(200, 80);
                points[4] = new Vector2(250, 50);

                AutoSizeAxes = Axes.None;
                RelativeSizeAxes = Axes.Both;
                PathRadius = 2;
                Vertices = approximator(points);
                Colour = Color4.White;
            }
        }

        private partial class DoubleApproximatedPathTest : SmoothPath
        {
            private readonly Vector2[] inputPath;

            public int NumControlPoints { get; set; }

            public int NumTestPoints { get; set; }

            public int MaxIterations { get; set; }

            public bool OptimizePath { get; set; }

            public DoubleApproximatedPathTest(ApproximatorFunc approximator, int numControlPoints, int numTestPoints, int maxIterations)
            {
                Vector2[] points = new Vector2[5];
                points[0] = new Vector2(50, 250);
                points[1] = new Vector2(150, 230);
                points[2] = new Vector2(100, 150);
                points[3] = new Vector2(200, 80);
                points[4] = new Vector2(250, 50);

                AutoSizeAxes = Axes.None;
                RelativeSizeAxes = Axes.Both;
                PathRadius = 2;
                Colour = Color4.Magenta;

                NumControlPoints = numControlPoints;
                NumTestPoints = numTestPoints;
                MaxIterations = maxIterations;
                inputPath = approximator(points).ToArray();
                UpdatePath();
            }

            public void UpdatePath()
            {
                if (!OptimizePath) return;

                var controlPoints = PathApproximator.PiecewiseLinearToBezier(inputPath, NumControlPoints, NumTestPoints, MaxIterations);
                Vertices = PathApproximator.BezierToPiecewiseLinear(controlPoints.ToArray());
            }
        }
    }
}
