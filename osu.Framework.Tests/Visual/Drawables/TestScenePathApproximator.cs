// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Lines;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Utils;
using osu.Framework.Testing;
using System.Numerics;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Drawables
{
    public partial class TestScenePathApproximator : GridTestScene
    {
        public TestScenePathApproximator()
            : base(2, 2)
        {
            Cell(0).AddRange(new[]
            {
                createLabel(nameof(PathApproximator.BezierToPiecewiseLinear)),
                new ApproximatedPathTest(PathApproximator.BezierToPiecewiseLinear),
            });

            Cell(1).AddRange(new[]
            {
                createLabel(nameof(PathApproximator.CatmullToPiecewiseLinear)),
                new ApproximatedPathTest(PathApproximator.CatmullToPiecewiseLinear),
            });

            Cell(2).AddRange(new[]
            {
                createLabel(nameof(PathApproximator.CircularArcToPiecewiseLinear)),
                new ApproximatedPathTest(PathApproximator.CircularArcToPiecewiseLinear),
            });

            Cell(3).AddRange(new[]
            {
                createLabel(nameof(PathApproximator.LagrangePolynomialToPiecewiseLinear)),
                new ApproximatedPathTest(PathApproximator.LagrangePolynomialToPiecewiseLinear),
            });
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
    }
}
