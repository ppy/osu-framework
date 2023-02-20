// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osuTK.Graphics;
using osu.Framework.Graphics.Lines;
using osuTK;

namespace osu.Framework.Tests.Visual.Drawables
{
    public partial class TestScenePathWithZeroLength : GridTestScene
    {
        public TestScenePathWithZeroLength()
            : base(2, 2)
        {
            Cell(0).AddRange(new[]
            {
                createLabel("0 length segment"),
                createPath(new[]
                {
                    new Vector2(150f),
                    new Vector2(150f),
                }),
            });

            Cell(1).AddRange(new[]
            {
                createLabel("Middle few tiny segments"),
                createPath(new[]
                {
                    new Vector2(50f, 100f),
                    new Vector2(100f),
                    new Vector2(100f + 1e-6f),
                    new Vector2(100f + 2e-6f),
                    new Vector2(100f + 3e-6f),
                    new Vector2(100f + 4e-6f),
                    new Vector2(200f + 1e-6f),
                    new Vector2(200f + 2e-6f),
                    new Vector2(200f + 3e-6f),
                    new Vector2(200f + 4e-6f),
                    new Vector2(250f, 200f),
                }),
            });

            Cell(2).AddRange(new[]
            {
                createLabel("First few tiny segments"),
                createPath(new[]
                {
                    new Vector2(100f),
                    new Vector2(100f + 1e-6f),
                    new Vector2(100f + 2e-6f),
                    new Vector2(100f + 3e-6f),
                    new Vector2(100f + 4e-6f),
                    new Vector2(200f),
                }),
            });

            Cell(3).AddRange(new[]
            {
                createLabel("Final few tiny segments"),
                createPath(new[]
                {
                    new Vector2(100f),
                    new Vector2(200f),
                    new Vector2(200f + 1e-6f),
                    new Vector2(200f + 2e-6f),
                    new Vector2(200f + 3e-6f),
                    new Vector2(200f + 4e-6f),
                }),
            });
        }

        private static Drawable createLabel(string text) => new SpriteText
        {
            Text = text,
            Font = new FontUsage(size: 20),
            Colour = Color4.White,
        };

        private static Path createPath(Vector2[] points) => new Path
        {
            PathRadius = 50,
            Vertices = points,
        };
    }
}
