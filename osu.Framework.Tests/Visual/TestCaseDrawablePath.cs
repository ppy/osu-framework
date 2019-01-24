// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Lines;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.Events;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseDrawablePath : GridTestCase
    {
        public TestCaseDrawablePath()
            : base(3, 2)
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
                new TexturedPath
                {
                    RelativeSizeAxes = Axes.Both,
                    Vertices = new List<Vector2> { Vector2.One * 50, Vector2.One * 100 },
                    Texture = gradientTexture,
                    Colour = Color4.Green,
                },
            });

            Cell(1).AddRange(new[]
            {
                createLabel("Curved path"),
                new TexturedPath
                {
                    RelativeSizeAxes = Axes.Both,
                    Vertices = new List<Vector2>
                    {
                        new Vector2(50, 50),
                        new Vector2(50, 150),
                        new Vector2(150, 150),
                        new Vector2(150, 50),
                        new Vector2(50, 50),
                    },
                    Texture = gradientTexture,
                    Colour = Color4.Blue,
                },
            });

            Cell(2).AddRange(new[]
            {
                createLabel("Self-overlapping path"),
                new TexturedPath
                {
                    RelativeSizeAxes = Axes.Both,
                    Vertices = new List<Vector2>
                    {
                        new Vector2(50, 50),
                        new Vector2(50, 150),
                        new Vector2(150, 150),
                        new Vector2(150, 100),
                        new Vector2(20, 100),
                    },
                    Texture = gradientTexture,
                    Colour = Color4.Red,
                },
            });

            Cell(3).AddRange(new[]
            {
                createLabel("Smoothed path"),
                new SmoothPath
                {
                    RelativeSizeAxes = Axes.Both,
                    PathWidth = 5,
                    Vertices = new List<Vector2>
                    {
                        new Vector2(50, 50),
                        new Vector2(125, 100),
                    },
                    Colour = Color4.White,
                }
            });

            Cell(4).AddRange(new[]
            {
                createLabel("un-smoothed path"),
                new Path
                {
                    RelativeSizeAxes = Axes.Both,
                    PathWidth = 5,
                    Vertices = new List<Vector2>
                    {
                        new Vector2(50, 50),
                        new Vector2(125, 100),
                    },
                    Colour = Color4.White,
                }
            });

            Cell(5).AddRange(new[]
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

        private class UserDrawnPath : TexturedPath
        {
            private Vector2 oldPos;

            protected override bool OnDragStart(DragStartEvent e)
            {
                AddVertex(e.MousePosition);
                oldPos = e.MousePosition;
                return true;
            }

            protected override bool OnDrag(DragEvent e)
            {
                Vector2 pos = e.MousePosition;
                if ((pos - oldPos).Length > 10)
                {
                    AddVertex(pos);
                    oldPos = pos;
                }

                return base.OnDrag(e);
            }
        }
    }
}
