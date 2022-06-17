// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Containers
{
    public class TestSceneCachedBufferedContainer : GridTestScene
    {
        public TestSceneCachedBufferedContainer()
            : base(5, 3)
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
                "cached with no redraw on parent scale&fade",
            };

            var boxes = new List<ContainingBox>();

            for (int i = 0; i < Rows * Cols; ++i)
            {
                if (i >= labels.Length)
                    break;

                ContainingBox box;

                Cell(i).AddRange(new Drawable[]
                {
                    new SpriteText
                    {
                        Text = labels[i],
                        Font = new FontUsage(size: 20),
                    },
                    box = new ContainingBox(i >= 6, i >= 8)
                    {
                        Child = new CountingBox(i == 2 || i == 3, i == 4 || i == 5, cached: i % 2 == 1 || i == 10)
                        {
                            RedrawOnScale = i != 10
                        },
                    }
                });

                boxes.Add(box);
            }

            AddWaitStep("wait for boxes", 5);

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
            AddAssert("box 3 count is less than box 2 count", () => boxes[3].Count < boxes[2].Count);

            // ensure cached with only translation is never updating children.
            AddAssert("box 5 count is 1", () => boxes[1].Count == 1);

            // ensure a parent scaling is invalidating cache.
            AddAssert("box 5 count is less than box 6 count", () => boxes[5].Count < boxes[6].Count);

            // ensure we don't break on colour invalidations (due to blanket invalidation logic in Drawable.Invalidate).
            AddAssert("box 7 count equals box 8 count", () => boxes[7].Count == boxes[8].Count);

            AddAssert("box 10 count is 1", () => boxes[10].Count == 1);
        }

        private class ContainingBox : Container<CountingBox>
        {
            public new int Count => Child.Count;

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
            public new int Count;

            private readonly bool rotating;
            private readonly bool moving;
            private readonly SpriteText count;

            public CountingBox(bool rotating = false, bool moving = false, bool cached = false)
                : base(cachedFrameBuffer: cached)
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
                        Font = new FontUsage(size: 80),
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
