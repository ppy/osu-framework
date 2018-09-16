// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseDelayedUnload : TestCase
    {
        private const int panel_count = 1024;

        public TestCaseDelayedUnload()
        {
            FillFlowContainerNoInput flow;
            ScrollContainer scroll;

            Children = new Drawable[]
            {
                scroll = new ScrollContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        flow = new FillFlowContainerNoInput
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                        }
                    }
                }
            };

            for (int i = 1; i < panel_count; i++)
                flow.Add(new Container
                {
                    Size = new Vector2(128),
                    Children = new Drawable[]
                    {
                        new DelayedLoadUnloadWrapper(() => new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Children = new Drawable[]
                            {
                                new TestBox{ RelativeSizeAxes = Axes.Both }
                            }
                        }, 500, 2000),
                        new SpriteText { Text = i.ToString() },
                    }
                });

            var childrenWithAvatarsLoaded = flow.Children.Where(c => c.Children.OfType<DelayedLoadWrapper>().First().Content?.IsLoaded ?? false);

            int loadedCountInitial = 0;
            int loadedCountSecondary = 0;

            AddUntilStep(() => (loadedCountInitial = childrenWithAvatarsLoaded.Count()) > 5, "wait some loaded");

            AddStep("scroll down", () => scroll.ScrollToEnd());

            AddUntilStep(() => (loadedCountSecondary = childrenWithAvatarsLoaded.Count()) > loadedCountInitial, "wait more loaded");

            AddAssert("not too many loaded", () => childrenWithAvatarsLoaded.Count() < panel_count / 4);

            AddUntilStep(() => childrenWithAvatarsLoaded.Count() < loadedCountSecondary, "wait some unloaded");
        }

        private class FillFlowContainerNoInput : FillFlowContainer<Container>
        {
            public override bool HandleKeyboardInput => false;
            public override bool HandleMouseInput => false;
        }

        public class TestBox : Container
        {
            public TestBox()
            {
                RelativeSizeAxes = Axes.Both;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                Child = new SpriteText
                {
                    Colour = Color4.Yellow,
                    Text = @"loaded",
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                };
            }
        }
    }
}
