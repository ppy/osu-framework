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
    public partial class TestSceneTextureAverageColour : FrameworkTestScene
    {
        public TestSceneTextureAverageColour()
        {
            Add(new TextureStoreProvider());
        }

        private partial class TextureStoreProvider : FillFlowContainer
        {
            private DependencyContainer dependencies = null!;

            [BackgroundDependencyLoader]
            private void load(IRenderer renderer, GameHost host, Game game)
            {
                dependencies.CacheAs(new TextureStore(renderer, host.CreateTextureLoaderStore(new NamespacedResourceStore<byte[]>(game.Resources, "Textures"))));

                AutoSizeAxes = Axes.Y;
                RelativeSizeAxes = Axes.X;
                Direction = FillDirection.Full;
                Spacing = new Vector2(10);
                Children = new[]
                {
                    new Cell("d3-colour-interpolation"),
                    new Cell("figma-colour-interpolation"),
                    new Cell("sample-texture"),
                    new Cell("sample-texture-2"),
                };
            }

            protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
                => dependencies = new DependencyContainer(base.CreateChildDependencies(parent));
        }

        private partial class Cell : CompositeDrawable
        {
            private readonly string textureName;
            private Sprite sprite = null!;
            private Box coloredBox = null!;

            public Cell(string textureName)
            {
                this.textureName = textureName;
            }

            [BackgroundDependencyLoader]
            private void load(TextureStore textures)
            {
                Size = new Vector2(200);
                InternalChild = new GridContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    RowDimensions = new[]
                    {
                        new Dimension(),
                        new Dimension(GridSizeMode.Absolute, 30)
                    },
                    ColumnDimensions = new[]
                    {
                        new Dimension(GridSizeMode.Relative, 1f)
                    },
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            sprite = new Sprite
                            {
                                RelativeSizeAxes = Axes.Both,
                                Texture = textures.Get(textureName)
                            }
                        },
                        new Drawable[]
                        {
                            coloredBox = new Box
                            {
                                RelativeSizeAxes = Axes.Both
                            }
                        }
                    }
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                coloredBox.Colour = sprite.Texture.AverageColour;
            }
        }
    }
}
