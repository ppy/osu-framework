// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osuTK.Graphics.ES30;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Tests.Visual.Performance
{
    public partial class TestSceneTexturePerformance : TestSceneBoxPerformance
    {
        private readonly BindableBool disableMipmaps = new BindableBool();
        private readonly BindableBool uniqueTextures = new BindableBool();

        private Texture nonMipmappedSampleTexture = null!;
        private Texture mipmappedSampleTexture = null!;
        private PersistentTextureUpload sampleTextureUpload = null!;

        private IResourceStore<TextureUpload> textureLoaderStore = null!;

        [Resolved]
        private IRenderer renderer { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load(Game game, TextureStore store, GameHost host)
        {
            textureLoaderStore = host.CreateTextureLoaderStore(new NamespacedResourceStore<byte[]>(game.Resources, @"Textures"));

            mipmappedSampleTexture = store.Get(@"sample-texture");
            nonMipmappedSampleTexture = new TextureStore(renderer, textureLoaderStore, manualMipmaps: true).Get(@"sample-texture");
            sampleTextureUpload = new PersistentTextureUpload(textureLoaderStore.Get(@"sample-texture"));
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddLabel("Textures");
            AddToggleStep("disable mipmaps", v => disableMipmaps.Value = v);
            AddToggleStep("unique textures", v => uniqueTextures.Value = v);
        }

        protected override double TimePerAction => 100;

        [Test]
        public void TestUploadPerformance()
        {
            AddStep("clear textures", () =>
            {
                foreach (var sprite in this.ChildrenOfType<TestSprite>())
                    sprite.Texture = renderer.CreateTexture(mipmappedSampleTexture.Width, mipmappedSampleTexture.Height);
            });

            AddRepeatStep("upload texture", () =>
            {
                foreach (var sprite in this.ChildrenOfType<TestSprite>())
                    sprite.Texture.SetData(sampleTextureUpload);
            }, 25);
        }

        protected override void Dispose(bool isDisposing)
        {
            sampleTextureUpload.Dispose();
            base.Dispose(isDisposing);
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

            private readonly Texture mipmappedTexture;
            private readonly Texture nonMipmappedTexture;

            private TextureStore? spriteLocalStore;

            public TestSprite(Texture mipmappedTexture, Texture nonMipmappedTexture)
            {
                this.mipmappedTexture = mipmappedTexture;
                this.nonMipmappedTexture = nonMipmappedTexture;
            }

            [BackgroundDependencyLoader]
            private void load(IRenderer renderer, GameHost host, Game game)
            {
                DisableMipmaps.BindValueChanged(v =>
                {
                    spriteLocalStore?.Dispose();
                    spriteLocalStore = new TextureStore(renderer, host.CreateTextureLoaderStore(new NamespacedResourceStore<byte[]>(game.Resources, @"Textures")), manualMipmaps: v.NewValue);

                    updateTexture();
                }, true);

                UniqueTextures.BindValueChanged(v => updateTexture(), true);
            }

            private void updateTexture()
            {
                if (UniqueTextures.Value)
                    Texture = spriteLocalStore!.Get(@"sample-texture");
                else
                    Texture = DisableMipmaps.Value ? nonMipmappedTexture : mipmappedTexture;
            }
        }

        private class PersistentTextureUpload : ITextureUpload
        {
            private readonly ITextureUpload upload;

            public ReadOnlySpan<Rgba32> Data => upload.Data;

            public int Level => upload.Level;

            public RectangleI Bounds
            {
                get => upload.Bounds;
                set => upload.Bounds = value;
            }

            public PixelFormat Format => upload.Format;

            public PersistentTextureUpload(ITextureUpload upload)
            {
                this.upload = upload;
            }

            void IDisposable.Dispose()
            {
            }

            public void Dispose() => upload.Dispose();
        }
    }
}
