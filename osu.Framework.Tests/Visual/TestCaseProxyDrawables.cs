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
    public class TestCaseProxyDrawables : GridTestCase
    {
        public TestCaseProxyDrawables()
            : base(4, 4)
        {
            Cell(0, 0).Child = generateProxyAboveAllPresentTest(1);
            Cell(0, 1).Child = generateProxyBelowAllPresentTest(1);
            Cell(0, 2).Child = generateProxyAboveAllPresentTest(6);
            Cell(0, 3).Child = generateProxyBelowAllPresentTest(6);

            Cell(1, 0).Child = generateProxyAboveOriginalMaskedAway(1);
            Cell(1, 1).Child = generateProxyBelowOriginalMaskedAway(1);
            Cell(1, 2).Child = generateProxyAboveOriginalMaskedAway(6);
            Cell(1, 3).Child = generateProxyBelowOriginalMaskedAway(6);

            Cell(2, 0).Child = generateProxyAboveBoxParentNotPresent(1);
            Cell(2, 1).Child = generateProxyBelowBoxParentNotPresent(1);
            Cell(2, 2).Child = generateProxyAboveBoxParentNotPresent(6);
            Cell(2, 3).Child = generateProxyBelowBoxParentNotPresent(6);

            Cell(3, 0).Child = generateProxyAboveBoxParentNotAlive(1);
            Cell(3, 1).Child = generateProxyBelowBoxParentNotAlive(1);
            Cell(3, 2).Child = generateProxyAboveBoxParentNotAlive(6);
            Cell(3, 3).Child = generateProxyBelowBoxParentNotAlive(6);
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
                RelativeSizeAxes = Axes.Both;

                bool proxyIsBelow = layerBelow is ProxyDrawable;

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
