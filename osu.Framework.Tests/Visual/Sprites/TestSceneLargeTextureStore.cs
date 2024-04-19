// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using osuTK;

namespace osu.Framework.Tests.Visual.Sprites
{
    public partial class TestSceneLargeTextureStore : FrameworkTestScene
    {
        public TestSceneLargeTextureStore()
        {
            AddRange(new Drawable[]
            {
                new LargeTextureStoreProvider(true)
                {
                    Width = 0.5f
                },
                new Box
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    RelativeSizeAxes = Axes.Y,
                    Width = 2f
                },
                new LargeTextureStoreProvider(false)
                {
                    Width = 0.5f,
                    RelativePositionAxes = Axes.X,
                    X = 0.5f
                }
            });
        }

        private partial class LargeTextureStoreProvider : CompositeDrawable
        {
            private readonly bool useManualMipmaps;
            private DependencyContainer dependencies = null!;

            public LargeTextureStoreProvider(bool useManualMipmaps)
            {
                this.useManualMipmaps = useManualMipmaps;
            }

            [BackgroundDependencyLoader]
            private void load(IRenderer renderer, GameHost host, Game game)
            {
                dependencies.CacheAs(new LargeTextureStore(renderer, host.CreateTextureLoaderStore(new NamespacedResourceStore<byte[]>(game.Resources, "Textures")), manualMipmaps: useManualMipmaps));

                RelativeSizeAxes = Axes.Both;
                InternalChildren = new Drawable[]
                {
                    new SpriteText
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Text = $"useManaulMipmaps: {useManualMipmaps}"
                    },
                    new GridContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding { Top = 20 },
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
                    }
                };
            }

            protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
                => dependencies = new DependencyContainer(base.CreateChildDependencies(parent));
        }

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
                Scale = new Vector2(0.4f);
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
