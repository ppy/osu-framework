// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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
        private readonly Box[] boxes = new Box[5];

        public TestSceneSmoothedEdges()
            : base(2, 3)
        {
            Vector2[] smoothnesses =
            {
                new Vector2(0, 0),
                new Vector2(0, 2),
                new Vector2(2, 0),
                new Vector2(1, 1),
                new Vector2(2, 2),
            };

            for (int i = 0; i < boxes.Length; ++i)
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

            AddAssert("(0, 0) has no inflation", () => boxes[0].InflationAmount == Vector2.Zero);
            AddAssert("(1, 0) has inflation only in y", () => boxes[1].InflationAmount.X == 0 && boxes[1].InflationAmount.Y > 0);
            AddAssert("(2, 0) has inflation only in x", () => boxes[2].InflationAmount.X > 0 && boxes[2].InflationAmount.Y == 0);
            AddAssert("(0, 1) has inflation in x and y", () => boxes[3].InflationAmount.X > 0 && boxes[3].InflationAmount.Y > 0);
            AddAssert("(1, 1) has inflation in x and y", () => boxes[4].InflationAmount.X > 0 && boxes[4].InflationAmount.Y > 0);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            for (int i = 0; i < boxes.Length; ++i)
                boxes[i].Spin(10000, RotationDirection.Clockwise);
        }
    }
}
