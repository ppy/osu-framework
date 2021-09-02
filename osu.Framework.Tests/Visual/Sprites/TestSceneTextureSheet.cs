// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osuTK;

namespace osu.Framework.Tests.Visual.Sprites
{
    public class TestSceneTextureSheet : FrameworkTestScene
    {
        [Resolved]
        protected TextureStore Textures { get; private set; }

        [Test]
        public void TestManualTextureSheet()
        {
            AddStep("clear content", () => Clear());

            var sheet = new TextureSheet(Textures.Get(@"sample-texture"));

            sheet.Append(new TextureCropSchema[]
            {
                new TextureCropSchema
                {
                    Size = new Vector2(256),
                    Offset = new Vector2(0)
                },
                new TextureCropSchema
                {
                    Size = new Vector2(256),
                    Offset = new Vector2(256, 0)
                },
                new TextureCropSchema
                {
                    Size = new Vector2(256),
                    Offset = new Vector2(0, 256)
                },
                new TextureCropSchema
                {
                    Size = new Vector2(256),
                    Offset = new Vector2(256, 256)
                }
            });

            var textures = sheet.Build();

            AddAssert("validate textures", () =>
            {
                foreach (var texture in textures)
                {
                    if (texture == null)
                        return false;
                }

                return true;
            });

            var sprites = new List<Sprite>
            {
                new Sprite
                {
                    Texture = textures[0]
                },
                new Sprite
                {
                    Texture = textures[1]
                },
                new Sprite
                {
                    Texture = textures[2]
                },
                new Sprite
                {
                    Texture = textures[3]
                },
            };

            AddStep("draw sprites", () =>
            {
                Add(new FillFlowContainer<Sprite>
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(512),
                    Direction = FillDirection.Full,
                    Children = sprites
                });
            });
        }

        [Test]
        public void TestAutomatedTextureSheet()
        {
            AddStep("clear content", () => Clear());

            var sheet = TextureSheet.Auto(Textures.Get(@"sample-texture"), 2, 2, new Vector2(256), new Vector2(0));

            var textures = sheet.Build();

            AddAssert("validate textures", () =>
            {
                foreach (var texture in textures)
                {
                    if (texture == null)
                        return false;
                }

                return true;
            });

            var sprites = new List<Sprite>
            {
                new Sprite
                {
                    Texture = textures[0]
                },
                new Sprite
                {
                    Texture = textures[1]
                },
                new Sprite
                {
                    Texture = textures[2]
                },
                new Sprite
                {
                    Texture = textures[3]
                },
            };

            AddStep("draw sprites", () =>
            {
                Add(new FillFlowContainer<Sprite>
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(512),
                    Direction = FillDirection.Full,
                    Children = sprites
                });
            });
        }
    }
}
