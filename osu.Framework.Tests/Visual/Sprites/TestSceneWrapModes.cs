// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Sprites
{
    [System.ComponentModel.Description("texture wrap modes")]
    public class TestSceneWrapModes : GridTestScene
    {
        private readonly WrapMode[] wrapModes = { WrapMode.None, WrapMode.ClampToEdge, WrapMode.ClampToBorder, WrapMode.Repeat };

        public TestSceneWrapModes()
            : base(4, 4)
        {
        }

        private readonly Texture[] textures = new Texture[4 * 4];

        [BackgroundDependencyLoader]
        private void load(TextureStore store)
        {
            for (int i = 0; i < 4; ++i)
            {
                for (int j = 0; j < 4; ++j)
                    textures[i * 4 + j] = store.Get(@"sample-texture", wrapModes[i], wrapModes[j]);
            }
        }

        [Test]
        public void TestSprites() => createTest(tex => new Sprite
        {
            Texture = tex,
            TextureRectangle = new RectangleF(0.25f, 0.25f, 0.5f, 0.5f),
        });

        [Test]
        public void TestTriangles() => createTest(tex => new EquilateralTriangle
        {
            Texture = tex,
            TextureRectangle = new RectangleF(0.25f, 0.25f, 0.5f, 0.5f),
        });

        [Test, Ignore("not implemented yet")]
        public void TestVideos() => createTest(_ => new TestVideo());

        private void createTest(Func<Texture, Drawable> creatorFunc) => AddStep("create test", () =>
        {
            for (int i = 0; i < Rows; ++i)
            {
                for (int j = 0; j < Cols; ++j)
                {
                    Cell(i, j).Children = new Drawable[]
                    {
                        new SpriteText
                        {
                            Text = $"S={wrapModes[i]},T={wrapModes[j]}",
                            Font = new FontUsage(size: 20),
                        },
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Size = new Vector2(0.5f),
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Children = new[]
                            {
                                creatorFunc(textures[i * 4 + j]).With(d =>
                                {
                                    d.RelativeSizeAxes = Axes.Both;
                                    d.Size = Vector2.One;
                                    d.Anchor = Anchor.Centre;
                                    d.Origin = Anchor.Centre;
                                }),
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Size = new Vector2(0.5f),
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Masking = true,
                                    BorderColour = Color4.Red,
                                    BorderThickness = 2,
                                    Child = new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = new Color4(0, 0, 0, 0),
                                    }
                                }
                            }
                        }
                    };
                }
            }
        });
    }
}
