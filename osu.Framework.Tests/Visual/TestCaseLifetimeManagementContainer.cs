// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Linq;
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
                container = new LifetimeManagementContainer
                {
                    Clock = new FramedClock(manualClock),
                },
            };
        });

        private void skipTo(double time)
        {
            AddStep($"set time to {time}", () => manualClock.CurrentTime = time);
        }

        private void validate(int numAlive)
        {
            AddAssert($"{numAlive} alive children", () =>
            {
                int num = 0;
                foreach (var child in container.InternalChildren)
                {
                    var isAlive = container.AliveInternalChildren.Any(c => c == child);
                    num += isAlive ? 1 : 0;
                    Assert.AreEqual(isAlive, child.ShouldBeAlive);
                }

                return num == numAlive;
            });
        }

        [Test]
        public void Basic()
        {
            AddStep("add children", () =>
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
            AddStep("add children", () =>
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

        public class TestChild : Container
        {
            public override bool RemoveWhenNotAlive => false;

            private readonly SpriteText text;

            public TestChild(double lifetimeStart, double lifetimeEnd)
            {
                LifetimeStart = lifetimeStart;
                LifetimeEnd = lifetimeEnd;
                Add(text = new SpriteText());
            }

            protected override void Update()
            {
                text.Y = ChildID * text.TextSize;
                text.Text = $"{ChildID}: {LifetimeStart}..{LifetimeEnd}";
            }
        }
    }
}
