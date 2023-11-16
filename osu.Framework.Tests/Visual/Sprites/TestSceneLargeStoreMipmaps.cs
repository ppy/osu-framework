// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using osuTK;

namespace osu.Framework.Tests.Visual.Sprites
{
    public partial class TestSceneLargeStoreMipmaps : FrameworkTestScene
    {
        private DependencyContainer dependencies = null!;

        [BackgroundDependencyLoader]
        private void load(IRenderer renderer, GameHost host, Game game)
        {
            dependencies.CacheAs(new LargeTextureStore(renderer, host.CreateTextureLoaderStore(new NamespacedResourceStore<byte[]>(game.Resources, "Textures")), manualMipmaps: false));

            Child = new GridContainer
            {
                RelativeSizeAxes = Axes.Both,
                ColumnDimensions = new[]
                {
                    new Dimension(GridSizeMode.Relative, 0.5f),
                    new Dimension(GridSizeMode.Relative, 0.5f),
                },
                RowDimensions = new[]
                {
                    new Dimension(GridSizeMode.Relative, 0.5f),
                    new Dimension(GridSizeMode.Relative, 0.5f),
                },
                Content = new[]
                {
                    new Drawable[]
                    {
                        new MipmapSprite(0),
                        new MipmapSprite(1)
                    },
                    new Drawable[]
                    {
                        new MipmapSprite(2),
                        new MipmapSprite(3)
                    }
                }
            };
        }

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
            => dependencies = new DependencyContainer(base.CreateChildDependencies(parent));

        private partial class MipmapSprite : Sprite
        {
            private readonly int level;

            public MipmapSprite(int level)
            {
                this.level = level;
            }

            [BackgroundDependencyLoader]
            private void load(LargeTextureStore textures)
            {
                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;
                Texture = textures.Get("sample-texture.png");
                Scale = new Vector2(0.5f);
            }

            protected override DrawNode CreateDrawNode() => new SingleMipmapSpriteDrawNode(this, level);

            private class SingleMipmapSpriteDrawNode : SpriteDrawNode
            {
                private readonly int level;

                public SingleMipmapSpriteDrawNode(Sprite source, int level)
                    : base(source)
                {
                    this.level = level;
                }

                protected override void Blit(IRenderer renderer)
                {
                    Texture.MipLevel = level;
                    base.Blit(renderer);
                    renderer.FlushCurrentBatch(null);
                    Texture.MipLevel = null;
                }
            }
        }
    }
}
