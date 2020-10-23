// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using osu.Framework.Graphics.Containers;
using osu.Framework.Statistics;

namespace osu.Framework.Graphics.Performance
{
    /// <summary>
    /// Provides time-optimised updates for notifications of lifetime change notifications.
    /// This is used in specialised <see cref="CompositeDrawable"/>s to optimise lifetime changes (see: <see cref="CompositeDrawable"/>).
    /// </summary>
    /// <remarks>
    /// The time complexity of updating lifetimes is O(number of alive items).
    /// </remarks>
    public class LifetimeEntryManager
    {
        /// <summary>
        /// Invoked immediately when a <see cref="LifetimeEntry"/> becomes alive.
        /// </summary>
        public event Action<LifetimeEntry> OnBecomeAlive;

        /// <summary>
        /// Invoked immediately when a <see cref="LifetimeEntry"/> becomes dead.
        /// </summary>
        public event Action<LifetimeEntry> OnBecomeDead;

        /// <summary>
        /// Invoked when a <see cref="LifetimeEntry"/> crosses a lifetime boundary.
        /// </summary>
        public event Action<LifetimeEntry, LifetimeBoundaryKind, LifetimeBoundaryCrossingDirection> OnBoundaryCrossed;

        /// <summary>
        /// Contains all the newly-added (but not yet processed) entries.
        /// </summary>
        private readonly HashSet<LifetimeEntry> newEntries = new HashSet<LifetimeEntry>();

        /// <summary>
        /// Contains all the currently-alive entries.
        /// </summary>
        private readonly HashSet<LifetimeEntry> activeEntries = new HashSet<LifetimeEntry>();

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
                        OnBecomeDead?.Invoke(entry);

                    break;

                case LifetimeEntryState.Past:
                    removed = pastEntries.Remove(entry);
                    break;

                case LifetimeEntryState.Future:
                    removed = futureEntries.Remove(entry);
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
                OnBecomeDead?.Invoke(entry);
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
        /// Invoked when the lifetime of an entry has changed, so that an appropriate sorting order is maintained.
        /// </summary>
        /// <param name="entry">The <see cref="LifetimeEntry"/> that changed.</param>
        /// <param name="lifetimeStart">The new start time.</param>
        /// <param name="lifetimeEnd">The new end time.</param>
        private void requestLifetimeUpdate(LifetimeEntry entry, double lifetimeStart, double lifetimeEnd)
        {
            var futureOrPastSet = futureOrPastEntries(entry.State);

            if (futureOrPastSet != null)
            {
                futureOrPastSet.Remove(entry);

                // Since the entry is no-longer present inside this manager, it needs to be enqueued back into the new entry list to be processed in the next update.
                newEntries.Add(entry);
            }

            entry.UpdateLifetime(lifetimeStart, lifetimeEnd);
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
                aliveChildrenChanged |= updateChildEntry(startTime, endTime, entry, true, true);
            newEntries.Clear();

            // Check for newly alive entries when time is increased.
            while (futureEntries.Count > 0)
            {
                FrameStatistics.Increment(StatisticsCounterType.CCL);

                var entry = futureEntries.Min;
                Debug.Assert(entry.State == LifetimeEntryState.Future);

                if (getState(entry, startTime, endTime) == LifetimeEntryState.Future)
                    break;

                futureEntries.Remove(entry);
                aliveChildrenChanged |= updateChildEntry(startTime, endTime, entry, false, true);
            }

            // Check for newly alive entries when time is decreased.
            while (pastEntries.Count > 0)
            {
                FrameStatistics.Increment(StatisticsCounterType.CCL);

                var entry = pastEntries.Max;
                Debug.Assert(entry.State == LifetimeEntryState.Past);

                if (getState(entry, startTime, endTime) == LifetimeEntryState.Past)
                    break;

                pastEntries.Remove(entry);
                aliveChildrenChanged |= updateChildEntry(startTime, endTime, entry, false, true);
            }

            // Checks for newly dead entries when time is increased/decreased.
            foreach (var entry in activeEntries)
            {
                FrameStatistics.Increment(StatisticsCounterType.CCL);
                aliveChildrenChanged |= updateChildEntry(startTime, endTime, entry, false, false);
            }

            // Remove all newly-dead entries.
            activeEntries.RemoveWhere(e => e.State != LifetimeEntryState.Current);

            while (eventQueue.Count != 0)
            {
                var (entry, kind, direction) = eventQueue.Dequeue();
                OnBoundaryCrossed?.Invoke(entry, kind, direction);
            }

            return aliveChildrenChanged;
        }

        private bool updateChildEntry(double startTime, double endTime, LifetimeEntry entry, bool fromLifetimeChange, bool mutateActive)
        {
            LifetimeEntryState oldState = entry.State;

            Debug.Assert(!futureEntries.Contains(entry) && !pastEntries.Contains(entry));
            Debug.Assert(oldState != LifetimeEntryState.Current || activeEntries.Contains(entry));

            LifetimeEntryState newState = getState(entry, startTime, endTime);

            Debug.Assert(newState != LifetimeEntryState.New);

            // If the state hasn't changed...
            if (newState == oldState)
            {
                // Then we need to re-insert to future/past entries if updating from a lifetime change event.
                if (fromLifetimeChange)
                    futureOrPastEntries(newState)?.Add(entry);
                // Otherwise, we should only be here if we're updating the active entries.
                else
                    Debug.Assert(newState == LifetimeEntryState.Current);

                return false;
            }

            bool aliveEntriesChanged = false;

            if (newState == LifetimeEntryState.Current)
            {
                if (mutateActive)
                    activeEntries.Add(entry);

                OnBecomeAlive?.Invoke(entry);
                aliveEntriesChanged = true;
            }
            else if (oldState == LifetimeEntryState.Current)
            {
                if (mutateActive)
                    activeEntries.Remove(entry);

                OnBecomeDead?.Invoke(entry);
                aliveEntriesChanged = true;
            }

            entry.State = newState;
            futureOrPastEntries(newState)?.Add(entry);
            enqueueEvents(entry, oldState, newState);

            return aliveEntriesChanged;
        }

        private LifetimeEntryState getState(LifetimeEntry entry, double rangeStart, double rangeEnd)
        {
            // Consider a static entry and a moving time range:
            //                 [-----------Entry-----------]
            // [----Range----]                                                    (not alive)
            //   [----Range----]                                                  (alive)
            //                               [----Range----]                      (alive)
            //                                             [----Range----]        (not alive)
            //                                              [----Range----]       (not alive)
            //

            if (rangeEnd < entry.LifetimeStart)
                return LifetimeEntryState.Future;

            if (rangeStart >= entry.LifetimeEnd)
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
        /// Compare by <see cref="LifetimeEntry.LifetimeStart"/>.
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
        /// Compare by <see cref="LifetimeEntry.LifetimeEnd"/>.
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
