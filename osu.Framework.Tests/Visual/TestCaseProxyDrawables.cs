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
            : base(4, 2)
        {
            Cell(0, 0).Child = generateProxyAboveAllPresentTest();
            Cell(0, 1).Child = generateProxyBelowAllPresentTest();

            Cell(1, 0).Child = generateProxyAboveOriginalMaskedAway();
            Cell(1, 1).Child = generateProxyBelowOriginalMaskedAway();

            Cell(2, 0).Child = generateProxyAboveBoxParentNotPresent();
            Cell(2, 1).Child = generateProxyBelowBoxParentNotPresent();

            Cell(3, 0).Child = generateProxyAboveBoxParentNotAlive();
            Cell(3, 1).Child = generateProxyBelowBoxParentNotAlive();
        }

        private Drawable generateProxyAboveAllPresentTest()
        {
            var box = new Box
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(50),
            };

            return new Visualiser("Proxy above, all present", box, box.CreateProxy());
        }

        private Drawable generateProxyBelowAllPresentTest()
        {
            var box = new Box
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(50),
            };

            return new Visualiser("Proxy below, all present", box.CreateProxy(), box);
        }

        private Drawable generateProxyAboveOriginalMaskedAway()
        {
            var box = new Box
            {
                RelativeSizeAxes = Axes.Both,
                Position = new Vector2(60)
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

            return new Visualiser("Proxy above, original masked away", boxMaskingContainer, box.CreateProxy());
        }

        private Drawable generateProxyBelowOriginalMaskedAway()
        {
            var box = new Box
            {
                RelativeSizeAxes = Axes.Both,
                Position = new Vector2(60)
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

            return new Visualiser("Proxy below, original masked away", box.CreateProxy(), boxMaskingContainer);
        }

        private Drawable generateProxyAboveBoxParentNotPresent()
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

            return new Visualiser("Proxy above, original parent invisible", invisibleContainer, box.CreateProxy());
        }

        private Drawable generateProxyBelowBoxParentNotPresent()
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

            return new Visualiser("Proxy below, original parent invisible", box.CreateProxy(), invisibleContainer);
        }

        private Drawable generateProxyAboveBoxParentNotAlive()
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

            return new Visualiser("Proxy above, original parent not alive", invisibleContainer, box.CreateProxy());
        }

        private Drawable generateProxyBelowBoxParentNotAlive()
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

            return new Visualiser("Proxy below, original parent not alive", box.CreateProxy(), invisibleContainer);
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
