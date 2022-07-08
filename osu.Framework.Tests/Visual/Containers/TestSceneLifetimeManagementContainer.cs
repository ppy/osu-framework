// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Performance;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Logging;
using osu.Framework.Timing;

namespace osu.Framework.Tests.Visual.Containers
{
    public class TestSceneLifetimeManagementContainer : FrameworkTestScene
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
                    Assert.AreEqual(child.ShouldBeAlive, child.IsAlive, $"Aliveness is invalid for {child}");
                }

                return num == numAlive;
            });
        }

        [Test]
        public void TestBasic()
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
        public void TestAddLoadedDrawable()
        {
            TestChild child = null;

            AddStep("add child", () => container.AddInternal(child = new TestChild(0, 2)));
            skipTo(1);
            AddStep("remove child", () => container.RemoveInternal(child));
            AddStep("add same child", () => container.AddInternal(child));
            validate(1);
        }

        [Test]
        public void TestDynamicChange()
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
        public void TestBoundaryCrossing()
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
                b.CheckCrossings(new LifetimeBoundaryCrossedEvent(b, LifetimeBoundaryKind.End, LifetimeBoundaryCrossingDirection.Forward));
                c.CheckCrossings(
                    new LifetimeBoundaryCrossedEvent(c, LifetimeBoundaryKind.Start, LifetimeBoundaryCrossingDirection.Forward),
                    new LifetimeBoundaryCrossedEvent(c, LifetimeBoundaryKind.End, LifetimeBoundaryCrossingDirection.Forward));
            });
            skipTo(1);
            AddStep("Check crossings", () =>
            {
                a.CheckCrossings();
                b.CheckCrossings();
                c.CheckCrossings(new LifetimeBoundaryCrossedEvent(c, LifetimeBoundaryKind.End, LifetimeBoundaryCrossingDirection.Backward));
            });
            skipTo(-1);
            AddStep("Check crossings", () =>
            {
                a.CheckCrossings(
                    new LifetimeBoundaryCrossedEvent(a, LifetimeBoundaryKind.End, LifetimeBoundaryCrossingDirection.Backward));
                b.CheckCrossings(new LifetimeBoundaryCrossedEvent(b, LifetimeBoundaryKind.End, LifetimeBoundaryCrossingDirection.Backward),
                    new LifetimeBoundaryCrossedEvent(b, LifetimeBoundaryKind.Start, LifetimeBoundaryCrossingDirection.Backward));
                c.CheckCrossings(new LifetimeBoundaryCrossedEvent(c, LifetimeBoundaryKind.Start, LifetimeBoundaryCrossingDirection.Backward));
            });
        }

        [Test]
        public void TestLifetimeChangeOnCallback()
        {
            AddStep("Add children", () =>
            {
                TestChild a;
                container.AddInternal(a = new TestChild(0, 1));
                container.OnCrossing += e =>
                {
                    var kind = e.Kind;
                    var direction = e.Direction;
                    if (kind == LifetimeBoundaryKind.End && direction == LifetimeBoundaryCrossingDirection.Forward)
                        a.LifetimeEnd = 2;
                    else if (kind == LifetimeBoundaryKind.Start && direction == LifetimeBoundaryCrossingDirection.Backward)
                        a.LifetimeEnd = 1;
                    else if (kind == LifetimeBoundaryKind.Start && direction == LifetimeBoundaryCrossingDirection.Forward)
                        a.LifetimeStart = a.LifetimeStart == 0 ? 1 : 0;
                };
            });
            skipTo(1);
            validate(1);
            skipTo(-1);
            validate(0);
            skipTo(0);
            validate(0);
            skipTo(1);
            validate(1);
        }

        [Test]
        public void TestLifetimeMutatingChildren()
        {
            AddStep("detach container", () => Remove(container));

            TestLifetimeMutatingChild first = null, second = null;
            AddStep("add children", () =>
            {
                container.AddInternal(first = new TestLifetimeMutatingChild(3, 5));
                container.AddInternal(second = new TestLifetimeMutatingChild(3, 5));
            });

            AddStep("process single frame when children alive", () =>
            {
                manualClock.CurrentTime = 4;
                container.UpdateSubTree();
            });
            AddAssert("both children processed", () => first.Processed && second.Processed);
        }

        [Test, Ignore("Takes too long. Unignore when you changed relevant code.")]
        public void TestFuzz()
        {
            var rng = new Random(2222);

            void randomLifetime(out double l, out double r)
            {
                l = rng.Next(5);
                r = rng.Next(5);

                if (l > r)
                    (l, r) = (r, l);

                ++r;
            }

            void checkAll() => Schedule(() =>
            {
                foreach (var child in container.InternalChildren)
                    Assert.AreEqual(child.ShouldBeAlive, child.IsAlive, $"Aliveness is invalid for {child}");
            });

            void addChild()
            {
                randomLifetime(out double l, out double r);
                container.AddInternal(new TestChild(l, r));
                checkAll();
            }

            void removeChild()
            {
                var child = container.InternalChildren[rng.Next(container.InternalChildren.Count)];
                Logger.Log($"removeChild: {child.ChildID}");
                container.RemoveInternal(child);
            }

            void changeLifetime()
            {
                var child = container.InternalChildren[rng.Next(container.InternalChildren.Count)];
                randomLifetime(out double l, out double r);
                Logger.Log($"changeLifetime: {child.ChildID}, {l}, {r}");
                child.LifetimeStart = l;
                child.LifetimeEnd = r;

                // This is called from boundary crossing events and results in timing issues if the LTMC is not updated in time. Force an update here to prevent such issues.
                container.UpdateSubTree();
                checkAll();
            }

            void changeTime()
            {
                int time = rng.Next(6);
                Logger.Log($"changeTime: {time}");
                manualClock.CurrentTime = time;
                checkAll();
            }

            AddStep("init", () =>
            {
                addChild();
                container.OnCrossing += e =>
                {
                    Logger.Log($"OnCrossing({e})");
                    changeLifetime();
                };
            });

            int count = 1;

            for (int i = 0; i < 1000; i++)
            {
                switch (rng.Next(3))
                {
                    case 0:
                        if (count < 20)
                        {
                            AddStep("Add child", addChild);
                            count += 1;
                        }
                        else
                        {
                            AddStep("Remove child", removeChild);
                            count -= 1;
                        }

                        break;

                    case 1:
                        AddStep("Change lifetime", changeLifetime);
                        break;

                    case 2:
                        AddStep("Change time", changeTime);
                        break;
                }
            }
        }

        public class TestChild : SpriteText
        {
            public override bool RemoveWhenNotAlive => false;
            public List<LifetimeBoundaryCrossedEvent> Crossings = new List<LifetimeBoundaryCrossedEvent>();
            public int StartDelta, EndDelta;

            public TestChild(double lifetimeStart, double lifetimeEnd)
            {
                LifetimeStart = lifetimeStart;
                LifetimeEnd = lifetimeEnd;
                Text = ".";
            }

            protected override void Update()
            {
                Y = ChildID * Font.Size;
                Text = $"{ChildID}: {LifetimeStart}..{LifetimeEnd} [{string.Join(", ", Crossings.Select(x => x.ToString()))}]";
            }

            public void CheckCrossings(params LifetimeBoundaryCrossedEvent[] expected)
            {
                Assert.AreEqual(expected, Crossings, $"{nameof(CheckCrossings)} for child {ChildID}");
                Crossings.Clear();
            }
        }

        public class TestLifetimeMutatingChild : TestChild
        {
            public bool Processed { get; private set; }

            public TestLifetimeMutatingChild(double lifetimeStart, double lifetimeEnd)
                : base(lifetimeStart, lifetimeEnd)
            {
            }

            protected override void Update()
            {
                base.Update();

                LifetimeEnd = LifetimeStart;
                Processed = true;
            }
        }

        public class TestContainer : LifetimeManagementContainer
        {
            public event Action<LifetimeBoundaryCrossedEvent> OnCrossing;

            protected override void OnChildLifetimeBoundaryCrossed(LifetimeBoundaryCrossedEvent e)
            {
                if (e.Child is TestChild c)
                {
                    c.Crossings.Add(e);
                    int d = e.Direction == LifetimeBoundaryCrossingDirection.Forward ? 1 : -1;
                    if (e.Kind == LifetimeBoundaryKind.Start)
                        c.StartDelta += d;
                    else
                        c.EndDelta += d;
                    Assert.IsTrue(Math.Abs(c.StartDelta) <= 1 && Math.Abs(c.EndDelta) <= 1);
                }

                OnCrossing?.Invoke(e);
            }

            public new void UpdateSubTree() => base.UpdateSubTree();
        }
    }
}
