// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;

namespace osu.Framework.Tests.Visual.Platform
{
    public class TestSceneResourceStores : FrameworkTestScene
    {
        private FontStore fontStore;
        private FillFlowContainer textContainer;
        private Storage storage;
        private TextureStore textureStore;

        [BackgroundDependencyLoader]
        private void load(FontStore fontStore, Storage storage, AudioManager audioManager, TextureStore textureStore)
        {
            Child = new ScrollContainer
            {
                RelativeSizeAxes = Axes.Both,
                Child = textContainer = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                },
            };

            this.fontStore = fontStore;
            this.storage = storage;
            this.textureStore = textureStore;
        }

        [Test]
        public void TestGetFontStoreResources()
        {
            AddStep("Print resources in font store", () => populateText(fontStore.GetAvailableResources()));
        }

        [Test]
        public void TestGetStorageBackedResources()
        {
            AddStep("Print storage backed resources", () => populateText(new StorageBackedResourceStore(storage).GetAvailableResources()));
        }

        [Test]
        public void TestGetTextureStore()
        {
            AddStep("Print texture store", () => populateText(textureStore.GetAvailableResources()));
        }

        private void populateText(IEnumerable<string> lines)
        {
            textContainer.Clear();
            foreach (var text in lines)
            {
                textContainer.Add(new SpriteText
                {
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    Text = text,
                });
            }
        }
    }
}
