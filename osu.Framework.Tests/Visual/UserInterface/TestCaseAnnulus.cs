// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestCaseAnnulus : TestCase
    {
        public override IReadOnlyList<Type> RequiredTypes => new[] { typeof(Annulus), typeof(AnnularDrawNode) };

        private readonly Annulus annulus;

        private const double period = 4000;
        private const double transition_period = 2000;

        private readonly Texture gradientTextureHorizontal;
        private readonly Texture gradientTextureVertical;
        private readonly Texture gradientTextureBoth;

        public TestCaseAnnulus()
        {
            const int width = 128;

            var image = new Image<Rgba32>(width, 1);

            gradientTextureHorizontal = new Texture(width, 1, true);
            for (int i = 0; i < width; ++i)
            {
                float brightness = (float)i / (width - 1);
                image[i, 0] = new Rgba32((byte)(128 + (1 - brightness) * 127), (byte)(128 + brightness * 127), 128, 255);
            }

            gradientTextureHorizontal.SetData(new TextureUpload(image));

            image = new Image<Rgba32>(width, 1);

            gradientTextureVertical = new Texture(1, width, true);
            for (int i = 0; i < width; ++i)
            {
                float brightness = (float)i / (width - 1);
                image[i, 0] = new Rgba32((byte)(128 + (1 - brightness) * 127), (byte)(128 + brightness * 127), 128, 255);
            }

            gradientTextureVertical.SetData(new TextureUpload(image));

            image = new Image<Rgba32>(width, width);

            gradientTextureBoth = new Texture(width, width, true);
            for (int i = 0; i < width; ++i)
            {
                for (int j = 0; j < width; ++j)
                {
                    float brightness = (float)i / (width - 1);
                    float brightness2 = (float)j / (width - 1);
                    image[i, j] = new Rgba32(
                        (byte)(128 + (1 + brightness - brightness2) / 2 * 127),
                        (byte)(128 + (1 + brightness2 - brightness) / 2 * 127),
                        (byte)(128 + (brightness + brightness2) / 2 * 127),
                        255);
                }
            }

            gradientTextureBoth.SetData(new TextureUpload(image));

            Children = new Drawable[]
            {
                annulus = new Annulus
                {
                    Width = 0.8f,
                    Height = 0.8f,
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                },
            };

            AddStep("Horizontal Gradient Texture", () => setTexture(1));
            AddStep("Vertical Gradient Texture", () => setTexture(2));
            AddStep("2D Gradient Texture", () => setTexture(3));
            AddStep("White Texture", () => setTexture(0));

            AddStep("Red Colour", () => setColour(1));
            AddStep("Horzontal Gradient Colour", () => setColour(2));
            AddStep("Vertical Gradient Colour", () => setColour(3));
            AddStep("2D Gradient Colour", () => setColour(4));
            AddStep("White Colour", () => setColour(0));

            AddSliderStep("Start angle", 0d, 2d, 0d, angle => annulus.StartAngle.Value = MathHelper.TwoPi * angle);
            AddSliderStep("End angle", 0d, 2d, 1d, angle => annulus.EndAngle.Value = MathHelper.TwoPi * angle);
            AddSliderStep("Fill", 0, 10, 10, fill => annulus.InnerRadius = fill / 10f);
        }

        private void setTexture(int textureMode)
        {
            switch (textureMode)
            {
                case 0:
                    annulus.Texture = Texture.WhitePixel;
                    break;
                case 1:
                    annulus.Texture = gradientTextureHorizontal;
                    break;
                case 2:
                    annulus.Texture = gradientTextureVertical;
                    break;
                case 3:
                    annulus.Texture = gradientTextureBoth;
                    break;
            }
        }

        private void setColour(int colourMode)
        {
            switch (colourMode)
            {
                case 0:
                    annulus.Colour = new Color4(255, 255, 255, 255);
                    break;
                case 1:
                    annulus.Colour = new Color4(255, 128, 128, 255);
                    break;
                case 2:
                    annulus.Colour = new ColourInfo
                    {
                        TopLeft = new Color4(255, 128, 128, 255),
                        TopRight = new Color4(128, 255, 128, 255),
                        BottomLeft = new Color4(255, 128, 128, 255),
                        BottomRight = new Color4(128, 255, 128, 255),
                    };
                    break;
                case 3:
                    annulus.Colour = new ColourInfo
                    {
                        TopLeft = new Color4(255, 128, 128, 255),
                        TopRight = new Color4(255, 128, 128, 255),
                        BottomLeft = new Color4(128, 255, 128, 255),
                        BottomRight = new Color4(128, 255, 128, 255),
                    };
                    break;
                case 4:
                    annulus.Colour = new ColourInfo
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
