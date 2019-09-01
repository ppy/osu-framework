// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;

namespace osu.Framework.Tests.Visual.Drawables
{
    public class TestSceneProxyDrawableLifetime : FrameworkTestScene
    {
        [SetUp]
        public void Setup() => Schedule(Clear);

        [Test]
        public void TestProxyAliveWhenOriginalAlive()
        {
            Box box = null;
            Drawable proxy = null;

            Steps.AddStep("add proxy", () =>
            {
                Add(box = new Box
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(100)
                });

                Add(proxy = box.CreateProxy());
            });

            Steps.AddAssert("proxy should be alive", () => proxy.ShouldBeAlive);
            Steps.AddAssert("proxy is alive", () => proxy.IsAlive);

            Steps.AddStep("expire box", () => box.Expire());

            Steps.AddAssert("proxy should not be alive", () => !proxy.ShouldBeAlive);
            Steps.AddAssert("proxy is not alive", () => !proxy.IsAlive);
        }

        [Test]
        public void TestLifetimeTransferred()
        {
            Box box = null;
            Drawable proxy = null;
            bool lifetimeChanged = false;

            Steps.AddStep("add proxy", () =>
            {
                lifetimeChanged = false;

                Add(box = new Box
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(100)
                });

                Add(proxy = box.CreateProxy().With(d => d.LifetimeChanged += _ => lifetimeChanged = true));
            });

            Steps.AddStep("set lifetimes", () =>
            {
                box.LifetimeStart = Time.Current - 5000;
                box.LifetimeEnd = Time.Current + 5000;
            });

            Steps.AddAssert("lifetime changed", () => lifetimeChanged);
            Steps.AddAssert("lifetime transferred from box", () => proxy.LifetimeStart == box.LifetimeStart && proxy.LifetimeEnd == box.LifetimeEnd);
        }

        [Test]
        public void TestRemovedWhenOriginalRemoved()
        {
            Container container = null;
            Box box = null;
            Drawable proxy = null;

            Steps.AddStep("add proxy", () =>
            {
                Add(container = new Container
                {
                    RelativeSizeAxes = Axes.Both
                });

                container.Add(box = new Box
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(100)
                });

                container.Add(proxy = box.CreateProxy());
            });

            Steps.AddStep("expire box", () => box.Expire(true));

            Steps.AddAssert("box removed", () => !container.Contains(box));
            Steps.AddAssert("proxy removed", () => !container.Contains(proxy));
        }
    }
}
