// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Testing;

namespace osu.Framework.VisualTests.Tests
{
    internal class TestCaseCircularProgress : TestCase
    {
        public override string Description => @"Circular progress bar";

        private CircularProgress clock1;
        private CircularProgress clock2;
        private CircularProgress clock3;
        private CircularProgress clock4;

        public override void Reset()
        {
            base.Reset();

            // A test texture to apply to the clocks
            const int width = 20;
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

            Children = new Drawable[]
            {
                new Container
                {
                    Depth = 3,

                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,

                    Width = 320,
                    Height = 320,
                    CornerRadius = 8,

                    Masking = true,

                    Children = new Drawable[]
                    {
                        new Box
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            RelativeSizeAxes = Axes.Both,
                            Colour = new Color4(100, 100, 100, 255),
                        },
                        clock1 = new CircularProgress
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,

                            Width = 300,
                            Height = 300,
                            Colour = new Color4(128, 255, 128, 255),
                        },
                    },
                },
                clock2 = new CircularProgress
                {
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    Position = new Vector2(20, 20),
                    Texture = gradientTexture,

                    Width = 100,
                    Height = 100,
                },
                clock3 = new CircularProgress
                {
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    Position = new Vector2(220, 20),
                    Texture = gradientTexture,
                    ColourInfo = ColourInfo.GradientVertical(new Color4(128, 255, 128, 255), new Color4(255, 128, 128, 255)),

                    Width = 100,
                    Height = 100,

                    Scale = new Vector2(-0.6f, 1),
                },
                clock4 = new CircularProgress
                {
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    Position = new Vector2(420, 20),
                    //Texture = gradientTexture,
                    ColourInfo = ColourInfo.GradientHorizontal(new Color4(128, 255, 128, 255), new Color4(255, 128, 128, 255)),

                    Width = 100,
                    Height = 100,

                    //Scale = new Vector2(-0.6f, 1),
                },
            };
        }

        protected override void Update()
        {
            base.Update();
            clock1.Current.Value = Time.Current % 500 / 500;
            clock2.Current.Value = Time.Current % 730 / 730;
            clock3.Current.Value = Time.Current % 800 / 800;
            clock4.Current.Value = Time.Current % 860 / 430 - 1;
        }
    }
}
