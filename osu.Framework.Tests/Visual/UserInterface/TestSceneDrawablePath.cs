// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Lines;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Textures;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneDrawablePath : FrameworkTestScene
    {
        private const int texture_width = 20;

        private Texture gradientTexture;

        [BackgroundDependencyLoader]
        private void load(IRenderer renderer)
        {
            var image = new Image<Rgba32>(texture_width, 1);

            for (int i = 0; i < texture_width; ++i)
            {
                byte brightnessByte = (byte)((float)i / (texture_width - 1) * 255);
                image[i, 0] = new Rgba32(255, 255, 255, brightnessByte);
            }

            gradientTexture = renderer.CreateTexture(texture_width, 1, true);
            gradientTexture.SetData(new TextureUpload(image));
        }

        [Test]
        public void TestSimplePath()
        {
            AddStep("create path", () =>
            {
                Child = new TexturedPath
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Vertices = new List<Vector2> { Vector2.Zero, new Vector2(300, 300) },
                    Texture = gradientTexture,
                };
            });
        }

        [Test]
        public void TestMultiplePointPath()
        {
            AddStep("create path", () =>
            {
                Child = new TexturedPath
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Vertices = new List<Vector2>
                    {
                        new Vector2(50, 50),
                        new Vector2(50, 150),
                        new Vector2(150, 150),
                        new Vector2(150, 50),
                        new Vector2(50, 50),
                    },
                    Texture = gradientTexture,
                };
            });
        }

        [Test]
        public void TestSelfOverlappingPath()
        {
            AddStep("create path", () =>
            {
                Child = new TexturedPath
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Vertices = new List<Vector2>
                    {
                        new Vector2(50, 50),
                        new Vector2(50, 150),
                        new Vector2(150, 150),
                        new Vector2(150, 100),
                        new Vector2(20, 100),
                    },
                    Texture = gradientTexture,
                };
            });
        }

        [Test]
        public void TestSmoothPath()
        {
            AddStep("create path", () =>
            {
                Child = new SmoothPath
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    PathRadius = 10,
                    Vertices = new List<Vector2>
                    {
                        Vector2.Zero,
                        new Vector2(200)
                    },
                };
            });
        }

        [Test]
        public void TestUnsmoothPath()
        {
            AddStep("create path", () =>
            {
                Child = new Path
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    PathRadius = 10,
                    Vertices = new List<Vector2>
                    {
                        Vector2.Zero,
                        new Vector2(200)
                    },
                };
            });
        }

        [Test]
        public void TestPathBlending()
        {
            AddStep("create path", () =>
            {
                Children = new Drawable[]
                {
                    new Box
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Size = new Vector2(200)
                    },
                    new TexturedPath
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Colour = Color4.Red,
                        Vertices = new List<Vector2>
                        {
                            new Vector2(50, 50),
                            new Vector2(50, 150),
                            new Vector2(150, 150),
                            new Vector2(150, 100),
                            new Vector2(20, 100),
                        },
                        Texture = gradientTexture,
                    }
                };
            });
        }

        [Test]
        public void TestSizing()
        {
            Path path = null;

            AddStep("create autosize path", () =>
            {
                Child = new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(200),
                    Child = path = new Path
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        PathRadius = 10,
                        Vertices = new List<Vector2>
                        {
                            Vector2.Zero,
                            new Vector2(100, 0)
                        },
                    }
                };
            });

            AddAssert("size = (120, 20)", () => Precision.AlmostEquals(new Vector2(120, 20), path.DrawSize));

            AddStep("make path relative-sized", () =>
            {
                path.AutoSizeAxes = Axes.None;
                path.RelativeSizeAxes = Axes.Both;
                path.Size = Vector2.One;
            });

            AddAssert("size = (200, 200)", () => Precision.AlmostEquals(new Vector2(200), path.DrawSize));

            AddStep("make path absolute-sized", () =>
            {
                path.RelativeSizeAxes = Axes.None;
                path.Size = new Vector2(100);
            });

            AddAssert("size = (100, 100)", () => Precision.AlmostEquals(new Vector2(100), path.DrawSize));
        }
    }
}
