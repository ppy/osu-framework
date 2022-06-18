// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics.Containers;
using osu.Framework.Statistics;

namespace osu.Framework.Graphics.Performance
{
    /// <summary>
    /// Provides time-optimised updates for lifetime change notifications.
    /// This is used in specialised <see cref="CompositeDrawable"/>s to optimise lifetime changes (see: <see cref="LifetimeManagementContainer"/>).
    /// </summary>
    /// <remarks>
    /// The time complexity of updating lifetimes is O(number of alive items).
    /// </remarks>
    public class LifetimeEntryManager
    {
        /// <summary>
        /// Invoked immediately when a <see cref="LifetimeEntry"/> becomes alive.
        /// </summary>
        public event Action<LifetimeEntry> EntryBecameAlive;

        /// <summary>
        /// Invoked immediately when a <see cref="LifetimeEntry"/> becomes dead.
        /// </summary>
        public event Action<LifetimeEntry> EntryBecameDead;

        /// <summary>
        /// Invoked when a <see cref="LifetimeEntry"/> crosses a lifetime boundary.
        /// </summary>
        public event Action<LifetimeEntry, LifetimeBoundaryKind, LifetimeBoundaryCrossingDirection> EntryCrossedBoundary;

        /// <summary>
        /// Contains all the newly-added (but not yet processed) entries.
        /// </summary>
        private readonly List<LifetimeEntry> newEntries = new List<LifetimeEntry>();

        /// <summary>
        /// Contains all the currently-alive entries.
        /// </summary>
        private readonly List<LifetimeEntry> activeEntries = new List<LifetimeEntry>();

        /// <summary>
        /// Contains all entries that should come alive in the future.
        /// </summary>
        private readonly SortedSet<LifetimeEntry> futureEntries = new SortedSet<LifetimeEntry>(new LifetimeStartComparator());

        /// <summary>
        /// Contains all entries that were alive in the past.
        /// </summary>
        private readonly SortedSet<LifetimeEntry> pastEntries = new SortedSet<LifetimeEntry>(new LifetimeEndComparator());

        private readonly Queue<(LifetimeEntry, LifetimeBoundaryKind, LifetimeBoundaryCrossingDirection)> eventQueue =
            new Queue<(LifetimeEntry, LifetimeBoundaryKind, LifetimeBoundaryCrossingDirection)>();

        /// <summary>
        /// Used to ensure a stable sort if multiple entries with the same lifetime are added.
        /// </summary>
        private ulong currentChildId;

        /// <summary>
        /// Adds an entry.
        /// </summary>
        /// <param name="entry">The <see cref="LifetimeEntry"/> to add.</param>
        public void AddEntry(LifetimeEntry entry)
        {
            entry.RequestLifetimeUpdate += requestLifetimeUpdate;
            entry.ChildId = ++currentChildId;
            entry.State = LifetimeEntryState.New;

            newEntries.Add(entry);
        }

        /// <summary>
        /// Removes an entry.
        /// </summary>
        /// <param name="entry">The <see cref="LifetimeEntry"/> to remove.</param>
        /// <returns>Whether <paramref name="entry"/> was contained by this <see cref="LifetimeEntryManager"/>.</returns>
        public bool RemoveEntry(LifetimeEntry entry)
        {
            entry.RequestLifetimeUpdate -= requestLifetimeUpdate;

            bool removed = false;

            switch (entry.State)
            {
                case LifetimeEntryState.New:
                    removed = newEntries.Remove(entry);
                    break;

                case LifetimeEntryState.Current:
                    removed = activeEntries.Remove(entry);

                    if (removed)
                        EntryBecameDead?.Invoke(entry);

                    break;

                case LifetimeEntryState.Past:
                    // Past entries may be found in the newEntries set after a lifetime change (see requestLifetimeUpdate).
                    removed = pastEntries.Remove(entry) || newEntries.Remove(entry);
                    break;

                case LifetimeEntryState.Future:
                    // Future entries may be found in the newEntries set after a lifetime change (see requestLifetimeUpdate).
                    removed = futureEntries.Remove(entry) || newEntries.Remove(entry);
                    break;
            }

            if (!removed)
                return false;

            entry.ChildId = 0;
            return true;
        }

        /// <summary>
        /// Removes all entries.
        /// </summary>
        public void ClearEntries()
        {
            foreach (var entry in newEntries)
            {
                entry.RequestLifetimeUpdate -= requestLifetimeUpdate;
                entry.ChildId = 0;
            }

            foreach (var entry in pastEntries)
            {
                entry.RequestLifetimeUpdate -= requestLifetimeUpdate;
                entry.ChildId = 0;
            }

            foreach (var entry in activeEntries)
            {
                entry.RequestLifetimeUpdate -= requestLifetimeUpdate;
                EntryBecameDead?.Invoke(entry);
                entry.ChildId = 0;
            }

            foreach (var entry in futureEntries)
            {
                entry.RequestLifetimeUpdate -= requestLifetimeUpdate;
                entry.ChildId = 0;
            }

            newEntries.Clear();
            pastEntries.Clear();
            activeEntries.Clear();
            futureEntries.Clear();
        }

        /// <summary>
        /// Invoked when the lifetime of an entry is going to changed.
        /// </summary>
        private void requestLifetimeUpdate(LifetimeEntry entry)
        {
            // Entries in the past/future sets need to be re-sorted to prevent the comparer from becoming unstable.
            // To prevent, e.g. CompositeDrawable alive children changing during enumeration, the entry's state must not be updated immediately.
            //
            // In order to achieve the above, the entry is first removed from the past/future set (resolving the comparator stability issues)
            // and then re-queued back onto the new entries list to be re-processed in the next Update().
            //
            // Note that this does not apply to entries that are in the current set, as they don't utilise a lifetime comparer.

            var futureOrPastSet = futureOrPastEntries(entry.State);

            if (futureOrPastSet?.Remove(entry) == true)
            {
                // Enqueue the entry to be processed in the next Update().
                newEntries.Add(entry);
            }
        }

        /// <summary>
        /// Retrieves the sorted set for a <see cref="LifetimeEntryState"/>.
        /// </summary>
        /// <param name="state">The <see cref="LifetimeEntryState"/>.</param>
        /// <returns>Either <see cref="futureEntries"/>, <see cref="pastEntries"/>, or null.</returns>
        [CanBeNull]
        private SortedSet<LifetimeEntry> futureOrPastEntries(LifetimeEntryState state)
        {
            switch (state)
            {
                case LifetimeEntryState.Future:
                    return futureEntries;

                case LifetimeEntryState.Past:
                    return pastEntries;

                default:
                    return null;
            }
        }

        /// <summary>
        /// Updates the lifetime of entries at a single time value.
        /// </summary>
        /// <param name="time">The time to update at.</param>
        /// <returns>Whether the state of any entries changed.</returns>
        public bool Update(double time) => Update(time, time);

        /// <summary>
        /// Updates the lifetime of entries within a given time range.
        /// </summary>
        /// <param name="startTime">The start of the time range.</param>
        /// <param name="endTime">The end of the time range.</param>
        /// <returns>Whether the state of any entries changed.</returns>
        public bool Update(double startTime, double endTime)
        {
            endTime = Math.Max(endTime, startTime);

            bool aliveChildrenChanged = false;

            // Check for newly-added entries.
            foreach (var entry in newEntries)
                aliveChildrenChanged |= updateChildEntry(entry, startTime, endTime, true, true);
            newEntries.Clear();

            // Check for newly alive entries when time is increased.
            while (futureEntries.Count > 0)
            {
                FrameStatistics.Increment(StatisticsCounterType.CCL);

                var entry = futureEntries.Min.AsNonNull(); // guaranteed by the .Count > 0 guard.
                Debug.Assert(entry.State == LifetimeEntryState.Future);

                if (getState(entry, startTime, endTime) == LifetimeEntryState.Future)
                    break;

                futureEntries.Remove(entry);
                aliveChildrenChanged |= updateChildEntry(entry, startTime, endTime, false, true);
            }

            // Check for newly alive entries when time is decreased.
            while (pastEntries.Count > 0)
            {
                FrameStatistics.Increment(StatisticsCounterType.CCL);

                var entry = pastEntries.Max.AsNonNull(); // guaranteed by the .Count > 0 guard.
                Debug.Assert(entry.State == LifetimeEntryState.Past);

                if (getState(entry, startTime, endTime) == LifetimeEntryState.Past)
                    break;

                pastEntries.Remove(entry);
                aliveChildrenChanged |= updateChildEntry(entry, startTime, endTime, false, true);
            }

            // Checks for newly dead entries when time is increased/decreased.
            foreach (var entry in activeEntries)
            {
                FrameStatistics.Increment(StatisticsCounterType.CCL);
                aliveChildrenChanged |= updateChildEntry(entry, startTime, endTime, false, false);
            }

            // Remove all newly-dead entries.
            activeEntries.RemoveAll(e => e.State != LifetimeEntryState.Current);

            while (eventQueue.Count != 0)
            {
                var (entry, kind, direction) = eventQueue.Dequeue();
                EntryCrossedBoundary?.Invoke(entry, kind, direction);
            }

            return aliveChildrenChanged;
        }

        /// <summary>
        /// Updates the state of a single <see cref="LifetimeEntry"/>.
        /// </summary>
        /// <param name="entry">The <see cref="LifetimeEntry"/> to update.</param>
        /// <param name="startTime">The start of the time range.</param>
        /// <param name="endTime">The end of the time range.</param>
        /// <param name="isNewEntry">Whether <paramref name="entry"/> is part of the new entries set.
        /// The state may be "new" or "past"/"future", in which case it will undergo further processing to return it to the correct set.</param>
        /// <param name="mutateActiveEntries">Whether <see cref="activeEntries"/> should be mutated by this invocation.
        /// If <c>false</c>, the caller is expected to handle mutation of <see cref="activeEntries"/> based on any changes to the entry's state.</param>
        /// <returns>Whether the state of <paramref name="entry"/> has changed.</returns>
        private bool updateChildEntry(LifetimeEntry entry, double startTime, double endTime, bool isNewEntry, bool mutateActiveEntries)
        {
            LifetimeEntryState oldState = entry.State;

            // Past/future sets don't call this function unless a state change is guaranteed.
            Debug.Assert(!futureEntries.Contains(entry) && !pastEntries.Contains(entry));

            // The entry can be in one of three states:
            // 1. The entry was previously in the past/future sets but a lifetime change was requested. Its state is currently "past"/"future".
            // 2. The entry is a completely new entry. Its state is currently "new".
            // 3. The entry is currently-active. Its state is "current" but it's also in the active set.
            Debug.Assert(oldState != LifetimeEntryState.Current || activeEntries.Contains(entry));

            LifetimeEntryState newState = getState(entry, startTime, endTime);
            Debug.Assert(newState != LifetimeEntryState.New);

            if (newState == oldState)
            {
                // If the state hasn't changed, then there's two possibilities:
                // 1. The entry was in the past/future sets and a lifetime change was requested. The entry needs to be added back to the past/future sets.
                // 2. The entry is and continues to remain active.
                if (isNewEntry)
                    futureOrPastEntries(newState)?.Add(entry);
                else
                    Debug.Assert(newState == LifetimeEntryState.Current);

                // In both cases, the entry doesn't need to be processed further as it's already in the correct state.
                return false;
            }

            bool aliveEntriesChanged = false;

            if (newState == LifetimeEntryState.Current)
            {
                if (mutateActiveEntries)
                    activeEntries.Add(entry);

                EntryBecameAlive?.Invoke(entry);
                aliveEntriesChanged = true;
            }
            else if (oldState == LifetimeEntryState.Current)
            {
                if (mutateActiveEntries)
                    activeEntries.Remove(entry);

                EntryBecameDead?.Invoke(entry);
                aliveEntriesChanged = true;
            }

            entry.State = newState;
            futureOrPastEntries(newState)?.Add(entry);
            enqueueEvents(entry, oldState, newState);

            return aliveEntriesChanged;
        }

        /// <summary>
        /// Retrieves the new state for an entry.
        /// </summary>
        /// <param name="entry">The <see cref="LifetimeEntry"/>.</param>
        /// <param name="startTime">The start of the time range.</param>
        /// <param name="endTime">The end of the time range.</param>
        /// <returns>The state of <paramref name="entry"/>. Can be either <see cref="LifetimeEntryState.Past"/>, <see cref="LifetimeEntryState.Current"/>, or <see cref="LifetimeEntryState.Future"/>.</returns>
        private LifetimeEntryState getState(LifetimeEntry entry, double startTime, double endTime)
        {
            // Consider a static entry and a moving time range:
            //                 [-----------Entry-----------]
            // [----Range----] |                           |                      (not alive)
            //   [----Range----]                           |                      (alive)
            //                 |             [----Range----]                      (alive)
            //                 |                           [----Range----]        (not alive)
            //                 |                           | [----Range----]      (not alive)
            //                 |                           |

            if (endTime < entry.LifetimeStart)
                return LifetimeEntryState.Future;

            if (startTime >= entry.LifetimeEnd)
                return LifetimeEntryState.Past;

            return LifetimeEntryState.Current;
        }

        private void enqueueEvents(LifetimeEntry entry, LifetimeEntryState oldState, LifetimeEntryState newState)
        {
            Debug.Assert(oldState != newState);

            switch (oldState)
            {
                case LifetimeEntryState.Future:
                    eventQueue.Enqueue((entry, LifetimeBoundaryKind.Start, LifetimeBoundaryCrossingDirection.Forward));
                    if (newState == LifetimeEntryState.Past)
                        eventQueue.Enqueue((entry, LifetimeBoundaryKind.End, LifetimeBoundaryCrossingDirection.Forward));
                    break;

                case LifetimeEntryState.Current:
                    eventQueue.Enqueue(newState == LifetimeEntryState.Past
                        ? (entry, LifetimeBoundaryKind.End, LifetimeBoundaryCrossingDirection.Forward)
                        : (entry, LifetimeBoundaryKind.Start, LifetimeBoundaryCrossingDirection.Backward));
                    break;

                case LifetimeEntryState.Past:
                    eventQueue.Enqueue((entry, LifetimeBoundaryKind.End, LifetimeBoundaryCrossingDirection.Backward));
                    if (newState == LifetimeEntryState.Future)
                        eventQueue.Enqueue((entry, LifetimeBoundaryKind.Start, LifetimeBoundaryCrossingDirection.Backward));
                    break;
            }
        }

        /// <summary>
        /// Compares by <see cref="LifetimeEntry.LifetimeStart"/>.
        /// </summary>
        private sealed class LifetimeStartComparator : IComparer<LifetimeEntry>
        {
            public int Compare(LifetimeEntry x, LifetimeEntry y)
            {
                if (x == null) throw new ArgumentNullException(nameof(x));
                if (y == null) throw new ArgumentNullException(nameof(y));

                int c = x.LifetimeStart.CompareTo(y.LifetimeStart);
                return c != 0 ? c : x.ChildId.CompareTo(y.ChildId);
            }
        }

        /// <summary>
        /// Compares by <see cref="LifetimeEntry.LifetimeEnd"/>.
        /// </summary>
        private sealed class LifetimeEndComparator : IComparer<LifetimeEntry>
        {
            public int Compare(LifetimeEntry x, LifetimeEntry y)
            {
                if (x == null) throw new ArgumentNullException(nameof(x));
                if (y == null) throw new ArgumentNullException(nameof(y));

                int c = x.LifetimeEnd.CompareTo(y.LifetimeEnd);
                return c != 0 ? c : x.ChildId.CompareTo(y.ChildId);
            }
        }
    }
}
