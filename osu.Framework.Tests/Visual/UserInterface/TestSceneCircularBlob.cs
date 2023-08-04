// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.UserInterface;
using osuTK;
using osuTK.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public partial class TestSceneCircularBlob : FrameworkTestScene
    {
        [Resolved]
        private IRenderer renderer { get; set; } = null!;

        private CircularBlob blob = null!;

        private Texture gradientTextureHorizontal = null!;
        private Texture gradientTextureVertical = null!;
        private Texture gradientTextureBoth = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            const int width = 128;

            var image = new Image<Rgba32>(width, 1);

            gradientTextureHorizontal = renderer.CreateTexture(width, 1, true);

            for (int i = 0; i < width; ++i)
            {
                float brightness = (float)i / (width - 1);
                image[i, 0] = new Rgba32((byte)(128 + (1 - brightness) * 127), (byte)(128 + brightness * 127), 128, 255);
            }

            gradientTextureHorizontal.SetData(new TextureUpload(image));

            image = new Image<Rgba32>(width, 1);

            gradientTextureVertical = renderer.CreateTexture(1, width, true);

            for (int i = 0; i < width; ++i)
            {
                float brightness = (float)i / (width - 1);
                image[i, 0] = new Rgba32((byte)(128 + (1 - brightness) * 127), (byte)(128 + brightness * 127), 128, 255);
            }

            gradientTextureVertical.SetData(new TextureUpload(image));

            image = new Image<Rgba32>(width, width);

            gradientTextureBoth = renderer.CreateTexture(width, width, true);

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

            Box background;
            Container maskingContainer;

            Children = new Drawable[]
            {
                background = new Box
                {
                    Colour = FrameworkColour.GreenDark,
                    RelativeSizeAxes = Axes.Both,
                    Alpha = 0f,
                },
                maskingContainer = new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(250),
                    CornerRadius = 20,
                    Child = blob = new CircularBlob
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Size = new Vector2(400)
                    }
                }
            };

            AddStep("Horizontal Gradient Texture", delegate { setTexture(1); });
            AddStep("Vertical Gradient Texture", delegate { setTexture(2); });
            AddStep("2D Graident Texture", delegate { setTexture(3); });
            AddStep("White Texture", delegate { setTexture(0); });

            AddStep("Red Colour", delegate { setColour(1); });
            AddStep("Horzontal Gradient Colour", delegate { setColour(2); });
            AddStep("Vertical Gradient Colour", delegate { setColour(3); });
            AddStep("2D Gradient Colour", delegate { setColour(4); });
            AddStep("White Colour", delegate { setColour(0); });

            AddToggleStep("Toggle masking", m => maskingContainer.Masking = m);
            AddToggleStep("Toggle aspect ratio", r => blob.Size = r ? new Vector2(600, 400) : new Vector2(400));
            AddToggleStep("Toggle background", b => background.Alpha = b ? 1 : 0);
            AddSliderStep("Scale", 0f, 2f, 1f, s => blob.Scale = new Vector2(s));
            AddSliderStep("Fill", 0f, 1f, 0.5f, f => blob.InnerRadius = f);
            AddSliderStep("Amplitude", 0f, 1f, 0.3f, ns => blob.Amplitude = ns);
            AddSliderStep("Frequency", 0f, 5f, 1.5f, ns => blob.Frequency = ns);
            AddSliderStep("Seed", 0, 999999999, 0, s => blob.Seed = s);
        }

        private void setTexture(int textureMode)
        {
            switch (textureMode)
            {
                case 0:
                    blob.Texture = renderer.WhitePixel;
                    break;

                case 1:
                    blob.Texture = gradientTextureHorizontal;
                    break;

                case 2:
                    blob.Texture = gradientTextureVertical;
                    break;

                case 3:
                    blob.Texture = gradientTextureBoth;
                    break;
            }
        }

        private void setColour(int colourMode)
        {
            switch (colourMode)
            {
                case 0:
                    blob.Colour = new Color4(255, 255, 255, 255);
                    break;

                case 1:
                    blob.Colour = new Color4(255, 88, 88, 255);
                    break;

                case 2:
                    blob.Colour = new ColourInfo
                    {
                        TopLeft = new Color4(255, 128, 128, 255),
                        TopRight = new Color4(128, 255, 128, 255),
                        BottomLeft = new Color4(255, 128, 128, 255),
                        BottomRight = new Color4(128, 255, 128, 255),
                    };
                    break;

                case 3:
                    blob.Colour = new ColourInfo
                    {
                        TopLeft = new Color4(255, 128, 128, 255),
                        TopRight = new Color4(255, 128, 128, 255),
                        BottomLeft = new Color4(128, 255, 128, 255),
                        BottomRight = new Color4(128, 255, 128, 255),
                    };
                    break;

                case 4:
                    blob.Colour = new ColourInfo
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
