// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using osu.Framework.Statistics;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// This container is optimized for when number of alive children is significantly smaller than number of all children.
    /// Specifically, the time complexity of <see cref="Drawable.Update"/> should be closer to
    /// O(number of alive children + 1) rather than O(number of all children + 1) for typical frames.
    /// </summary>
    /// <remarks>
    /// This container assumes child's <see cref="Drawable.Clock"/> is the same as ours and
    /// <see cref="Drawable.ShouldBeAlive"/> is not overrided.
    /// Also, <see cref="Drawable.RemoveWhenNotAlive"/> should be false.
    /// </remarks>
    public class LifetimeManagementContainer : CompositeDrawable
    {
        /// <summary>
        /// Contains all but <see cref="newChildren"/>.
        /// </summary>
        private readonly Dictionary<Drawable, ChildEntry> childStateMap = new Dictionary<Drawable, ChildEntry>();

        private readonly List<Drawable> newChildren = new List<Drawable>();

        private readonly SortedSet<ChildEntry> futureChildren = new SortedSet<ChildEntry>(new LifetimeStartComparator());

        private readonly SortedSet<ChildEntry> pastChildren = new SortedSet<ChildEntry>(new LifetimeEndComparator());

        private readonly Queue<LifetimeBoundaryCrossedEvent> eventQueue = new Queue<LifetimeBoundaryCrossedEvent>();

        /// <summary>
        /// Update child life according to the current time.
        /// The entry should be in <see cref="childStateMap"/>.
        /// If the current state is <see cref="LifetimeState.Current"/> then
        /// the child should be contained in <see cref="CompositeDrawable.AliveInternalChildren"/>.
        /// Otherwise, the entry should already be removed from the corresponding list.
        /// </summary>
        /// <returns>Whether <see cref="CompositeDrawable.AliveInternalChildren"/> has changed.</returns>
        private bool updateChildEntry([NotNull] ChildEntry entry, bool onChildLifetimeChange = false)
        {
            var child = entry.Drawable;
            var oldState = entry.State;
            Debug.Assert(child.LoadState >= LoadState.Ready);
            Debug.Assert(childStateMap.TryGetValue(child, out var e) && e == entry);
            Debug.Assert(oldState != LifetimeState.Current || AliveInternalChildren.Contains(child));
            Debug.Assert(!futureChildren.Contains(entry) && !pastChildren.Contains(entry));

            var currentTime = Time.Current;
            var newState =
                currentTime < entry.LifetimeStart ? LifetimeState.Future :
                entry.LifetimeEnd <= currentTime ? LifetimeState.Past :
                LifetimeState.Current;

            if (newState == oldState)
            {
                // We need to re-insert to future/past children even if lifetime state is not changed if it is from ChildLifetimeChange.
                if (onChildLifetimeChange)
                    futureOrPastChildren(newState)?.Add(entry);
                else
                    Debug.Assert(newState != LifetimeState.Future && newState != LifetimeState.Past);
                return false;
            }

            bool aliveChildrenChanged = false;

            if (newState == LifetimeState.Current)
            {
                MakeChildAlive(child);
                aliveChildrenChanged = true;
            }
            else if (oldState == LifetimeState.Current)
            {
                bool removed = MakeChildDead(child);
                Trace.Assert(!removed, $"{nameof(RemoveWhenNotAlive)} is not supported for children of {nameof(LifetimeManagementContainer)}");
                aliveChildrenChanged = true;
            }

            entry.State = newState;
            futureOrPastChildren(newState)?.Add(entry);

            enqueueEvents(child, oldState, newState);

            return aliveChildrenChanged;
        }

        private void enqueueEvents(Drawable child, LifetimeState oldState, LifetimeState newState)
        {
            Debug.Assert(oldState != newState);

            switch (oldState)
            {
                case LifetimeState.Future:
                    eventQueue.Enqueue(new LifetimeBoundaryCrossedEvent(child, LifetimeBoundaryKind.Start, LifetimeBoundaryCrossingDirection.Forward));
                    if (newState == LifetimeState.Past)
                        eventQueue.Enqueue(new LifetimeBoundaryCrossedEvent(child, LifetimeBoundaryKind.End, LifetimeBoundaryCrossingDirection.Forward));
                    break;

                case LifetimeState.Current:
                    eventQueue.Enqueue(newState == LifetimeState.Past
                        ? new LifetimeBoundaryCrossedEvent(child, LifetimeBoundaryKind.End, LifetimeBoundaryCrossingDirection.Forward)
                        : new LifetimeBoundaryCrossedEvent(child, LifetimeBoundaryKind.Start, LifetimeBoundaryCrossingDirection.Backward));
                    break;

                case LifetimeState.Past:
                    eventQueue.Enqueue(new LifetimeBoundaryCrossedEvent(child, LifetimeBoundaryKind.End, LifetimeBoundaryCrossingDirection.Backward));
                    if (newState == LifetimeState.Future)
                        eventQueue.Enqueue(new LifetimeBoundaryCrossedEvent(child, LifetimeBoundaryKind.Start, LifetimeBoundaryCrossingDirection.Backward));
                    break;
            }
        }

        protected override bool CheckChildrenLife()
        {
            // We have to at least wait until Clock becomes available.
            if (LoadState != LoadState.Loaded) return false;

            bool aliveChildrenChanged = false;

            // Move loaded children to appropriate list.
            for (var i = newChildren.Count - 1; i >= 0; --i)
            {
                FrameStatistics.Increment(StatisticsCounterType.CCL);
                var child = newChildren[i];

                if (child.LoadState >= LoadState.Ready)
                {
                    Debug.Assert(!childStateMap.ContainsKey(child));

                    newChildren.RemoveAt(i);

                    var entry = new ChildEntry(child);
                    childStateMap.Add(child, entry);

                    aliveChildrenChanged |= updateChildEntry(entry);
                }
            }

            var currentTime = Time.Current;

            // Checks for newly alive children when time is increased or new children added.
            while (futureChildren.Count > 0)
            {
                FrameStatistics.Increment(StatisticsCounterType.CCL);
                var entry = futureChildren.Min;
                Debug.Assert(entry.State == LifetimeState.Future);

                if (currentTime < entry.LifetimeStart)
                    break;

                futureChildren.Remove(entry);

                aliveChildrenChanged |= updateChildEntry(entry);
            }

            while (pastChildren.Count > 0)
            {
                FrameStatistics.Increment(StatisticsCounterType.CCL);
                var entry = pastChildren.Max;
                Debug.Assert(entry.State == LifetimeState.Past);

                if (entry.LifetimeEnd <= currentTime)
                    break;

                pastChildren.Remove(entry);

                aliveChildrenChanged |= updateChildEntry(entry);
            }

            for (var i = AliveInternalChildren.Count - 1; i >= 0; --i)
            {
                FrameStatistics.Increment(StatisticsCounterType.CCL);
                var child = AliveInternalChildren[i];
                Debug.Assert(childStateMap.ContainsKey(child));
                var entry = childStateMap[child];

                aliveChildrenChanged |= updateChildEntry(entry);
            }

            Debug.Assert(newChildren.Count + futureChildren.Count + pastChildren.Count + AliveInternalChildren.Count == InternalChildren.Count);

            while (eventQueue.Count != 0)
            {
                var e = eventQueue.Dequeue();
                OnChildLifetimeBoundaryCrossed(e);
            }

            return aliveChildrenChanged;
        }

        [CanBeNull]
        private SortedSet<ChildEntry> futureOrPastChildren(LifetimeState state)
        {
            switch (state)
            {
                case LifetimeState.Future:
                    return futureChildren;

                case LifetimeState.Past:
                    return pastChildren;

                default:
                    return null;
            }
        }

        private void childLifetimeChanged(Drawable child)
        {
            if (!childStateMap.TryGetValue(child, out var entry)) return;

            futureOrPastChildren(entry.State)?.Remove(entry);
            entry.UpdateLifetime();

            updateChildEntry(entry, true);
        }

        protected internal override void AddInternal(Drawable drawable)
        {
            Trace.Assert(!drawable.RemoveWhenNotAlive, $"{nameof(RemoveWhenNotAlive)} is not supported for {nameof(LifetimeManagementContainer)}");
            drawable.LifetimeChanged += childLifetimeChanged;

            newChildren.Add(drawable);
            base.AddInternal(drawable);
        }

        protected internal override bool RemoveInternal(Drawable drawable)
        {
            drawable.LifetimeChanged -= childLifetimeChanged;

            if (childStateMap.TryGetValue(drawable, out var entry))
            {
                childStateMap.Remove(drawable);
                futureOrPastChildren(entry.State)?.Remove(entry);
            }
            else
            {
                newChildren.Remove(drawable);
            }

            return base.RemoveInternal(drawable);
        }

        protected internal override void ClearInternal(bool disposeChildren = true)
        {
            childStateMap.Clear();
            newChildren.Clear();
            futureChildren.Clear();
            pastChildren.Clear();

            base.ClearInternal(disposeChildren);
        }

        /// <summary>
        /// Called when the clock is crossed child lifetime boundary.
        /// If child's lifetime is changed during this callback and that causes additional crossings,
        /// those events are queued and this method will be called later (on the same frame).
        /// </summary>
        protected virtual void OnChildLifetimeBoundaryCrossed(LifetimeBoundaryCrossedEvent e)
        {
        }

        /// We have to maintain lifetime separately because it is used as a key of sorted set and
        /// dynamic change of key ordering is invalid.
        private sealed class ChildEntry
        {
            [NotNull]
            public readonly Drawable Drawable;

            public LifetimeState State { get; set; }
            public double LifetimeStart { get; private set; }
            public double LifetimeEnd { get; private set; }

            public ChildEntry([NotNull] Drawable drawable)
            {
                Drawable = drawable;
                State = LifetimeState.New;
                UpdateLifetime();
            }

            public void UpdateLifetime()
            {
                LifetimeStart = Drawable.LifetimeStart;
                LifetimeEnd = Math.Max(Drawable.LifetimeStart, Drawable.LifetimeEnd); // Negative intervals are undesired for calculation.
            }
        }

        /// <summary>
        /// Compare by <see cref="ChildEntry.LifetimeStart"/>.
        /// </summary>
        private sealed class LifetimeStartComparator : IComparer<ChildEntry>
        {
            public int Compare(ChildEntry x, ChildEntry y)
            {
                if (x == null) throw new ArgumentNullException(nameof(x));
                if (y == null) throw new ArgumentNullException(nameof(y));

                var c = x.LifetimeStart.CompareTo(y.LifetimeStart);
                return c != 0 ? c : x.Drawable.ChildID.CompareTo(y.Drawable.ChildID);
            }
        }

        /// <summary>
        /// Compare by <see cref="ChildEntry.LifetimeEnd"/>.
        /// </summary>
        private sealed class LifetimeEndComparator : IComparer<ChildEntry>
        {
            public int Compare(ChildEntry x, ChildEntry y)
            {
                if (x == null) throw new ArgumentNullException(nameof(x));
                if (y == null) throw new ArgumentNullException(nameof(y));

                var c = x.LifetimeEnd.CompareTo(y.LifetimeEnd);
                return c != 0 ? c : x.Drawable.ChildID.CompareTo(y.Drawable.ChildID);
            }
        }

        private enum LifetimeState
        {
            /// Not yet loaded.
            New,

            /// Currently dead and becomes alive in the future: current time &lt; <see cref="Drawable.LifetimeStart"/>.
            Future,

            /// Currently alive.
            Current,

            /// Currently dead and becomes alive if the clock is rewound: <see cref="Drawable.LifetimeEnd"/> &lt;= current time.
            Past,
        }
    }

    /// <summary>
    /// Represents a direction of lifetime boundary crossing.
    /// </summary>
    public enum LifetimeBoundaryCrossingDirection
    {
        /// <summary>
        /// A crossing from past to future.
        /// </summary>
        Forward,

        /// <summary>
        /// A crossing from future to past.
        /// </summary>
        Backward,
    }

    /// <summary>
    /// Represents one of boudaries of lifetime interval.
    /// </summary>
    public enum LifetimeBoundaryKind
    {
        /// <summary>
        /// <see cref="Drawable.LifetimeStart"/>.
        /// </summary>
        Start,

        /// <summary>
        /// <see cref="Drawable.LifetimeEnd"/>.
        /// </summary>
        End,
    }

    /// <summary>
    /// Represents that the clock is crossed <see cref="LifetimeManagementContainer"/>'s child lifetime boundary i.e. <see cref="Drawable.LifetimeStart"/> or <see cref="Drawable.LifetimeEnd"/>,
    /// </summary>
    public struct LifetimeBoundaryCrossedEvent
    {
        /// <summary>
        /// The drawable.
        /// </summary>
        public readonly Drawable Child;

        /// <summary>
        /// Which lifetime boundary is crossed.
        /// </summary>
        public readonly LifetimeBoundaryKind Kind;

        /// <summary>
        /// The direction of the crossing.
        /// </summary>
        public readonly LifetimeBoundaryCrossingDirection Direction;

        public LifetimeBoundaryCrossedEvent(Drawable child, LifetimeBoundaryKind kind, LifetimeBoundaryCrossingDirection direction)
        {
            Child = child;
            Kind = kind;
            Direction = direction;
        }

        public override string ToString() => $"({Child.ChildID}, {Kind}, {Direction})";
    }
}
