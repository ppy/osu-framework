﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Desktop.Tests.Visual
{
    [TestFixture]
    internal class TestCaseSmoothedEdges : GridTestCase
    {
        public TestCaseSmoothedEdges() : base(2, 2)
        {
            Vector2[] smoothnesses =
            {
                new Vector2(0, 0),
                new Vector2(0, 2),
                new Vector2(1, 1),
                new Vector2(2, 2),
            };

            for (int i = 0; i < Rows * Cols; ++i)
            {
                Cell(i).AddRange(new Drawable[]
                {
                    new SpriteText
                    {
                        Text = $"{nameof(Sprite.EdgeSmoothness)}={smoothnesses[i]}",
                        TextSize = 20,
                    },
                    boxes[i] = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.White,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Size = new Vector2(0.5f),
                        EdgeSmoothness = smoothnesses[i],
                    },
                });
            }
        }

        public override string Description => @"Boxes with automatically smoothed edges (no anti-aliasing).";

        private readonly Box[] boxes = new Box[4];

        protected override void Update()
        {
            base.Update();

            foreach (Box box in boxes)
                box.Rotation += 0.01f;
        }
    }
}
