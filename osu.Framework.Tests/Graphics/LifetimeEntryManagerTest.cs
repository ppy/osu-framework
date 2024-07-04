// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics.Performance;

namespace osu.Framework.Tests.Graphics
{
    [TestFixture]
    public class LifetimeEntryManagerTest
    {
        private TestLifetimeEntryManager manager;

        [SetUp]
        public void Setup()
        {
            manager = new TestLifetimeEntryManager();
        }

        [Test]
        public void TestBasic()
        {
            manager.AddEntry(new LifetimeEntry { LifetimeStart = -1, LifetimeEnd = 1 });
            manager.AddEntry(new LifetimeEntry { LifetimeStart = 0, LifetimeEnd = 1 });
            manager.AddEntry(new LifetimeEntry { LifetimeStart = 0, LifetimeEnd = 2 });
            manager.AddEntry(new LifetimeEntry { LifetimeStart = 1, LifetimeEnd = 2 });
            manager.AddEntry(new LifetimeEntry { LifetimeStart = 2, LifetimeEnd = 2 });
            manager.AddEntry(new LifetimeEntry { LifetimeStart = 2, LifetimeEnd = 3 });

            checkCountAliveAt(0, 3);
            checkCountAliveAt(1, 2);
            checkCountAliveAt(2, 1);
            checkCountAliveAt(0, 3);
            checkCountAliveAt(3, 0);
        }

        [Test]
        public void TestRemoveAndReAdd()
        {
            var entry = new LifetimeEntry { LifetimeStart = 0, LifetimeEnd = 1 };

            manager.AddEntry(entry);
            checkCountAliveAt(0, 1);

            manager.RemoveEntry(entry);
            checkCountAliveAt(0, 0);

            manager.AddEntry(entry);
            checkCountAliveAt(0, 1);
        }

        [Test]
        public void TestDynamicChange()
        {
            manager.AddEntry(new LifetimeEntry { LifetimeStart = -1, LifetimeEnd = 0 });
            manager.AddEntry(new LifetimeEntry { LifetimeStart = 0, LifetimeEnd = 1 });
            manager.AddEntry(new LifetimeEntry { LifetimeStart = 0, LifetimeEnd = 1 });
            manager.AddEntry(new LifetimeEntry { LifetimeStart = 1, LifetimeEnd = 2 });

            checkCountAliveAt(0, 2);

            manager.Entries[0].LifetimeEnd = 1;
            manager.Entries[1].LifetimeStart = 1;
            manager.Entries[2].LifetimeEnd = 0;
            manager.Entries[3].LifetimeStart = 0;

            checkCountAliveAt(0, 2);

            foreach (var entry in manager.Entries)
            {
                entry.LifetimeEnd += 1;
                entry.LifetimeStart += 1;
            }

            checkCountAliveAt(0, 1);

            foreach (var entry in manager.Entries)
            {
                entry.LifetimeStart -= 1;
                entry.LifetimeEnd -= 1;
            }

            checkCountAliveAt(0, 2);
        }

        [Test]
        public void TestBoundaryCrossing()
        {
            manager.AddEntry(new LifetimeEntry { LifetimeStart = -1, LifetimeEnd = 0 });
            manager.AddEntry(new LifetimeEntry { LifetimeStart = 0, LifetimeEnd = 1 });
            manager.AddEntry(new LifetimeEntry { LifetimeStart = 1, LifetimeEnd = 2 });

            // No crossings for the first update.
            manager.Update(0);
            checkNoCrossing(manager.Entries[0]);
            checkNoCrossing(manager.Entries[1]);
            checkNoCrossing(manager.Entries[2]);

            manager.Update(2);
            checkNoCrossing(manager.Entries[0]);
            checkCrossing(manager.Entries[1], 0, LifetimeBoundaryKind.End, LifetimeBoundaryCrossingDirection.Forward);
            checkCrossing(manager.Entries[2], 0, LifetimeBoundaryKind.Start, LifetimeBoundaryCrossingDirection.Forward);
            checkCrossing(manager.Entries[2], 1, LifetimeBoundaryKind.End, LifetimeBoundaryCrossingDirection.Forward);

            manager.Update(1);
            checkNoCrossing(manager.Entries[0]);
            checkNoCrossing(manager.Entries[1]);
            checkCrossing(manager.Entries[2], 0, LifetimeBoundaryKind.End, LifetimeBoundaryCrossingDirection.Backward);

            manager.Update(-1);
            checkCrossing(manager.Entries[0], 0, LifetimeBoundaryKind.End, LifetimeBoundaryCrossingDirection.Backward);
            checkCrossing(manager.Entries[1], 0, LifetimeBoundaryKind.End, LifetimeBoundaryCrossingDirection.Backward);
            checkCrossing(manager.Entries[1], 1, LifetimeBoundaryKind.Start, LifetimeBoundaryCrossingDirection.Backward);
            checkCrossing(manager.Entries[2], 0, LifetimeBoundaryKind.Start, LifetimeBoundaryCrossingDirection.Backward);
        }

        [Test]
        public void TestLifetimeChangeOnCallback()
        {
            int updateTime = 0;

            manager.AddEntry(new LifetimeEntry { LifetimeStart = 0, LifetimeEnd = 1 });
            manager.EntryCrossedBoundary += (entry, kind, direction) =>
            {
                switch (kind)
                {
                    case LifetimeBoundaryKind.End when direction == LifetimeBoundaryCrossingDirection.Forward:
                        entry.LifetimeEnd = 2;
                        break;

                    case LifetimeBoundaryKind.Start when direction == LifetimeBoundaryCrossingDirection.Backward:
                        entry.LifetimeEnd = 1;
                        break;

                    case LifetimeBoundaryKind.Start when direction == LifetimeBoundaryCrossingDirection.Forward:
                        entry.LifetimeStart = entry.LifetimeStart == 0 ? 1 : 0;
                        break;
                }

                // Lifetime changes are applied in the _next_ update.
                // ReSharper disable once AccessToModifiedClosure - intentional.
                manager.Update(updateTime);
            };

            manager.Update(updateTime = 0);

            checkCountAliveAt(updateTime = 1, 1);
            checkCountAliveAt(updateTime = -1, 0);
            checkCountAliveAt(updateTime = 0, 0);
            checkCountAliveAt(updateTime = 1, 1);
        }

        [Test]
        public void TestUpdateWithTimeRange()
        {
            manager.AddEntry(new LifetimeEntry { LifetimeStart = -1, LifetimeEnd = 1 });
            manager.AddEntry(new LifetimeEntry { LifetimeStart = 0, LifetimeEnd = 1 });
            manager.AddEntry(new LifetimeEntry { LifetimeStart = 0, LifetimeEnd = 2 });
            manager.AddEntry(new LifetimeEntry { LifetimeStart = 1, LifetimeEnd = 2 });
            manager.AddEntry(new LifetimeEntry { LifetimeStart = 2, LifetimeEnd = 2 });
            manager.AddEntry(new LifetimeEntry { LifetimeStart = 2, LifetimeEnd = 3 });

            checkCountAliveAt(-3, -2, 0);
            checkCountAliveAt(-3, -1, 1);

            checkCountAliveAt(-2, 4, 6);
            checkCountAliveAt(-1, 4, 6);
            checkCountAliveAt(0, 4, 6);
            checkCountAliveAt(1, 4, 4);
            checkCountAliveAt(2, 4, 1);
            checkCountAliveAt(3, 4, 0);
            checkCountAliveAt(4, 4, 0);
        }

        [Test]
        public void TestRemoveFutureAfterLifetimeChange()
        {
            manager.AddEntry(new LifetimeEntry { LifetimeStart = 1, LifetimeEnd = 2 });
            checkCountAliveAt(0, 0);

            manager.Entries[0].LifetimeEnd = 3;
            manager.RemoveEntry(manager.Entries[0]);

            checkCountAliveAt(1, 0);
        }

        [Test]
        public void TestRemovePastAfterLifetimeChange()
        {
            manager.AddEntry(new LifetimeEntry { LifetimeStart = -2, LifetimeEnd = -1 });
            checkCountAliveAt(0, 0);

            manager.Entries[0].LifetimeStart = -3;
            manager.RemoveEntry(manager.Entries[0]);

            checkCountAliveAt(-2, 0);
        }

        [Test]
        public void TestFuzz()
        {
            var rng = new Random(2222);
            int currentTime = 0;

            addEntry();

            manager.EntryCrossedBoundary += (_, _, _) => changeLifetime();
            manager.Update(0);

            int count = 1;

            for (int i = 0; i < 1000; i++)
            {
                switch (rng.Next(3))
                {
                    case 0:
                        if (count < 20)
                        {
                            addEntry();
                            count += 1;
                        }
                        else
                        {
                            removeEntry();
                            count -= 1;
                        }

                        break;

                    case 1:
                        changeLifetime();
                        break;

                    case 2:
                        changeTime();
                        break;
                }
            }

            void randomLifetime(out double l, out double r)
            {
                l = rng.Next(5);
                r = rng.Next(5);

                if (l > r)
                    (l, r) = (r, l);

                ++r;
            }

            // ReSharper disable once AccessToModifiedClosure - intentional.
            void checkAll() => checkAlivenessAt(currentTime);

            void addEntry()
            {
                randomLifetime(out double l, out double r);
                manager.AddEntry(new LifetimeEntry { LifetimeStart = l, LifetimeEnd = r });
                checkAll();
            }

            void removeEntry()
            {
                var entry = manager.Entries[rng.Next(manager.Entries.Count)];
                manager.RemoveEntry(entry);
                checkAll();
            }

            void changeLifetime()
            {
                var entry = manager.Entries[rng.Next(manager.Entries.Count)];
                randomLifetime(out double l, out double r);
                entry.LifetimeStart = l;
                entry.LifetimeEnd = r;
                checkAll();
            }

            void changeTime()
            {
                int time = rng.Next(6);
                currentTime = time;
                checkAll();
            }
        }

        private void checkCountAliveAt(int time, int expectedCount) => checkCountAliveAt(time, time, expectedCount);

        private void checkCountAliveAt(int startTime, int endTime, int expectedCount)
        {
            checkAlivenessAt(startTime, endTime);
            Assert.That(manager.Entries.Count(entry => entry.State == LifetimeEntryState.Current), Is.EqualTo(expectedCount));
        }

        private void checkAlivenessAt(int time) => checkAlivenessAt(time, time);

        private void checkAlivenessAt(int startTime, int endTime)
        {
            manager.Update(startTime, endTime);

            for (int i = 0; i < manager.Entries.Count; i++)
            {
                var entry = manager.Entries[i];
                bool isAlive = entry.State == LifetimeEntryState.Current;
                bool shouldBeAlive = endTime >= entry.LifetimeStart && startTime < entry.LifetimeEnd;

                Assert.That(isAlive, Is.EqualTo(shouldBeAlive), $"Aliveness is invalid for entry {i}");
            }
        }

        private void checkNoCrossing(LifetimeEntry entry) => Assert.That(manager.Crossings, Does.Not.Contain(entry));

        private void checkCrossing(LifetimeEntry entry, int index, LifetimeBoundaryKind kind, LifetimeBoundaryCrossingDirection direction)
            => Assert.That(manager.Crossings[entry][index], Is.EqualTo((kind, direction)));

        private class TestLifetimeEntryManager : LifetimeEntryManager
        {
            public IReadOnlyList<LifetimeEntry> Entries => entries;

            private readonly List<LifetimeEntry> entries = new List<LifetimeEntry>();

            public IReadOnlyDictionary<LifetimeEntry, List<(LifetimeBoundaryKind kind, LifetimeBoundaryCrossingDirection direction)>> Crossings => crossings;

            private readonly Dictionary<LifetimeEntry, List<(LifetimeBoundaryKind kind, LifetimeBoundaryCrossingDirection direction)>> crossings =
                new Dictionary<LifetimeEntry, List<(LifetimeBoundaryKind kind, LifetimeBoundaryCrossingDirection direction)>>();

            public TestLifetimeEntryManager()
            {
                EntryCrossedBoundary += (entry, kind, direction) =>
                {
                    if (!crossings.ContainsKey(entry))
                        crossings[entry] = new List<(LifetimeBoundaryKind kind, LifetimeBoundaryCrossingDirection direction)>();
                    crossings[entry].Add((kind, direction));
                };
            }

            public new void AddEntry(LifetimeEntry entry)
            {
                entries.Add(entry);
                base.AddEntry(entry);
            }

            public new bool RemoveEntry(LifetimeEntry entry)
            {
                if (base.RemoveEntry(entry))
                {
                    entries.Remove(entry);
                    return true;
                }

                return false;
            }

            public new void ClearEntries()
            {
                entries.Clear();
                base.ClearEntries();
            }

            public new bool Update(double time)
            {
                crossings.Clear();
                return base.Update(time);
            }
        }
    }
}
