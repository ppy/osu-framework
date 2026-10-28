// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using osuTK;
using osuTK.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Tests.Visual.Graphics
{
    public partial class TestSceneTexturePremultiplication : FrameworkTestScene
    {
        private TextureStore textures = null!;

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            textures = new TextureStore(host.Renderer, host.CreateTextureLoaderStore(new CustomResourceStore()), false, TextureFilteringMode.Nearest);
        }

        [Test]
        public void TestComparison()
        {
            AddStep("setup", () =>
            {
                Child = new FillFlowContainer
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    AutoSizeAxes = Axes.Both,
                    Spacing = new Vector2(0f, 5f),
                    Direction = FillDirection.Vertical,
                    Children = new Drawable[]
                    {
                        new Container
                        {
                            AutoSizeAxes = Axes.Both,
                            Children = new[]
                            {
                                new Box
                                {
                                    Size = new Vector2(256, 128),
                                    Colour = Color4.Blue,
                                },
                                new Sprite
                                {
                                    Texture = textures.Get("zero-to-red"),
                                    Size = new Vector2(256, 128),
                                }
                            },
                        },
                        new SpriteText
                        {
                            Text = "Rendering of the sprite above should be identical to the one below",
                        },
                        new Sprite
                        {
                            Texture = textures.Get("blue-to-red"),
                            Size = new Vector2(256, 128),
                        },
                    }
                };
            });
        }

        private class CustomResourceStore : IResourceStore<byte[]>
        {
            public byte[] Get(string name) => throw new System.NotImplementedException();
            public Task<byte[]> GetAsync(string name, CancellationToken cancellationToken = default) => throw new System.NotImplementedException();

            public Stream GetStream(string name)
            {
                switch (name)
                {
                    case "zero-to-red":
                    {
                        var memoryStream = new MemoryStream();

                        Image<Rgba32> image = new Image<Rgba32>(256, 1);

                        for (int i = 0; i < 256; i++)
                            image[i, 0] = new Rgba32(255, 0, 0, (byte)i);

                        image.SaveAsPng(memoryStream);
                        memoryStream.Seek(0, SeekOrigin.Begin);
                        return memoryStream;
                    }

                    case "blue-to-red":
                    {
                        var memoryStream = new MemoryStream();

                        Image<Rgba32> image = new Image<Rgba32>(256, 1);

                        for (int i = 0; i < 256; i++)
                            image[i, 0] = new Rgba32((byte)i, 0, (byte)(255 - i), 255);

                        image.SaveAsPng(memoryStream);
                        memoryStream.Seek(0, SeekOrigin.Begin);
                        return memoryStream;
                    }

                    default:
                        return Stream.Null;
                }
            }

            public IEnumerable<string> GetAvailableResources() => new[] { "zero-to-red", "blue-to-red" };

            public void Dispose()
            {
            }
        }
    }
}
