// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Testing;
using osuTK;

namespace osu.Framework.Tests.Visual.Sprites
{
    public class TestSceneTextureCropping : GridTestScene
    {
        public TestSceneTextureCropping()
            : base(3, 3)
        {
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            for (int i = 0; i < Rows; ++i)
            {
                for (int j = 0; j < Cols; ++j)
                {
                    RectangleF cropRectangle = new RectangleF(i / 3f, j / 3f, 1 / 3f, 1 / 3f);
                    Cell(i, j).AddRange(new Drawable[]
                    {
                        new SpriteText
                        {
                            Text = $"{cropRectangle}",
                            Font = new FontUsage(size: 14),
                        },
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Size = new Vector2(0.5f),
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Masking = true,
                            Children = new Drawable[]
                            {
                                new Sprite
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Texture = texture?.Crop(cropRectangle, relativeSizeAxes: Axes.Both),
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                }
                            }
                        }
                    });
                }
            }
        }

        private Texture texture;

        [BackgroundDependencyLoader]
        private void load(TextureStore store)
        {
            texture = store.Get(@"sample-texture");
        }
    }
}
