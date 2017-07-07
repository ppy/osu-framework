// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.VisualTests.Tests
{
    internal class TestCaseOnlineTextures : TestCase
    {
        private const int panel_count = 2048;

        public TestCaseOnlineTextures()
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
                        new DelayedLoadWrapper(new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            OnLoadComplete = d =>
                            {
                                var c = (Container)d;

                                if ((c.Children.FirstOrDefault() as Sprite)?.Texture == null)
                                {
                                    c.Add(new SpriteText {
                                        Colour = Color4.Gray,
                                        Text = @"nope",
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                    });
                                }
                            },
                            Children = new Drawable[]
                            {
                                new Avatar(i) { RelativeSizeAxes = Axes.Both }
                            }
                        }),
                        new SpriteText { Text = i.ToString() },
                    }
                });

            var childrenWithAvatarsLoaded = flow.Children.Where(c => c.Children.OfType<DelayedLoadWrapper>().First().Children.FirstOrDefault()?.IsLoaded ?? false);

            AddWaitStep(10);
            AddStep("scroll down", () => scroll.ScrollToEnd());
            AddWaitStep(10);
            AddAssert("some loaded", () => childrenWithAvatarsLoaded.Count() > 5);
            AddAssert("not too many loaded", () => childrenWithAvatarsLoaded.Count() < panel_count / 4);
        }

        private class FillFlowContainerNoInput : FillFlowContainer<Container>
        {
            public override bool HandleInput => false;
        }
    }

    public class Avatar : Sprite
    {
        private readonly int userId;

        public Avatar(int userId)
        {
            this.userId = userId;
        }

        [BackgroundDependencyLoader]
        private void load(TextureStore textures)
        {
            Texture = textures.Get($@"https://a.ppy.sh/{userId}");
        }
    }
}
