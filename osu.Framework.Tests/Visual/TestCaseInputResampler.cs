// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Lines;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    [System.ComponentModel.Description("live path optimiastion")]
    public class TestCaseInputResampler : GridTestCase
    {
        public TestCaseInputResampler() : base(3, 3)
        {
            const int width = 2;
            Texture gradientTexture = new Texture(width, 1, true);
            byte[] data = new byte[width * 4];
            for (int i = 0; i < width; ++i)
            {
                float brightness = (float)i / (width - 1);
                int index = i * 4;
                data[index + 0] = (byte)(brightness * 255);
                data[index + 1] = (byte)(brightness * 255);
                data[index + 2] = (byte)(brightness * 255);
                data[index + 3] = 255;
            }
            gradientTexture.SetData(new TextureUpload(data));

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
                    RelativeSizeAxes = Axes.Both,
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
                    RelativeSizeAxes = Axes.Both,
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
                    RelativeSizeAxes = Axes.Both,
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
            TextSize = 14,
            Colour = Color4.White,
        };

        private class SmoothedPath : Path
        {
            protected SmoothedPath()
            {
                PathWidth = 2;
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
                const int target_raw = 1024;
                RelativeSizeAxes = Axes.Both;
                Texture = texture;
                Colour = colour;

                for (int i = 0; i < target_raw; i++)
                {
                    float x = (float)(Math.Sin(i / (double)target_raw * (Math.PI * 0.5)) * 200) + 50.5f;
                    float y = (float)(Math.Cos(i / (double)target_raw * (Math.PI * 0.5)) * 200) + 50.5f;
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

            protected virtual void AddUserVertex(Vector2 v) => AddRawVertex(v);

            protected override bool OnDragStart(InputState state)
            {
                AddUserVertex(state.Mouse.Position);
                DrawText.Text = "Custom Smoothed Drawn: Smoothed=" + NumVertices + ", Raw=" + NumRaw;
                return true;
            }

            protected override bool OnDrag(InputState state)
            {
                AddUserVertex(state.Mouse.Position);
                DrawText.Text = "Custom Smoothed Drawn: Smoothed=" + NumVertices + ", Raw=" + NumRaw;
                return base.OnDrag(state);
            }
        }

        private class SmoothedUserDrawnPath : UserDrawnPath
        {
            protected override void AddUserVertex(Vector2 v) => AddSmoothedVertex(v);
        }
    }
}
