// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Testing;
using osuTK.Graphics.ES30;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Tests.Visual.Sprites
{
    public class TestSceneTextureUnit : GridTestScene
    {
        public TestSceneTextureUnit()
            : base(2, 2)
        {
            Cell(0, 0).Child = createTest(TextureUnit.Texture0, "white");
            Cell(0, 1).Child = createTest(TextureUnit.Texture1, "red");
            Cell(1, 0).Child = createTest(TextureUnit.Texture2, "green");
            Cell(1, 1).Child = createTest(TextureUnit.Texture3, "blue");
        }

        private Drawable createTest(TextureUnit unit, string expectedColour) => new Container
        {
            RelativeSizeAxes = Axes.Both,
            Children = new Drawable[]
            {
                new SpriteText
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Text = $"Unit {unit - TextureUnit.Texture0} ({expectedColour})"
                },
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Top = 20 },
                    Child = new TestSprite(unit)
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        RelativeSizeAxes = Axes.Both,
                        FillMode = FillMode.Fit
                    }
                }
            }
        };

        private class TestSprite : Sprite
        {
            private readonly TextureUnit unit;

            private Texture redTex;
            private Texture greenTex;
            private Texture blueTex;

            public TestSprite(TextureUnit unit)
            {
                this.unit = unit;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                // 0 -> White, 1 -> Red, 2 -> Green, 3 -> Blue
                Texture = createTexture(new Rgba32(255, 255, 255, 255));
                redTex = createTexture(new Rgba32(255, 0, 0, 255));
                greenTex = createTexture(new Rgba32(0, 255, 0, 255));
                blueTex = createTexture(new Rgba32(0, 0, 255, 255));
            }

            private Texture createTexture(Rgba32 pixel)
            {
                var texData = new Image<Rgba32>(32, 32);

                for (int x = 0; x < texData.Width; x++)
                {
                    for (int y = 0; y < texData.Height; y++)
                        texData[x, y] = pixel;
                }

                var tex = new Texture(texData.Width, texData.Height, true);
                tex.SetData(new TextureUpload(texData));

                return tex;
            }

            protected override DrawNode CreateDrawNode() => new TestBoxDrawNode(this, unit);

            private class TestBoxDrawNode : SpriteDrawNode
            {
                protected new TestSprite Source => (TestSprite)base.Source;

                private readonly TextureUnit unit;

                private Texture redTex;
                private Texture greenTex;
                private Texture blueTex;

                public TestBoxDrawNode(TestSprite source, TextureUnit unit)
                    : base(source)
                {
                    this.unit = unit;
                }

                public override void ApplyState()
                {
                    base.ApplyState();

                    redTex = Source.redTex;
                    greenTex = Source.greenTex;
                    blueTex = Source.blueTex;
                }

                public override void Draw(Action<TexturedVertex2D> vertexAction)
                {
                    redTex.TextureGL.Bind(TextureUnit.Texture1);
                    greenTex.TextureGL.Bind(TextureUnit.Texture2);
                    blueTex.TextureGL.Bind(TextureUnit.Texture3);

                    int unitId = unit - TextureUnit.Texture0;
                    Shader.GetUniform<int>("m_Sampler").UpdateValue(ref unitId);

                    base.Draw(vertexAction);

                    unitId = 0;
                    Shader.GetUniform<int>("m_Sampler").UpdateValue(ref unitId);
                }

                protected internal override bool CanDrawOpaqueInterior => false;
            }
        }
    }
}
