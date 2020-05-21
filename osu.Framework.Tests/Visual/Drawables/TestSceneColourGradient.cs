// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osuTK;

namespace osu.Framework.Tests.Visual.Drawables
{
    public class TestSceneColourGradient : GridTestScene
    {
        public TestSceneColourGradient()
            : base(4, 2)
        {
            Colour4 transparentBlack = new Colour4(0, 0, 0, 0);

            ColourInfo[] colours =
            {
                new ColourInfo
                {
                    TopLeft = Colour4.Pink.ToLinear(),
                    BottomLeft = Colour4.Pink.ToLinear(),
                    TopRight = Colour4.SkyBlue.ToLinear(),
                    BottomRight = Colour4.SkyBlue.ToLinear(),
                },
                new ColourInfo
                {
                    TopLeft = Colour4.Pink,
                    BottomLeft = Colour4.Pink,
                    TopRight = Colour4.SkyBlue,
                    BottomRight = Colour4.SkyBlue,
                },
                new ColourInfo
                {
                    TopLeft = Colour4.White.ToLinear(),
                    BottomLeft = Colour4.White.ToLinear(),
                    TopRight = Colour4.Black.ToLinear(),
                    BottomRight = Colour4.Black.ToLinear(),
                },
                new ColourInfo
                {
                    TopLeft = Colour4.White,
                    BottomLeft = Colour4.White,
                    TopRight = Colour4.Black,
                    BottomRight = Colour4.Black,
                },
                new ColourInfo
                {
                    TopLeft = Colour4.White.ToLinear(),
                    BottomLeft = Colour4.White.ToLinear(),
                    TopRight = Colour4.Transparent.ToLinear(),
                    BottomRight = Colour4.Transparent.ToLinear(),
                },
                new ColourInfo
                {
                    TopLeft = Colour4.White,
                    BottomLeft = Colour4.White,
                    TopRight = Colour4.Transparent,
                    BottomRight = Colour4.Transparent,
                },
                new ColourInfo
                {
                    TopLeft = Colour4.White.ToLinear(),
                    BottomLeft = Colour4.White.ToLinear(),
                    TopRight = transparentBlack.ToLinear(),
                    BottomRight = transparentBlack.ToLinear(),
                },
                new ColourInfo
                {
                    TopLeft = Colour4.White,
                    BottomLeft = Colour4.White,
                    TopRight = transparentBlack,
                    BottomRight = transparentBlack,
                },
            };

            string[] labels =
            {
                "Colours (Linear)",
                "Colours (sRGB)",
                "White to black (Linear brightness gradient)",
                "White to black (sRGB brightness gradient)",
                "White to transparent white (Linear brightness gradient)",
                "White to transparent white (sRGB brightness gradient)",
                "White to transparent black (Linear brightness gradient)",
                "White to transparent black (sRGB brightness gradient)",
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
