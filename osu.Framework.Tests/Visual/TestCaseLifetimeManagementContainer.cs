// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
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
        private TestContainer container;

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
        public void BoundaryCrossing()
        {
            TestChild a = null, b = null, c = null;
            AddStep("Add children", () =>
            {
                container.AddInternal(a = new TestChild(-1, 0));
                container.AddInternal(b = new TestChild(0, 1));
                container.AddInternal(c = new TestChild(1, 2));
            });
            skipTo(2);
            AddStep("Check crossings", () =>
            {
                a.CheckCrossings();
                b.CheckCrossings(new LifetimeBoundaryCrossing(LifetimeBoundaryKind.End, LifetimeBoundaryCrossingDirection.Forward));
                c.CheckCrossings(
                    new LifetimeBoundaryCrossing(LifetimeBoundaryKind.Start, LifetimeBoundaryCrossingDirection.Forward),
                    new LifetimeBoundaryCrossing(LifetimeBoundaryKind.End, LifetimeBoundaryCrossingDirection.Forward));
            });
            skipTo(1);
            AddStep("Check crossings", () =>
            {
                a.CheckCrossings();
                b.CheckCrossings();
                c.CheckCrossings(new LifetimeBoundaryCrossing(LifetimeBoundaryKind.End, LifetimeBoundaryCrossingDirection.Backward));
            });
            skipTo(-1);
            AddStep("Check crossings", () =>
            {
                a.CheckCrossings(
                    new LifetimeBoundaryCrossing(LifetimeBoundaryKind.End, LifetimeBoundaryCrossingDirection.Backward));
                b.CheckCrossings(new LifetimeBoundaryCrossing(LifetimeBoundaryKind.End, LifetimeBoundaryCrossingDirection.Backward),
                    new LifetimeBoundaryCrossing(LifetimeBoundaryKind.Start, LifetimeBoundaryCrossingDirection.Backward));
                c.CheckCrossings(new LifetimeBoundaryCrossing(LifetimeBoundaryKind.Start, LifetimeBoundaryCrossingDirection.Backward));
            });
        }

        public class TestChild : SpriteText
        {
            public override bool RemoveWhenNotAlive => false;
            public List<LifetimeBoundaryCrossing> Crossings = new List<LifetimeBoundaryCrossing>();

            public TestChild(double lifetimeStart, double lifetimeEnd)
            {
                LifetimeStart = lifetimeStart;
                LifetimeEnd = lifetimeEnd;
                Text = ".";
            }

            protected override void Update()
            {
                Y = ChildID * TextSize;
                Text = $"{ChildID}: {LifetimeStart}..{LifetimeEnd} [{string.Join(", ", Crossings.Select(x => x.ToString()))}]";
            }

            public void CheckCrossings(params LifetimeBoundaryCrossing[] expected)
            {
                Assert.AreEqual(expected, Crossings, $"{nameof(CheckCrossings)} for child {ChildID}");
                Crossings.Clear();
            }
        }

        public struct LifetimeBoundaryCrossing
        {
            public readonly LifetimeBoundaryKind Kind;
            public readonly LifetimeBoundaryCrossingDirection Direction;

            public LifetimeBoundaryCrossing(LifetimeBoundaryKind kind, LifetimeBoundaryCrossingDirection direction)
            {
                Kind = kind;
                Direction = direction;
            }

            public override string ToString() => $"({Kind}, {Direction})";
        }

        public class TestContainer : LifetimeManagementContainer
        {
            protected override void OnChildLifetimeBoundaryCrossed(Drawable child, LifetimeBoundaryKind kind, LifetimeBoundaryCrossingDirection direction)
            {
                ((TestChild)child).Crossings.Add(new LifetimeBoundaryCrossing(kind, direction));
            }
        }
    }
}
