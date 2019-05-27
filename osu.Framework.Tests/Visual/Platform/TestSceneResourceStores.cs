// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual.Platform
{
    public class TestSceneResourceStores : FrameworkTestScene
    {
        private FontStore fontStore;
        private FillFlowContainer textContainer;
        private Storage storage;

        [BackgroundDependencyLoader]
        private void load(FontStore fontStore, Storage storage)
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
        }

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("Clear text container", () => textContainer.Clear());
        }

        [Test]
        public void TestGetAvailableResources()
        {
            AddStep("get resources", () => populateText(fontStore.GetAvailableResources().ToArray()));
        }

        [Test]
        public void TestGetStorageBackedResources()
        {
            AddStep("get storage backed resources", () => populateText(new StorageBackedResourceStore(storage).GetAvailableResources()));
        }

        private void populateText(IEnumerable<string> lines)
        {
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
