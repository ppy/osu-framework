// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Lines;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Tests.Visual.Input
{
    [System.ComponentModel.Description("live path optimiastion")]
    public class TestSceneInputResampler : GridTestScene
    {
        public TestSceneInputResampler()
            : base(3, 3)
        {
        }

        [BackgroundDependencyLoader]
        private void load(IRenderer renderer)
        {
            const int width = 2;
            Texture gradientTexture = renderer.CreateTexture(width, 1, true);
            var image = new Image<Rgba32>(width, 1);

            for (int i = 0; i < width; ++i)
            {
                byte brightnessByte = (byte)((float)i / (width - 1) * 255);
                image[i, 0] = new Rgba32(brightnessByte, brightnessByte, brightnessByte);
            }

            gradientTexture.SetData(new TextureUpload(image));

            SpriteText[] text = new SpriteText[6];

            Cell(0, 0).AddRange(new Drawable[]
            {
                text[0] = createLabel("Raw"),
                new ArcPath(true, true, new InputResampler(), gradientTexture, Color4.Green, text[0]),
            });

            Cell(0, 1).AddRange(new Drawable[]
            {
                text[1] = createLabel("Rounded (resembles mouse input)"),
                new ArcPath(true, false, new InputResampler(), gradientTexture, Color4.Blue, text[1]),
            });

            Cell(0, 2).AddRange(new Drawable[]
            {
                text[2] = createLabel("Custom: Smoothed=0, Raw=0"),
                new UserDrawnPath
                {
                    DrawText = text[2],
                    Texture = gradientTexture,
                    Colour = Color4.White,
                },
            });

            Cell(1, 0).AddRange(new Drawable[]
            {
                text[3] = createLabel("Smoothed raw"),
                new ArcPath(false, true, new InputResampler(), gradientTexture, Color4.Green, text[3]),
            });

            Cell(1, 1).AddRange(new Drawable[]
            {
                text[4] = createLabel("Smoothed rounded"),
                new ArcPath(false, false, new InputResampler(), gradientTexture, Color4.Blue, text[4]),
            });

            Cell(1, 2).AddRange(new Drawable[]
            {
                text[5] = createLabel("Smoothed custom: Smoothed=0, Raw=0"),
                new SmoothedUserDrawnPath
                {
                    DrawText = text[5],
                    Texture = gradientTexture,
                    Colour = Color4.White,
                    InputResampler = new InputResampler(),
                },
            });

            Cell(2, 0).AddRange(new Drawable[]
            {
                text[3] = createLabel("Force-smoothed raw"),
                new ArcPath(false, true, new InputResampler { ResampleRawInput = true }, gradientTexture, Color4.Green, text[3]),
            });

            Cell(2, 1).AddRange(new Drawable[]
            {
                text[4] = createLabel("Force-smoothed rounded"),
                new ArcPath(false, false, new InputResampler { ResampleRawInput = true }, gradientTexture, Color4.Blue, text[4]),
            });

            Cell(2, 2).AddRange(new Drawable[]
            {
                text[5] = createLabel("Force-smoothed custom: Smoothed=0, Raw=0"),
                new SmoothedUserDrawnPath
                {
                    DrawText = text[5],
                    Texture = gradientTexture,
                    Colour = Color4.White,
                    InputResampler = new InputResampler
                    {
                        ResampleRawInput = true
                    },
                },
            });
        }

        private SpriteText createLabel(string text) => new SpriteText
        {
            Text = text,
            Font = new FontUsage(size: 14),
            Colour = Color4.White,
        };

        private class SmoothedPath : TexturedPath
        {
            protected SmoothedPath()
            {
                PathRadius = 2;
            }

            public InputResampler InputResampler { get; set; } = new InputResampler();

            protected int NumVertices { get; set; }

            protected int NumRaw { get; set; }

            protected void AddRawVertex(Vector2 pos)
            {
                NumRaw++;
                AddVertex(pos);
                NumVertices++;
            }

            protected bool AddSmoothedVertex(Vector2 pos)
            {
                NumRaw++;
                bool foundOne = false;

                foreach (Vector2 relevant in InputResampler.AddPosition(pos))
                {
                    AddVertex(relevant);
                    NumVertices++;
                    foundOne = true;
                }

                return foundOne;
            }
        }

        private class ArcPath : SmoothedPath
        {
            public ArcPath(bool raw, bool keepFraction, InputResampler inputResampler, Texture texture, Color4 colour, SpriteText output)
            {
                InputResampler = inputResampler;

                AutoSizeAxes = Axes.None;
                RelativeSizeAxes = Axes.Both;
                Texture = texture;
                Colour = colour;

                const float target_raw = 1024;

                for (float i = 0; i < target_raw; i++)
                {
                    float x = (MathF.Sin(i / target_raw * (MathF.PI * 0.5f)) * 200) + 50.5f;
                    float y = (MathF.Cos(i / target_raw * (MathF.PI * 0.5f)) * 200) + 50.5f;
                    Vector2 v = keepFraction ? new Vector2(x, y) : new Vector2((int)x, (int)y);
                    if (raw)
                        AddRawVertex(v);
                    else
                        AddSmoothedVertex(v);
                }

                output.Text += ": Smoothed=" + NumVertices + ", Raw=" + NumRaw;
            }
        }

        private class UserDrawnPath : SmoothedPath
        {
            public SpriteText DrawText;

            public UserDrawnPath()
            {
                AutoSizeAxes = Axes.None;
                RelativeSizeAxes = Axes.Both;
            }

            protected virtual void AddUserVertex(Vector2 v) => AddRawVertex(v);

            protected override bool OnDragStart(DragStartEvent e)
            {
                AddUserVertex(e.MousePosition);
                DrawText.Text = "Custom Smoothed Drawn: Smoothed=" + NumVertices + ", Raw=" + NumRaw;
                return true;
            }

            protected override void OnDrag(DragEvent e)
            {
                AddUserVertex(e.MousePosition);
                DrawText.Text = "Custom Smoothed Drawn: Smoothed=" + NumVertices + ", Raw=" + NumRaw;
                base.OnDrag(e);
            }
        }

        private class SmoothedUserDrawnPath : UserDrawnPath
        {
            protected override void AddUserVertex(Vector2 v) => AddSmoothedVertex(v);
        }
    }
}
