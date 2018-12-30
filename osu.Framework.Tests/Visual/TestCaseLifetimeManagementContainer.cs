// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osu.Framework.Timing;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseLifetimeManagementContainer : TestCase
    {
        private ManualClock manualClock;
        private LifetimeManagementContainer container;

        [SetUp]
        public void SetUp() => Schedule(() =>
        {
            manualClock = new ManualClock();

            Children = new Drawable[]
            {
                container = new TestContainer
                {
                    Clock = new FramedClock(manualClock),
                },
            };
        });

        private void skipTo(double time)
        {
            AddStep($"Set time to {time}", () => manualClock.CurrentTime = time);
        }

        private void validate(int numAlive)
        {
            AddAssert($"{numAlive} alive children", () =>
            {
                int num = 0;
                foreach (var child in container.InternalChildren)
                {
                    num += child.IsAlive ? 1 : 0;
                    Assert.AreEqual(child.IsAlive, child.ShouldBeAlive, $"Aliveness is invalid for {child}");
                }

                return num == numAlive;
            });
        }

        [Test]
        public void Basic()
        {
            AddStep("Add children", () =>
            {
                container.AddInternal(new TestChild(-1, 1));
                container.AddInternal(new TestChild(0, 1));
                container.AddInternal(new TestChild(0, 2));
                container.AddInternal(new TestChild(1, 2));
                container.AddInternal(new TestChild(2, 2));
                container.AddInternal(new TestChild(2, 3));
            });
            validate(3);
            skipTo(1);
            validate(2);
            skipTo(2);
            validate(1);
            skipTo(0);
            validate(3);
            skipTo(3);
            validate(0);
        }

        [Test]
        public void DynamicChange()
        {
            TestChild a = null, b = null, c = null, d = null;
            AddStep("Add children", () =>
            {
                container.AddInternal(a = new TestChild(-1, 0));
                container.AddInternal(b = new TestChild(0, 1));
                container.AddInternal(c = new TestChild(0, 1));
                container.AddInternal(d = new TestChild(1, 2));
            });
            validate(2);
            AddStep("Change lifetime", () =>
            {
                a.LifetimeEnd = 1;
                b.LifetimeStart = 1;
                c.LifetimeEnd = 0;
                d.LifetimeStart = 0;
            });
            validate(2);
            AddStep("Change lifetime", () =>
            {
                foreach (var x in new[] { a, b, c, d })
                {
                    x.LifetimeStart += 1;
                    x.LifetimeEnd += 1;
                }
            });
            validate(1);
            AddStep("Change lifetime", () =>
            {
                foreach (var x in new[] { a, b, c, d })
                {
                    x.LifetimeStart -= 1;
                    x.LifetimeEnd -= 1;
                }
            });
            validate(2);
        }

        [Test]
        public void Skip()
        {
            TestChild a = null, b = null, c = null;
            AddStep("Add children", () =>
            {
                container.AddInternal(a = new TestChild(-1, 0));
                container.AddInternal(b = new TestChild(0, 1));
                container.AddInternal(c = new TestChild(1, 2));
            });
            skipTo(2);
            AddAssert("Check skipped", () =>
                a.Skipped == null &&
                b.Skipped == null &&
                c.Skipped == LifetimeManagementContainer.SkipDirection.Forward);
            skipTo(1);
            skipTo(-1);
            AddAssert("Check skipped", () =>
                a.Skipped == null &&
                b.Skipped == LifetimeManagementContainer.SkipDirection.Backward &&
                c.Skipped == null);
        }

        public class TestChild : SpriteText
        {
            public override bool RemoveWhenNotAlive => false;

            public LifetimeManagementContainer.SkipDirection? Skipped;

            public TestChild(double lifetimeStart, double lifetimeEnd)
            {
                LifetimeStart = lifetimeStart;
                LifetimeEnd = lifetimeEnd;
                Text = ".";
            }

            protected override void Update()
            {
                Y = ChildID * TextSize;
                Text = $"{ChildID}: {LifetimeStart}..{LifetimeEnd}";
                Skipped = null;
            }
        }

        public class TestContainer : LifetimeManagementContainer
        {
            protected override void OnChildLifetimeSkipped(Drawable child, SkipDirection skipDirection)
            {
                if (child is TestChild c)
                {
                    c.Skipped = skipDirection;
                }
            }
        }
    }
}
