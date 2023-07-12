// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using osuTK.Graphics;
using osuTK.Graphics.ES30;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Tests.Visual.Performance
{
    public partial class TestSceneTextureUploadPerformance : RepeatedDrawablePerformanceTestScene
    {
        [Resolved]
        private IRenderer renderer { get; set; } = null!;

        private ReusableTextureUpload sampleTextureUpload = null!;
        private ReusableTextureUpload sampleTextureUpload2 = null!;

        public readonly BindableInt UploadsPerFrame = new BindableInt();

        public readonly BindableBool Mipmaps = new BindableBool();

        [BackgroundDependencyLoader]
        private void load(Game game, GameHost host)
        {
            var textureLoaderStore = host.CreateTextureLoaderStore(new NamespacedResourceStore<byte[]>(game.Resources, @"Textures"));

            sampleTextureUpload = new ReusableTextureUpload(textureLoaderStore.Get(@"sample-texture"));
            sampleTextureUpload2 = new ReusableTextureUpload(textureLoaderStore.Get(@"sample-texture-2"));
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddLabel("Upload");

            AddToggleStep("mipmap generation", v => Mipmaps.Value = v);
            AddSliderStep("uploads per frame", 1, 256, 50, v => UploadsPerFrame.Value = v);

            Mipmaps.BindValueChanged(_ => Recreate());
        }

        protected override Drawable CreateDrawable() => new Sprite
        {
            Texture = renderer.CreateTexture(512, 512, manualMipmaps: !Mipmaps.Value, initialisationColour: Color4.Black),
        };

        private ulong lastUploadedFrame;

        private int updateOffset;

        protected override void Update()
        {
            base.Update();

            // Ensure we don't hit a runaway scenario where too many uploads are queued
            // due to the update loop running at a higher rate than draw loop.
            if (lastUploadedFrame != renderer.FrameIndex && Flow.Count > 0)
            {
                for (int i = 0; i < UploadsPerFrame.Value; i++)
                {
                    var sprite = Flow[updateOffset++ % Flow.Count];

                    var upload = (int)(renderer.FrameIndex / ((float)DrawableCount.Value / UploadsPerFrame.Value)) % 2 == 0
                        ? sampleTextureUpload
                        : sampleTextureUpload2;

                    ((Sprite)sprite).Texture.SetData(upload);
                }

                lastUploadedFrame = renderer.FrameIndex;
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            sampleTextureUpload.Dispose();
            sampleTextureUpload2.Dispose();
            base.Dispose(isDisposing);
        }

        private class ReusableTextureUpload : ITextureUpload
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

            public ReusableTextureUpload(ITextureUpload upload)
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
