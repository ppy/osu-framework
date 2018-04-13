// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseCircularProgress : TestCase
    {
        public override IReadOnlyList<Type> RequiredTypes => new[] { typeof(CircularProgress), typeof(CircularProgressDrawNode), typeof(CircularProgressDrawNodeSharedData) };

        private readonly CircularProgress clock;

        private int rotateMode;
        private const double period = 4000;
        private const double transition_period = 2000;

        private readonly Texture gradientTextureHorizontal;
        private readonly Texture gradientTextureVertical;
        private readonly Texture gradientTextureBoth;

        public TestCaseCircularProgress()
        {
            const int width = 128;
            byte[] data = new byte[width * 4];

            gradientTextureHorizontal = new Texture(width, 1, true);
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

            gradientTextureVertical = new Texture(1, width, true);
            for (int i = 0; i < width; ++i)
            {
                float brightness = (float)i / (width - 1);
                int index = i * 4;
                data[index + 0] = (byte)(128 + (1 - brightness) * 127);
                data[index + 1] = (byte)(128 + brightness * 127);
                data[index + 2] = 128;
                data[index + 3] = 255;
            }
            gradientTextureVertical.SetData(new TextureUpload(data));

            byte[] data2 = new byte[width * width * 4];
            gradientTextureBoth = new Texture(width, width, true);
            for (int i = 0; i < width; ++i)
            {
                for (int j = 0; j < width; ++j)
                {
                    float brightness = (float)i / (width - 1);
                    float brightness2 = (float)j / (width - 1);
                    int index = i * 4 * width + j * 4;
                    data2[index + 0] = (byte)(128 + (1 + brightness - brightness2) / 2 * 127);
                    data2[index + 1] = (byte)(128 + (1 + brightness2 - brightness) / 2 * 127);
                    data2[index + 2] = (byte)(128 + (brightness + brightness2) / 2 * 127);
                    data2[index + 3] = 255;
                }
            }
            gradientTextureBoth.SetData(new TextureUpload(data2));


            Children = new Drawable[]
            {
                clock = new CircularProgress
                {
                    Width = 0.8f,
                    Height = 0.8f,
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                },
            };

            AddStep("Forward", delegate { rotateMode = 1; });
            AddStep("Backward", delegate { rotateMode = 2; });
            AddStep("Transition Focus", delegate { rotateMode = 3; });
            AddStep("Transition Focus 2", delegate { rotateMode = 4; });
            AddStep("Forward/Backward", delegate { rotateMode = 0; });

            AddStep("Horizontal Gradient Texture", delegate { setTexture(1); });
            AddStep("Vertical Gradient Texture", delegate { setTexture(2); });
            AddStep("2D Graident Texture", delegate { setTexture(3); });
            AddStep("White Texture", delegate { setTexture(0); });

            AddStep("Red Colour", delegate { setColour(1); });
            AddStep("Horzontal Gradient Colour", delegate { setColour(2); });
            AddStep("Vertical Gradient Colour", delegate { setColour(3); });
            AddStep("2D Gradient Colour", delegate { setColour(4); });
            AddStep("White Colour", delegate { setColour(0); });

            AddSliderStep("Fill", 0, 10, 10, fill => clock.InnerRadius = fill / 10f);
        }

        protected override void Update()
        {
            base.Update();
            switch (rotateMode)
            {
                case 0:
                    clock.Current.Value = Time.Current % (period * 2) / period - 1;
                    break;
                case 1:
                    clock.Current.Value = Time.Current % period / period;
                    break;
                case 2:
                    clock.Current.Value = Time.Current % period / period - 1;
                    break;
                case 3:
                    clock.Current.Value = Time.Current % transition_period / transition_period / 5 - 0.1f;
                    break;
                case 4:
                    clock.Current.Value = (Time.Current % transition_period / transition_period / 5 - 0.1f + 2) % 2 - 1;
                    break;
            }
        }

        private void setTexture(int textureMode)
        {
            switch (textureMode)
            {
                case 0:
                    clock.Texture = Texture.WhitePixel;
                    break;
                case 1:
                    clock.Texture = gradientTextureHorizontal;
                    break;
                case 2:
                    clock.Texture = gradientTextureVertical;
                    break;
                case 3:
                    clock.Texture = gradientTextureBoth;
                    break;
            }
        }

        private void setColour(int colourMode)
        {
            switch (colourMode)
            {
                case 0:
                    clock.Colour = new Color4(255, 255, 255, 255);
                    break;
                case 1:
                    clock.Colour = new Color4(255, 128, 128, 255);
                    break;
                case 2:
                    clock.Colour = new ColourInfo
                    {
                        TopLeft = new Color4(255, 128, 128, 255),
                        TopRight = new Color4(128, 255, 128, 255),
                        BottomLeft = new Color4(255, 128, 128, 255),
                        BottomRight = new Color4(128, 255, 128, 255),
                    };
                    break;
                case 3:
                    clock.Colour = new ColourInfo
                    {
                        TopLeft = new Color4(255, 128, 128, 255),
                        TopRight = new Color4(255, 128, 128, 255),
                        BottomLeft = new Color4(128, 255, 128, 255),
                        BottomRight = new Color4(128, 255, 128, 255),
                    };
                    break;
                case 4:
                    clock.Colour = new ColourInfo
                    {
                        TopLeft = new Color4(255, 128, 128, 255),
                        TopRight = new Color4(128, 255, 128, 255),
                        BottomLeft = new Color4(128, 128, 255, 255),
                        BottomRight = new Color4(255, 255, 255, 255),
                    };
                    break;
            }
        }
    }
}
