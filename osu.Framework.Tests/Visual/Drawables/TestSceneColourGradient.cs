// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Drawables
{
    public class TestSceneColourGradient : GridTestScene
    {
        public TestSceneColourGradient()
            : base(4, 2)
        {
            Color4 transparentBlack = new Color4(0, 0, 0, 0);

            ColourInfo[] colours =
            {
                new ColourInfo
                {
                    TopLeft = Color4.Pink.ToLinear(),
                    BottomLeft = Color4.Pink.ToLinear(),
                    TopRight = Color4.SkyBlue.ToLinear(),
                    BottomRight = Color4.SkyBlue.ToLinear(),
                },
                new ColourInfo
                {
                    TopLeft = Color4.Pink,
                    BottomLeft = Color4.Pink,
                    TopRight = Color4.SkyBlue,
                    BottomRight = Color4.SkyBlue,
                },
                new ColourInfo
                {
                    TopLeft = Color4.White.ToLinear(),
                    BottomLeft = Color4.White.ToLinear(),
                    TopRight = Color4.Black.ToLinear(),
                    BottomRight = Color4.Black.ToLinear(),
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
                    TopLeft = Color4.White.ToLinear(),
                    BottomLeft = Color4.White.ToLinear(),
                    TopRight = Color4.Transparent.ToLinear(),
                    BottomRight = Color4.Transparent.ToLinear(),
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
                    TopLeft = Color4.White.ToLinear(),
                    BottomLeft = Color4.White.ToLinear(),
                    TopRight = transparentBlack.ToLinear(),
                    BottomRight = transparentBlack.ToLinear(),
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
