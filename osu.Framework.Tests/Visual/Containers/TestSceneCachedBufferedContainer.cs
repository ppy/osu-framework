// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
    public partial class TestSceneCachedBufferedContainer : GridTestScene
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
                "cached with drawnode invalidation",
                "cached with force redraw",
                "cached with padding",
                "cached with new child",
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
                    box = new ContainingBox(i >= 6 && i <= 10, i >= 8 && i <= 10)
                    {
                        Child = new CountingBox(
                            rotating: i == 2 || i == 3,
                            moving: i == 4 || i == 5,
                            invalidate: i == 11,
                            forceRedraw: i == 12,
                            padding: i == 13,
                            newChild: i == 14,
                            cached: i % 2 == 1 || i >= 10)
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
                boxes[4].Count == boxes[6].Count &&
                boxes[6].Count == boxes[8].Count);

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

            // ensure drawnode invalidation doesn't invalidate cache.
            AddAssert("box 11 count is 1", () => boxes[11].Count == 1);

            // ensure force redraw always invalidates cache.
            AddAssert("box 12 count equals box 0 count", () => boxes[12].Count == boxes[0].Count);

            // ensure changing padding invalidates cache.
            AddAssert("box 13 count equals box 0 count", () => boxes[13].Count == boxes[0].Count);

            // ensure adding a new child invalidates cache.
            AddAssert("box 14 count equals box 0 count", () => boxes[14].Count == boxes[0].Count);
        }

        private partial class ContainingBox : Container<CountingBox>
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

        private partial class CountingBox : BufferedContainer
        {
            public new int Count;

            private readonly bool rotating;
            private readonly bool moving;
            private readonly bool invalidate;
            private readonly bool forceRedraw;
            private readonly bool padding;
            private readonly bool newChild;
            private readonly SpriteText count;

            private Drawable child = null;

            public CountingBox(
                bool rotating = false,
                bool moving = false,
                bool invalidate = false,
                bool forceRedraw = false,
                bool padding = false,
                bool newChild = false,
                bool cached = false
            )
                : base(cachedFrameBuffer: cached)
            {
                this.rotating = rotating;
                this.moving = moving;
                this.invalidate = invalidate;
                this.forceRedraw = forceRedraw;
                this.padding = padding;
                this.newChild = newChild;
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

                if (newChild)
                {
                    if (child != null)
                        RemoveInternal(child, true);

                    child = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Origin = Anchor.Centre,
                        Anchor = Anchor.Centre,
                        Colour = Color4.Red,
                        Alpha = 0.5f,
                    };

                    AddInternal(child);
                }

                if (invalidate)
                {
                    Invalidate(Invalidation.DrawNode);
                }

                if (forceRedraw)
                {
                    ForceRedraw();
                }

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
                if (padding) this.TransformTo(nameof(Padding), new MarginPadding(10), 200).Then().TransformTo(nameof(Padding), new MarginPadding(0), 200).Loop();
            }
        }
    }
}
