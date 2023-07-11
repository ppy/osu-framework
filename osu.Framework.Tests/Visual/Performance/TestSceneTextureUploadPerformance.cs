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
        private int count;

        private FillFlowContainer<Sprite>? fill;

        [Resolved]
        private IRenderer renderer { get; set; } = null!;

        private PersistentTextureUpload sampleTextureUpload = null!;

        [BackgroundDependencyLoader]
        private void load(Game game, GameHost host)
        {
            var textureLoaderStore = host.CreateTextureLoaderStore(new NamespacedResourceStore<byte[]>(game.Resources, @"Textures"));

            sampleTextureUpload = new PersistentTextureUpload(textureLoaderStore.Get(@"sample-texture"));
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddSliderStep("count", 1, 100, 10, v =>
            {
                count = v;
                recreate();
            });
        }

        private void recreate()
        {
            Child = fill = new FillFlowContainer<Sprite>
            {
                RelativeSizeAxes = Axes.X,
                Width = 0.5f,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Full,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
            };

            for (int i = 0; i < count; i++)
            {
                fill.Add(new Sprite
                {
                    Size = new Vector2(128),
                    Texture = renderer.CreateTexture(512, 512)
                });
            }
        }

        private ulong lastUploadedFrame;

        protected override void Update()
        {
            base.Update();

            if (fill == null) return;

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
