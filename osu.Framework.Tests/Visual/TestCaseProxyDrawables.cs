// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseProxyDrawables : TestCase
    {
        public TestCaseProxyDrawables()
        {
            Child = new ScrollContainer
            {
                RelativeSizeAxes = Axes.Both,
                Child = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Children = new[]
                    {
                        generateProxyAboveAllPresentTest(1),
                        generateProxyBelowAllPresentTest(1),
                        generateProxyAboveAllPresentTest(6),
                        generateProxyBelowAllPresentTest(6),
                        generateProxyAboveOriginalMaskedAway(1),
                        generateProxyBelowOriginalMaskedAway(1),
                        generateProxyAboveOriginalMaskedAway(6),
                        generateProxyBelowOriginalMaskedAway(6),
                        generateProxyAboveBoxParentNotPresent(1),
                        generateProxyBelowBoxParentNotPresent(1),
                        generateProxyAboveBoxParentNotPresent(6),
                        generateProxyBelowBoxParentNotPresent(6),
                        generateProxyAboveBoxParentNotAlive(1),
                        generateProxyBelowBoxParentNotAlive(1),
                        generateProxyAboveBoxParentNotAlive(6),
                        generateProxyBelowBoxParentNotAlive(6),
                        generateProxyAboveParentOriginalIndirectlyMaskedAway(1),
                        generateProxyBelowParentOriginalIndirectlyMaskedAway(1),
                        generateProxyAboveParentOriginalIndirectlyMaskedAway(6),
                        generateProxyBelowParentOriginalIndirectlyMaskedAway(6),
                    }
                }
            };
        }

        private Drawable generateProxyAboveAllPresentTest(int proxyCount)
        {
            var box = new Box
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(50),
            };

            var proxy = box.CreateProxy();
            for (int i = 1; i < proxyCount; i++)
                proxy = proxy.CreateProxy();

            return new Visualiser($"{proxyCount} proxy(s) above, all present", box, proxy);
        }

        private Drawable generateProxyBelowAllPresentTest(int proxyCount)
        {
            var box = new Box
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(50),
            };

            var proxy = box.CreateProxy();
            for (int i = 1; i < proxyCount; i++)
                proxy = proxy.CreateProxy();

            return new Visualiser($"{proxyCount} proxy(s) below, all present", proxy, box);
        }

        private Drawable generateProxyAboveOriginalMaskedAway(int proxyCount)
        {
            var box = new Box
            {
                RelativeSizeAxes = Axes.Both,
                Position = new Vector2(60, 0)
            };

            var boxMaskingContainer = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(50),
                Masking = true,
                BorderColour = Color4.Yellow,
                BorderThickness = 2,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Alpha = 0,
                        AlwaysPresent = true
                    },
                    box
                }
            };

            var proxy = box.CreateProxy();
            for (int i = 1; i < proxyCount; i++)
                proxy = proxy.CreateProxy();

            return new Visualiser($"{proxyCount} proxy(s) above, original masked away", boxMaskingContainer, proxy);
        }

        private Drawable generateProxyBelowOriginalMaskedAway(int proxyCount)
        {
            var box = new Box
            {
                RelativeSizeAxes = Axes.Both,
                Position = new Vector2(60, 0)
            };

            var boxMaskingContainer = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(50),
                Masking = true,
                BorderColour = Color4.Yellow,
                BorderThickness = 2,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Alpha = 0,
                        AlwaysPresent = true
                    },
                    box
                }
            };

            var proxy = box.CreateProxy();
            for (int i = 1; i < proxyCount; i++)
                proxy = proxy.CreateProxy();

            return new Visualiser($"{proxyCount} proxy(s) below, original masked away", proxy, boxMaskingContainer);
        }

        private Drawable generateProxyAboveBoxParentNotPresent(int proxyCount)
        {
            var box = new Box { RelativeSizeAxes = Axes.Both };

            var invisibleContainer = new NonPresentContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(50),
                Child = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = box
                }
            };

            var proxy = box.CreateProxy();
            for (int i = 1; i < proxyCount; i++)
                proxy = proxy.CreateProxy();

            return new Visualiser($"{proxyCount} proxy(s) above, original parent invisible", invisibleContainer, proxy);
        }

        private Drawable generateProxyBelowBoxParentNotPresent(int proxyCount)
        {
            var box = new Box { RelativeSizeAxes = Axes.Both };

            var invisibleContainer = new NonPresentContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(50),
                Child = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = box
                }
            };

            var proxy = box.CreateProxy();
            for (int i = 1; i < proxyCount; i++)
                proxy = proxy.CreateProxy();

            return new Visualiser($"{proxyCount} proxy(s) below, original parent invisible", proxy, invisibleContainer);
        }

        private Drawable generateProxyAboveBoxParentNotAlive(int proxyCount)
        {
            var box = new Box { RelativeSizeAxes = Axes.Both };

            var invisibleContainer = new NonAliveContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(50),
                Child = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = box
                }
            };

            var proxy = box.CreateProxy();
            for (int i = 1; i < proxyCount; i++)
                proxy = proxy.CreateProxy();

            return new Visualiser($"{proxyCount} proxy(s) above, original parent not alive", invisibleContainer, proxy);
        }

        private Drawable generateProxyBelowBoxParentNotAlive(int proxyCount)
        {
            var box = new Box { RelativeSizeAxes = Axes.Both };

            var invisibleContainer = new NonAliveContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(50),
                Child = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = box
                }
            };

            var proxy = box.CreateProxy();
            for (int i = 1; i < proxyCount; i++)
                proxy = proxy.CreateProxy();

            return new Visualiser($"{proxyCount} proxy(s) below, original parent not alive", proxy, invisibleContainer);
        }

        private class NonPresentContainer : Container
        {
            public override bool IsPresent => false;
        }

        private class NonAliveContainer : Container
        {
            protected internal override bool ShouldBeAlive => false;
        }

        private class Visualiser : CompositeDrawable
        {
            private readonly Drawable original;
            private readonly Drawable overlay;

            public Visualiser(string description, Drawable layerBelow, Drawable layerAbove)
            {
                Size = new Vector2(300);

                bool proxyIsBelow = layerBelow.IsProxy;

                original = proxyIsBelow ? layerBelow : layerAbove;
                while (original != (original = original.Original))
                {
                }

                overlay = new Container
                {
                    Colour = proxyIsBelow ? Color4.Red : Color4.Green,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Alpha = 0.5f,
                        },
                        new SpriteText
                        {
                            Anchor = Anchor.BottomCentre,
                            Origin = Anchor.BottomCentre,
                            Text = "proxy"
                        }
                    }
                };

                if (proxyIsBelow)
                {
                    InternalChildren = new[]
                    {
                        overlay,
                        layerBelow,
                        layerAbove,
                    };
                }
                else
                {
                    InternalChildren = new[]
                    {
                        layerBelow,
                        layerAbove,
                        overlay
                    };
                }

                AddRangeInternal(new Drawable[]
                {
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Masking = true,
                        BorderColour = Color4.Gray,
                        BorderThickness = 2,
                        Child = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Alpha = 0,
                            AlwaysPresent = true
                        }
                    },
                    new SpriteText
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Y = 10,
                        Text = description
                    }
                });
            }

            protected override void Update()
            {
                base.Update();

                var aabb = ToLocalSpace(original.ScreenSpaceDrawQuad).AABBFloat.Inflate(15);
                overlay.Position = aabb.Location;
                overlay.Size = aabb.Size;
            }
        }
    }
}
