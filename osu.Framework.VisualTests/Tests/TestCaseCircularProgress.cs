// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;
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
        private CircularProgress clock5;

        public override void Reset()
        {
            base.Reset();

            const int width = 20;
            byte[] data = new byte[width * 4];

            Texture gradientTextureHorizontal = new Texture(width, 1, true);
            for (int i = 0; i < width; ++i)
            {
                float brightness = (float)i / (width - 1);
                int index = i * 4;
                data[index + 0] = (byte)(128 + (1 - brightness) * 127);
                data[index + 1] = (byte)(128 + brightness * 127);
                data[index + 2] = 128;
                data[index + 3] = 255;
            }
            gradientTextureHorizontal.SetData(new TextureUpload(data));

            Texture gradientTextureVertical = new Texture(1, width, true);
            for (int i = 0; i<width; ++i)
            {
                float brightness = (float)i / (width - 1);
                int index = i * 4;
                data[index + 0] = (byte)(128 + (1 - brightness) * 127);
                data[index + 1] = (byte)(128 + brightness * 127);
                data[index + 2] = 128;
                data[index + 3] = 255;
            }
            gradientTextureVertical.SetData(new TextureUpload(data));


            Children = new Drawable[]
            {
                clock1 = new CircularProgress
                {
                    Width = 100,
                    Height = 100,
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    Position = new Vector2(20, 20),

                    Colour = new Color4(128, 255, 128, 255),

                },
                clock2 = new CircularProgress
                {
                    Width = 100,
                    Height = 100,
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    Position = new Vector2(20, 140),

                    Texture = gradientTextureVertical,
                },
                clock3 = new CircularProgress
                {
                    Width = 100,
                    Height = 100,
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    Position = new Vector2(140, 140),

                    Texture = gradientTextureHorizontal,
                },
                clock4 = new CircularProgress
                {
                    Width = 100,
                    Height = 100,
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    Position = new Vector2(20, 260),

                    ColourInfo = ColourInfo.GradientVertical(new Color4(128, 255, 128, 255), new Color4(255, 128, 128, 255)),
                },
                clock5 = new CircularProgress
                {
                    Width = 100,
                    Height = 100,
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    Position = new Vector2(140, 260),

                    ColourInfo = ColourInfo.GradientHorizontal(new Color4(128, 255, 128, 255), new Color4(255, 128, 128, 255)),
                },
            };
        }

        protected override void Update()
        {
            base.Update();
            clock1.Current.Value = Time.Current % 500 / 500;
            clock2.Current.Value = Time.Current % 730 / 365 - 1;
            clock3.Current.Value = Time.Current % 800 / 400 - 1;
            clock4.Current.Value = Time.Current % 860 / 430 - 1;
            clock5.Current.Value = Time.Current % 1332 / 666 - 1;
        }
    }
}
