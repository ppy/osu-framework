// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public partial class TestSceneFileSelector : FrameworkTestScene
    {
        private BasicFileSelector selector = null!;

        [Resolved]
        private GameHost host { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            Add(selector = new BasicFileSelector(null, new[] { ".png", ".jpg", ".jpeg" }) { RelativeSizeAxes = Axes.Both });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            selector.CurrentFile.BindValueChanged(f =>
            {
                using var resources = new StorageBackedResourceStore(host.GetStorage(f.NewValue.Directory!.FullName));
                using var store = new TextureStore(host.Renderer, host.CreateTextureLoaderStore(resources));

                Add(new Sprite
                {
                    FillMode = FillMode.Fit,
                    RelativeSizeAxes = Axes.Both,
                    Texture = store.Get(Path.GetFileName(f.NewValue.FullName)),
                });
            });
        }
    }
}
