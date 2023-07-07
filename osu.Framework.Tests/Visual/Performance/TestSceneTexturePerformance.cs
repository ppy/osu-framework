// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;

namespace osu.Framework.Tests.Visual.Performance
{
    public partial class TestSceneTexturePerformance : TestSceneBoxPerformance
    {
        private readonly BindableBool disableMipmaps = new BindableBool();
        private readonly BindableBool uniqueTextures = new BindableBool();

        private Texture nonMipmappedSampleTexture = null!;
        private Texture mipmappedSampleTexture = null!;

        private IResourceStore<TextureUpload> textureLoaderStore = null!;

        [Resolved]
        private IRenderer renderer { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load(Game game, TextureStore store, GameHost host)
        {
            textureLoaderStore = host.CreateTextureLoaderStore(new NamespacedResourceStore<byte[]>(game.Resources, @"Textures"));

            mipmappedSampleTexture = store.Get(@"sample-texture");
            nonMipmappedSampleTexture = new TextureStore(renderer, textureLoaderStore, manualMipmaps: true).Get(@"sample-texture");
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddLabel("Textures");
            AddToggleStep("disable mipmaps", v => disableMipmaps.Value = v);
            AddToggleStep("unique textures", v => uniqueTextures.Value = v);
        }

        protected override Drawable CreateDrawable() => new TestSprite(mipmappedSampleTexture, nonMipmappedSampleTexture)
        {
            FillWidth = { BindTarget = FillWidth },
            FillHeight = { BindTarget = FillHeight },
            GradientColour = { BindTarget = GradientColour },
            RandomiseColour = { BindTarget = RandomiseColour },
            DisableMipmaps = { BindTarget = disableMipmaps },
            UniqueTextures = { BindTarget = uniqueTextures },
        };

        private partial class TestSprite : TestBox
        {
            public readonly IBindable<bool> DisableMipmaps = new BindableBool();
            public readonly IBindable<bool> UniqueTextures = new BindableBool();

            private readonly Texture mipmapped;
            private readonly Texture nonMipmapped;

            private TextureStore? store;

            public TestSprite(Texture mipmapped, Texture nonMipmapped)
            {
                this.mipmapped = mipmapped;
                this.nonMipmapped = nonMipmapped;
            }

            [BackgroundDependencyLoader]
            private void load(IRenderer renderer, GameHost host, Game game)
            {
                DisableMipmaps.BindValueChanged(v =>
                {
                    Texture = v.NewValue ? nonMipmapped : mipmapped;

                    store?.Dispose();
                    store = new TextureStore(renderer, host.CreateTextureLoaderStore(new NamespacedResourceStore<byte[]>(game.Resources, @"Textures")), manualMipmaps: v.NewValue);
                }, true);

                UniqueTextures.BindValueChanged(v =>
                {
                    if (v.NewValue)
                        Texture = store!.Get("sample-texture");
                }, true);
            }
        }
    }
}
