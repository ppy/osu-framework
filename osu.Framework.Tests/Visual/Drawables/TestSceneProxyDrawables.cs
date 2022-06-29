// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Drawables
{
    public class TestSceneProxyDrawables : FrameworkTestScene
    {
        public TestSceneProxyDrawables()
        {
            Child = new BasicScrollContainer
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
                        generateOpaqueProxyAboveOpaqueBox(),
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

            return new Visualiser($"{proxyCount} proxy(s) above, all present")
            {
                Children = new Drawable[] { box, new ProxyVisualiser(proxy, true) }
            };
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

            return new Visualiser($"{proxyCount} proxy(s) below, all present")
            {
                Children = new Drawable[] { new ProxyVisualiser(proxy, false), box }
            };
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

            return new Visualiser($"{proxyCount} proxy(s) above, original masked away")
            {
                Children = new Drawable[] { boxMaskingContainer, new ProxyVisualiser(proxy, true) }
            };
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

            return new Visualiser($"{proxyCount} proxy(s) below, original masked away")
            {
                Children = new Drawable[] { new ProxyVisualiser(proxy, false), boxMaskingContainer }
            };
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

            return new Visualiser($"{proxyCount} proxy(s) above, original parent invisible")
            {
                Children = new Drawable[] { invisibleContainer, new ProxyVisualiser(proxy, true) }
            };
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

            return new Visualiser($"{proxyCount} proxy(s) below, original parent invisible")
            {
                Children = new Drawable[] { new ProxyVisualiser(proxy, false), invisibleContainer }
            };
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

            return new Visualiser($"{proxyCount} proxy(s) above, original parent not alive")
            {
                Children = new Drawable[] { invisibleContainer, new ProxyVisualiser(proxy, true) }
            };
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

            return new Visualiser($"{proxyCount} proxy(s) below, original parent not alive")
            {
                Children = new Drawable[] { new ProxyVisualiser(proxy, false), invisibleContainer }
            };
        }

        private Drawable generateProxyAboveParentOriginalIndirectlyMaskedAway(int proxyCount)
        {
            var box = new Box
            {
                Size = new Vector2(50),
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Position = new Vector2(-50)
            };

            var proxy = box.CreateProxy();
            for (int i = 1; i < proxyCount; i++)
                proxy = proxy.CreateProxy();

            return new Visualiser($"{proxyCount} proxy(s) above, proxy masked")
            {
                Children = new Drawable[]
                {
                    box,
                    new Container
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Size = new Vector2(100),
                        Masking = true,
                        CornerRadius = 20,
                        Children = new Drawable[]
                        {
                            new Box { RelativeSizeAxes = Axes.Both, Colour = Color4.Yellow.Opacity(0.2f) },
                            new ProxyVisualiser(proxy, true)
                        }
                    }
                }
            };
        }

        private Drawable generateProxyBelowParentOriginalIndirectlyMaskedAway(int proxyCount)
        {
            var box = new Box
            {
                Size = new Vector2(50),
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Position = new Vector2(-50)
            };

            var proxy = box.CreateProxy();
            for (int i = 1; i < proxyCount; i++)
                proxy = proxy.CreateProxy();

            return new Visualiser($"{proxyCount} proxy(s) below, proxy masked")
            {
                Children = new Drawable[]
                {
                    new Container
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Size = new Vector2(100),
                        Masking = true,
                        CornerRadius = 20,
                        Children = new Drawable[]
                        {
                            new Box { RelativeSizeAxes = Axes.Both, Colour = Color4.Yellow.Opacity(0.2f) },
                            new ProxyVisualiser(proxy, false)
                        }
                    },
                    box
                }
            };
        }

        private Drawable generateOpaqueProxyAboveOpaqueBox()
        {
            var box = new Box
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(50),
            };

            var proxy = box.CreateProxy();

            return new Visualiser("proxy above opaque box")
            {
                Children = new Drawable[]
                {
                    box,
                    new ProxyVisualiser(proxy, false, 1.0f)
                }
            };
        }

        private class NonPresentContainer : Container
        {
            private bool isPresent = true;
            public override bool IsPresent => isPresent;

            public override bool UpdateSubTree()
            {
                // We want to be present for updates
                isPresent = true;

                bool result = base.UpdateSubTree();
                if (!result)
                    return false;

                // We want to not be present for draw
                isPresent = false;
                return true;
            }
        }

        private class NonAliveContainer : Container
        {
            protected internal override bool ShouldBeAlive => false;
            public override bool DisposeOnDeathRemoval => false;
        }

        private class Visualiser : Container
        {
            protected override Container<Drawable> Content => content;
            private readonly Container content;

            public Visualiser(string description)
            {
                Size = new Vector2(300);

                InternalChildren = new Drawable[]
                {
                    content = new Container { RelativeSizeAxes = Axes.Both },
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
                };
            }
        }

        private class ProxyVisualiser : CompositeDrawable
        {
            private readonly Drawable original;
            private readonly Drawable overlay;

            public ProxyVisualiser(Drawable proxy, bool proxyIsBelow, float boxAlpha = 0.5f)
            {
                RelativeSizeAxes = Axes.Both;

                original = proxy.Original;

                while (original != (original = original.Original))
                {
                }

                if (proxyIsBelow)
                    AddInternal(proxy);

                AddInternal(overlay = new Container
                {
                    Colour = proxyIsBelow ? Color4.Red : Color4.Green,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Alpha = boxAlpha,
                        },
                        new SpriteText
                        {
                            Anchor = Anchor.BottomCentre,
                            Origin = Anchor.BottomCentre,
                            Colour = Color4.Black,
                            Text = "proxy"
                        }
                    }
                });

                if (!proxyIsBelow)
                    AddInternal(proxy);
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
