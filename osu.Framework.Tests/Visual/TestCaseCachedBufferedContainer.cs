// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseCachedBufferedContainer : GridTestCase
    {
        public override IReadOnlyList<Type> RequiredTypes => new[]
        {
            typeof(BufferedContainer),
            typeof(BufferedContainerDrawNode),
        };

        public TestCaseCachedBufferedContainer()
            : base(5, 2)
        {
            string[] labels =
            {
                "uncached",
                "cached",
                "uncached with rotation",
                "cached with rotation",
                "uncached with movement",
                "cached with movement",
                "uncached with parent scale",
                "cached with parent scale",
                "uncached with parent scale&fade",
                "cached with parent scale&fade",
            };

            var boxes = new List<ContainingBox>();

            for (int i = 0; i < Rows * Cols; ++i)
            {
                ContainingBox box;

                Cell(i).AddRange(new Drawable[]
                {
                    new SpriteText
                    {
                        Text = labels[i],
                        TextSize = 20,
                    },
                    box = new ContainingBox(i >= 6, i >= 8)
                    {
                        Child = new CountingBox(i == 2 || i == 3, i == 4 || i == 5)
                        {
                            CacheDrawnFrameBuffer = i % 2 == 1,
                        },
                    }
                });

                boxes.Add(box);
            }

            AddWaitStep(5);

            // ensure uncached is always updating children.
            AddAssert("box 0 count > 0", () => boxes[0].Count > 0);
            AddAssert("even box counts equal", () =>
                boxes[0].Count == boxes[2].Count &&
                boxes[2].Count == boxes[4].Count &&
                boxes[4].Count == boxes[6].Count);

            // ensure cached is never updating children.
            AddAssert("box 1 count is 1", () => boxes[1].Count == 1);

            // ensure rotation changes are invalidating cache (for now).
            AddAssert("box 2 count > 0", () => boxes[2].Count > 0);
            AddAssert("box 2 count equals box 3 count", () => boxes[2].Count == boxes[3].Count);

            // ensure cached with only translation is never updating children.
            AddAssert("box 5 count is 1", () => boxes[1].Count == 1);

            // ensure a parent scaling is invalidating cache.
            AddAssert("box 5 count equals box 6 count", () => boxes[5].Count == boxes[6].Count);

            // ensure we don't break on colour invalidations (due to blanket invalidation logic in Drawable.Invalidate).
            AddAssert("box 7 count equals box 8 count", () => boxes[7].Count == boxes[8].Count);
        }

        private class ContainingBox : Container
        {
            private readonly bool scaling;
            private readonly bool fading;

            public ContainingBox(bool scaling, bool fading)
            {
                this.scaling = scaling;
                this.fading = fading;

                RelativeSizeAxes = Axes.Both;
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                if (scaling) this.ScaleTo(1.2f, 1000).Then().ScaleTo(1, 1000).Loop();
                if (fading) this.FadeTo(0.5f, 1000).Then().FadeTo(1, 1000).Loop();
            }
        }

        private class CountingBox : BufferedContainer
        {
            private readonly bool rotating;
            private readonly bool moving;
            private readonly SpriteText count;
            public new int Count;

            public CountingBox(bool rotating = false, bool moving = false)
            {
                this.rotating = rotating;
                this.moving = moving;
                RelativeSizeAxes = Axes.Both;
                Origin = Anchor.Centre;
                Anchor = Anchor.Centre;

                Scale = new Vector2(0.5f);

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Origin = Anchor.Centre,
                        Anchor = Anchor.Centre,
                        Colour = Color4.NavajoWhite,
                    },
                    count = new SpriteText
                    {
                        Colour = Color4.Black,
                        Origin = Anchor.Centre,
                        Anchor = Anchor.Centre,
                        TextSize = 80
                    }
                };
            }

            protected override void Update()
            {
                base.Update();

                if (RequiresChildrenUpdate)
                {
                    Count++;
                    count.Text = Count.ToString();
                }
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                if (rotating) this.RotateTo(360, 1000).Loop();
                if (moving) this.MoveTo(new Vector2(100, 0), 2000, Easing.InOutSine).Then().MoveTo(new Vector2(0, 0), 2000, Easing.InOutSine).Loop();
            }
        }
    }
}
