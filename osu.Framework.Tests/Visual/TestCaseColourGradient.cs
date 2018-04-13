// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseColourGradient : GridTestCase
    {
        public TestCaseColourGradient() : base(2, 2)
        {
            Color4 transparentBlack = new Color4(0, 0, 0, 0);

            ColourInfo[] colours =
            {
                new ColourInfo
                {
                    TopLeft = Color4.White,
                    BottomLeft = Color4.Blue,
                    TopRight = Color4.Red,
                    BottomRight = Color4.Green,
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
                "White to black (linear brightness gradient)",
                "White to transparent white (sRGB brightness gradient)",
                "White to transparent black (mixed brightness gradient)",
            };

            for (int i = 0; i < Rows * Cols; ++i)
            {
                Cell(i).AddRange(new Drawable[]
                {
                    new SpriteText
                    {
                        Text = labels[i],
                        TextSize = 20,
                        Colour = colours[0],
                    },
                    boxes[i] = new Box
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

        private readonly Box[] boxes = new Box[4];

        protected override void Update()
        {
            base.Update();

            foreach (Box box in boxes)
                box.Rotation += 0.01f;
        }
    }
}
