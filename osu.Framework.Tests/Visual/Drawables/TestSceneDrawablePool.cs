// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Pooling;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Drawables
{
    public class TestSceneDrawablePool : TestScene
    {
        private DrawablePool<TestDrawable> pool;
        private SpriteText count;

        private readonly HashSet<TestDrawable> consumed = new HashSet<TestDrawable>();

        [Test]
        public void TestPoolInitialDrawableLoadedAheadOfTime()
        {
            const int pool_size = 3;
            resetWithNewPool(() => new TestPool(TimePerAction, pool_size));

            for (int i = 0; i < 3; i++)
                AddAssert("check drawable is in ready state", () => pool.Get().LoadState == LoadState.Ready);

            AddAssert("check drawable is not in ready state", () => pool.Get().LoadState < LoadState.Ready);
        }

        [Test]
        public void TestPoolUsageWithinLimits()
        {
            const int pool_size = 10;

            resetWithNewPool(() => new TestPool(TimePerAction, pool_size));

            AddRepeatStep("get new pooled drawable", () => consumeDrawable(), 50);

            AddUntilStep("all returned to pool", () => pool.CountAvailable == pool_size);

            AddAssert("consumed drawables report returned to pool", () => consumed.All(d => d.IsInPool));
            AddAssert("consumed drawables not disposed", () => consumed.All(d => !d.IsDisposed));

            AddAssert("consumed less than pool size", () => consumed.Count < pool_size);
        }

        [Test]
        public void TestPoolUsageExceedsLimits()
        {
            const int pool_size = 10;

            resetWithNewPool(() => new TestPool(TimePerAction * 20, pool_size));

            AddRepeatStep("get new pooled drawable", () => consumeDrawable(), 50);

            AddUntilStep("all returned to pool", () => pool.CountAvailable == consumed.Count);

            AddAssert("pool grew in size", () => pool.CountAvailable > pool_size);

            AddAssert("consumed drawables report returned to pool", () => consumed.All(d => d.IsInPool));
            AddAssert("consumed drawables not disposed", () => consumed.All(d => !d.IsDisposed));
        }

        [TestCase(10)]
        [TestCase(20)]
        public void TestPoolInitialSize(int initialPoolSize)
        {
            resetWithNewPool(() => new TestPool(TimePerAction * 20, initialPoolSize));

            AddUntilStep("available count is correct", () => pool.CountAvailable == initialPoolSize);
        }

        [Test]
        public void TestReturnWithoutAdding()
        {
            resetWithNewPool(() => new TestPool(TimePerAction, 1));

            TestDrawable drawable = null;

            AddStep("consume without adding", () => drawable = pool.Get());

            AddStep("manually return", () => drawable.Return());

            AddUntilStep("free was run", () => drawable.FreedCount == 1);
            AddUntilStep("was returned", () => pool.CountAvailable == 1);

            AddAssert("manually return twice throws", () =>
            {
                try
                {
                    drawable.Return();
                    return false;
                }
                catch (InvalidOperationException)
                {
                    return true;
                }
            });
        }

        [Test]
        public void TestPrepareAndFreeMethods()
        {
            resetWithNewPool(() => new TestPool(TimePerAction, 1));

            TestDrawable drawable = null;
            TestDrawable drawable2 = null;

            AddStep("consume item", () => drawable = (TestDrawable)consumeDrawable());

            AddAssert("prepare was run", () => drawable.PreparedCount == 1);
            AddUntilStep("free was run", () => drawable.FreedCount == 1);

            AddStep("consume item", () => drawable2 = (TestDrawable)consumeDrawable());

            AddAssert("is same item", () => ReferenceEquals(drawable, drawable2));

            AddAssert("prepare was run", () => drawable2.PreparedCount == 2);
            AddUntilStep("free was run", () => drawable2.FreedCount == 2);
        }

        [Test]
        public void TestUsePoolableDrawableWithoutPool()
        {
            TestDrawable drawable = null;

            AddStep("consume item", () => Add(drawable = new TestDrawable()));

            AddAssert("prepare was run", () => drawable.PreparedCount == 1);
            AddUntilStep("free was run", () => drawable.FreedCount == 1);

            AddUntilStep("drawable was disposed", () => drawable.IsDisposed);
        }

        [TestCase(10)]
        [TestCase(20)]
        public void TestPoolUsageExceedsMaximum(int maxPoolSize)
        {
            resetWithNewPool(() => new TestPool(TimePerAction * 20, 10, maxPoolSize));

            AddRepeatStep("get new pooled drawable", () => consumeDrawable(), 50);

            AddUntilStep("pool size hit maximum", () => pool.CountAvailable == maxPoolSize);
            AddUntilStep("count in pool is correct", () => consumed.Count(d => d.IsInPool) == maxPoolSize);
            AddAssert("non-returned drawables disposed", () => consumed.Where(d => !d.IsInPool).All(d => d.IsDisposed));
        }

        protected override void Update()
        {
            base.Update();
            if (count != null)
                count.Text = $"available: {pool.CountAvailable} consumed: {consumed.Count} disposed: {consumed.Count(d => d.IsDisposed)}";
        }

        private static int displayCount;

        private PoolableDrawable consumeDrawable()
        {
            var drawable = pool.Get(d =>
            {
                d.Position = new Vector2(RNG.NextSingle(), RNG.NextSingle());
                d.DisplayString = (++displayCount).ToString();
            });

            consumed.Add(drawable);
            Add(drawable);

            return drawable;
        }

        private void resetWithNewPool(Func<DrawablePool<TestDrawable>> createPool)
        {
            AddStep("reset stats", () => consumed.Clear());

            AddStep("create pool", () =>
            {
                pool = createPool();

                Children = new Drawable[]

                {
                    pool,
                    count = new SpriteText(),
                };
            });
        }

        private class TestPool : DrawablePool<TestDrawable>
        {
            private readonly double fadeTime;

            public TestPool(double fadeTime, int initialSize, int? maximumSize = null)
                : base(initialSize, maximumSize)
            {
                this.fadeTime = fadeTime;
            }

            protected override TestDrawable CreateNewDrawable()
            {
                return new TestDrawable(fadeTime);
            }
        }

        private class TestDrawable : PoolableDrawable
        {
            private readonly double fadeTime;

            private readonly SpriteText text;

            public string DisplayString
            {
                set => text.Text = value;
            }

            public TestDrawable()
                : this(1000)
            {
            }

            public TestDrawable(double fadeTime)
            {
                this.fadeTime = fadeTime;

                RelativePositionAxes = Axes.Both;
                Size = new Vector2(50);
                Origin = Anchor.Centre;

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        Colour = Color4.Green,
                        RelativeSizeAxes = Axes.Both,
                    },
                    text = new SpriteText
                    {
                        Text = "-",
                        Font = FontUsage.Default.With(size: 40),
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                    },
                };
            }

            public new bool IsDisposed => base.IsDisposed;

            public int PreparedCount { get; private set; }
            public int FreedCount { get; private set; }

            protected override void PrepareForUse()
            {
                this.FadeOutFromOne(fadeTime);
                this.RotateTo(0).RotateTo(80, fadeTime);

                Expire();

                PreparedCount++;
            }

            protected override void FreeAfterUse()
            {
                base.FreeAfterUse();
                FreedCount++;
            }
        }
    }
}
