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
        private CircularProgress clock5;
        private CircularProgress clock6;
        private CircularProgress clock7;

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

                    Texture = gradientTexture,
                },
                clock3 = new CircularProgress
                {
                    Width = 100,
                    Height = 100,
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    Position = new Vector2(140, 140),

                    Texture = gradientTexture,
                    ColourInfo = ColourInfo.GradientVertical(new Color4(128, 255, 128, 255), new Color4(255, 128, 128, 255)),
                },
                clock4 = new CircularProgress
                {
                    Width = 100,
                    Height = 100,
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    Position = new Vector2(260, 140),

                    //Texture = gradientTexture,
                    ColourInfo = ColourInfo.GradientHorizontal(new Color4(128, 255, 128, 255), new Color4(255, 128, 128, 255)),
                },
                clock5 = new CircularProgress
                {
                    Width = 100,
                    Height = 100,
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    Position = new Vector2(20, 260),

                    ColourInfo = ColourInfo.GradientHorizontal(new Color4(128, 255, 128, 255), new Color4(255, 128, 128, 255)),
                    UsePolarColourGradient = true,
                },
                clock6 = new CircularProgress
                {
                    Width = 100,
                    Height = 100,
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    Position = new Vector2(140, 260),

                    ColourInfo = ColourInfo.GradientVertical(new Color4(128, 255, 128, 255), new Color4(255, 128, 128, 255)),
                    UsePolarColourGradient = true,
                },
                clock7 = new CircularProgress
                {
                    Width = 100,
                    Height = 100,
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    Position = new Vector2(260, 260),

                    ColourInfo = new ColourInfo
                    {
                        TopLeft = new Color4(255, 255, 255, 255),
                        TopRight = new Color4(255, 128, 128, 255),
                        BottomLeft = new Color4(128, 255, 128, 255),
                        BottomRight = new Color4(128, 128, 255, 255),
                    },
                    UsePolarColourGradient = true,
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
            clock5.Current.Value = Time.Current % 3000 / 1500 - 1;
            clock6.Current.Value = Time.Current % 5000 / 2500 - 1;
            clock7.Current.Value = Time.Current % 6666 / 3333 - 1;
        }
    }
}
