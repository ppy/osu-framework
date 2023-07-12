// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using osuTK;
using SixLabors.ImageSharp.PixelFormats;
using PixelFormat = osuTK.Graphics.ES30.PixelFormat;

namespace osu.Framework.Tests.Visual.Performance
{
    public partial class TestSceneTextureUploadPerformance : PerformanceTestScene
    {
        private FillFlowContainer<Sprite> fill = null!;

        [Resolved]
        private IRenderer renderer { get; set; } = null!;

        private ReusableTextureUpload sampleTextureUpload = null!;

        [BackgroundDependencyLoader]
        private void load(Game game, GameHost host)
        {
            var textureLoaderStore = host.CreateTextureLoaderStore(new NamespacedResourceStore<byte[]>(game.Resources, @"Textures"));

            sampleTextureUpload = new ReusableTextureUpload(textureLoaderStore.Get(@"sample-texture"));
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Add(fill = new FillFlowContainer<Sprite>
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Full,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
            });

            AddSliderStep("count", 1, 100, 10, newCount =>
            {
                for (int i = fill.Count - 1; i >= newCount; i--)
                    fill.Remove(fill.Children[i], true);

                for (int i = fill.Count; i < newCount; i++)
                    fill.Add(createSprite());
            });
        }

        private Sprite createSprite() => new Sprite
        {
            Size = new Vector2(64),
            Texture = renderer.CreateTexture(512, 512)
        };

        private ulong lastUploadedFrame;

        protected override void Update()
        {
            base.Update();

            // Ensure we don't hit a runaway scenario where too many uploads are queued
            // due to the update loop running at a higher rate than draw loop.
            if (lastUploadedFrame != renderer.FrameIndex)
            {
                foreach (var sprite in fill)
                    sprite.Texture.SetData(sampleTextureUpload);
                lastUploadedFrame = renderer.FrameIndex;
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            sampleTextureUpload.Dispose();
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
