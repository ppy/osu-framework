// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osuTK;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Tests.Visual.Sprites
{
    public partial class TestSceneColorize : FrameworkTestScene
    {
        private readonly List<ColorizeSprite> colorizeSprites = new List<ColorizeSprite>();

        [BackgroundDependencyLoader]
        private void load(IRenderer renderer, TextureStore textures)
        {
            Texture texture = textures.Get("Colouring/note") ?? createFixtureTexture(renderer);

            Child = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.Both,
                Direction = FillDirection.Full,
                Spacing = new Vector2(12),
                Padding = new MarginPadding(20),
                Children = new[]
                {
                    labelled("Original", new Sprite
                    {
                        Size = new Vector2(128),
                        Texture = texture,
                    }),
                    labelled("Multiply / All", createColorize(texture, ColorizeAlgorithm.Multiply, ColorizeRange.All)),
                    labelled("SetLum / All", createColorize(texture, ColorizeAlgorithm.SetLum, ColorizeRange.All)),
                    labelled("Multiply / Gray", createColorize(texture, ColorizeAlgorithm.Multiply, ColorizeRange.Gray)),
                    labelled("SetLum / Gray", createColorize(texture, ColorizeAlgorithm.SetLum, ColorizeRange.Gray)),
                    labelled("Multiply / KeepWhite", createColorize(texture, ColorizeAlgorithm.Multiply, ColorizeRange.KeepWhite)),
                    labelled("SetLum / KeepWhite", createColorize(texture, ColorizeAlgorithm.SetLum, ColorizeRange.KeepWhite)),
                    labelled("Multiply / White", createColorize(texture, ColorizeAlgorithm.Multiply, ColorizeRange.White)),
                    labelled("SetLum / White", createColorize(texture, ColorizeAlgorithm.SetLum, ColorizeRange.White)),
                }
            };

            AddSliderStep("Hue", 0, 360, 0, h => applyTint(Colour4.FromHSV(h / 360f, 1f, 1f)));
            AddSliderStep("White strength (8-bit)", 0, 255, 0, t => applyWhiteTolerance(t / 255f));
            applyTint(Colour4.Red);
        }

        [Test]
        public void ColorizeSpritesLoad()
        {
            AddUntilStep("sprites loaded", () => colorizeSprites.TrueForAll(s => s.IsLoaded));
        }

        private Drawable labelled(string text, Drawable content)
        {
            return new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(4),
                Children = new[]
                {
                    new SpriteText { Text = text },
                    content,
                }
            };
        }

        private ColorizeSprite createColorize(Texture texture, ColorizeAlgorithm algorithm, ColorizeRange range)
        {
            var sprite = new ColorizeSprite
            {
                Size = new Vector2(128),
                Texture = texture,
                Algorithm = algorithm,
                Range = range,
            };

            colorizeSprites.Add(sprite);
            return sprite;
        }

        private void applyTint(Colour4 tint)
        {
            foreach (var sprite in colorizeSprites)
                sprite.Tint = tint;
        }

        private void applyWhiteTolerance(float tolerance)
        {
            foreach (var sprite in colorizeSprites)
                sprite.WhiteTolerance = tolerance;
        }

        private static Texture createFixtureTexture(IRenderer renderer)
        {
            const int size = 128;
            var image = new Image<Rgba32>(size, size);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = x / (float)(size - 1);
                    float ny = y / (float)(size - 1);

                    if (nx < 0.12f || nx > 0.88f || ny < 0.12f || ny > 0.88f)
                    {
                        byte glow = (byte)(40 * (1f - Math.Min(nx, Math.Min(1f - nx, Math.Min(ny, 1f - ny))) / 0.12f));
                        image[x, y] = new Rgba32(glow, glow, glow, glow);
                        continue;
                    }

                    if (ny < 0.28f)
                    {
                        image[x, y] = new Rgba32(255, 255, 255, 255);
                        continue;
                    }

                    if (nx > 0.37f && nx < 0.63f && ny > 0.55f && ny < 0.82f)
                    {
                        image[x, y] = new Rgba32(25, 51, 230, 255);
                        continue;
                    }

                    if (nx < 0.5f)
                    {
                        byte grey = (byte)(38 + (1f - ny) * 140);
                        image[x, y] = new Rgba32(grey, grey, grey, 255);
                        continue;
                    }

                    float t = Math.Clamp((ny - 0.28f) / 0.6f, 0f, 1f);
                    image[x, y] = new Rgba32(
                        255,
                        (byte)(255 + (40 - 255) * t),
                        (byte)(255 + (90 - 255) * t),
                        255);
                }
            }

            var texture = renderer.CreateTexture(size, size, true);
            texture.SetData(new TextureUpload(image));
            return texture;
        }
    }
}
