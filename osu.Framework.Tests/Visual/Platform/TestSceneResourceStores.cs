// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.IO.Stores;

namespace osu.Framework.Tests.Visual.Platform
{
    public class TestSceneResourceStores : FrameworkTestScene
    {
        private FontStore fontStore;
        private FillFlowContainer textContainer;

        [BackgroundDependencyLoader]
        private void load(FontStore fontStore)
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
        }

        [Test]
        public void TestGetAvailableResources()
        {
            AddStep("get resources", () =>
            {
                var resources = fontStore.GetAvailableResources().ToArray();
                foreach (var text in resources)
                {
                    textContainer.Add(new SpriteText
                    {
                        Anchor = Anchor.TopLeft,
                        Origin = Anchor.TopLeft,
                        Text = text,
                    });
                }
            });
        }
    }
}
