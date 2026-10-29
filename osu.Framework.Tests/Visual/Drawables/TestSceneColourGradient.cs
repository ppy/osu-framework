// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Drawables
{
    public partial class TestSceneColourGradient : GridTestScene
    {
        public TestSceneColourGradient()
            : base(4, 1)
        {
            Color4 transparentBlack = new Color4(0, 0, 0, 0);

            ColourInfo[] colours =
            {
                new ColourInfo
                {
                    TopLeft = Color4.Pink,
                    BottomLeft = Color4.Pink,
                    TopRight = Color4.SkyBlue,
                    BottomRight = Color4.SkyBlue,
                },
                new ColourInfo
                {
                    TopLeft = Color4.White,
                    BottomLeft = Color4.White,
                    TopRight = Color4.Black,
                    BottomRight = Color4.Black,
                },
                new ColourInfo
                {
                    TopLeft = Color4.White,
                    BottomLeft = Color4.White,
                    TopRight = Color4.Transparent,
                    BottomRight = Color4.Transparent,
                },
                new ColourInfo
                {
                    TopLeft = Color4.White,
                    BottomLeft = Color4.White,
                    TopRight = transparentBlack,
                    BottomRight = transparentBlack,
                },
            };

            string[] labels =
            {
                "Colours",
                "White to black",
                "White to transparent white",
                "White to transparent black",
            };

            for (int i = 0; i < Rows * Cols; ++i)
            {
                Cell(i).AddRange(new Drawable[]
                {
                    new SpriteText
                    {
                        Text = labels[i],
                        Font = new FontUsage(size: 20),
                    },
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Size = new Vector2(0.5f),
                        Colour = colours[i],
                    },
                });
            }
        }
    }
}
