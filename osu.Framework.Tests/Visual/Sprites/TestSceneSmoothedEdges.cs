﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Sprites
{
    public class TestSceneSmoothedEdges : GridTestScene
    {
        public TestSceneSmoothedEdges()
            : base(2, 2)
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
                        Font = new FontUsage(size: 20),
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

        protected override void LoadComplete()
        {
            base.LoadComplete();

            for (int i = 0; i < Rows * Cols; ++i)
                boxes[i].Spin(10000, RotationDirection.Clockwise);
        }

        private readonly Box[] boxes = new Box[4];
    }
}
