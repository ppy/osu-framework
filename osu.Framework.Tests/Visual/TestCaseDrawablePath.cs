// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Lines;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.States;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseDrawablePath : GridTestCase
    {
        public TestCaseDrawablePath()
            : base(2, 2)
        {
            const int width = 20;
            Texture gradientTexture = new Texture(width, 1, true);
            var image = new Image<Rgba32>(width, 1);

            for (int i = 0; i < width; ++i)
            {
                var brightnessByte = (byte)((float)i / (width - 1) * 255);
                image[i, 0] = new Rgba32(brightnessByte, brightnessByte, brightnessByte);
            }

            gradientTexture.SetData(new TextureUpload(image));

            Cell(0).AddRange(new[]
            {
                createLabel("Simple path"),
                new Path
                {
                    RelativeSizeAxes = Axes.Both,
                    Positions = new List<Vector2> { Vector2.One * 50, Vector2.One * 100 },
                    Texture = gradientTexture,
                    Colour = Color4.Green,
                },
            });

            Cell(1).AddRange(new[]
            {
                createLabel("Curved path"),
                new Path
                {
                    RelativeSizeAxes = Axes.Both,
                    Positions = new List<Vector2>
                    {
                        new Vector2(50, 50),
                        new Vector2(50, 250),
                        new Vector2(250, 250),
                        new Vector2(250, 50),
                        new Vector2(50, 50),
                    },
                    Texture = gradientTexture,
                    Colour = Color4.Blue,
                },
            });

            Cell(2).AddRange(new[]
            {
                createLabel("Self-overlapping path"),
                new Path
                {
                    RelativeSizeAxes = Axes.Both,
                    Positions = new List<Vector2>
                    {
                        new Vector2(50, 50),
                        new Vector2(50, 250),
                        new Vector2(250, 250),
                        new Vector2(250, 150),
                        new Vector2(20, 150),
                    },
                    Texture = gradientTexture,
                    Colour = Color4.Red,
                },
            });

            Cell(3).AddRange(new[]
            {
                createLabel("Draw something ;)"),
                new UserDrawnPath
                {
                    RelativeSizeAxes = Axes.Both,
                    Texture = gradientTexture,
                    Colour = Color4.White,
                },
            });
        }

        private Drawable createLabel(string text) => new SpriteText
        {
            Text = text,
            TextSize = 20,
            Colour = Color4.White,
        };

        private class UserDrawnPath : Path
        {
            private Vector2 oldPos;

            protected override bool OnDragStart(InputState state)
            {
                AddVertex(state.Mouse.Position);
                oldPos = state.Mouse.Position;
                return true;
            }

            protected override bool OnDrag(InputState state)
            {
                Vector2 pos = state.Mouse.Position;
                if ((pos - oldPos).Length > 10)
                {
                    AddVertex(pos);
                    oldPos = pos;
                }

                return base.OnDrag(state);
            }
        }
    }
}
