// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

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
        private enum LifetimeState
        {
            /// Not yet loaded.
            New,
            /// Currently dead and becomes alive in the future (with respect to <see cref="Drawable.Clock"/>).
            Future,
            /// Currently dead and becomes alive if the clock is rewinded.
            Past,
            /// Currently alive.
            Current,
        }

        /// We have to maintain lifetime separately because it is used as a key of sorted set and
        /// dynamic change of key ordering is invalid.
        private sealed class ChildEntry
        {
            [NotNull] public readonly Drawable Drawable;
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
                LifetimeEnd = Math.Max(Drawable.LifetimeStart, Drawable.LifetimeEnd);    // Negative intervals are undesired for calculation.
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

        /// <summary>
        /// Contains all but <see cref="newChildren"/>.
        /// </summary>
        private readonly Dictionary<Drawable, ChildEntry> childStateMap = new Dictionary<Drawable, ChildEntry>();

        private readonly List<Drawable> newChildren = new List<Drawable>();

        private readonly SortedSet<ChildEntry> futureChildren = new SortedSet<ChildEntry>(new LifetimeStartComparator());

        private readonly SortedSet<ChildEntry> pastChildren = new SortedSet<ChildEntry>(new LifetimeEndComparator());

        /// <summary>
        /// Update child life according to the current time.
        /// The entry should be in <see cref="childStateMap"/>.
        /// If the current state is <see cref="LifetimeState.Current"/> then
        /// the child should be contained in <see cref="CompositeDrawable.AliveInternalChildren"/>.
        /// Otherwise, the entry should already be removed from the corresponding list.
        /// </summary>
        /// <returns>Whether <see cref="CompositeDrawable.AliveInternalChildren"/> has changed.</returns>
        private bool updateChildEntry([NotNull] ChildEntry entry)
        {
            var child = entry.Drawable;
            Debug.Assert(child.LoadState >= LoadState.Ready);
            Debug.Assert(childStateMap.TryGetValue(child, out var e) && e == entry);
            Debug.Assert(entry.State != LifetimeState.Current || AliveInternalChildren.Contains(child));
            Debug.Assert(!futureChildren.Contains(entry) && !pastChildren.Contains(entry));

            var currentTime = Time.Current;
            var newState =
                currentTime < entry.LifetimeStart ? LifetimeState.Future :
                entry.LifetimeEnd <= currentTime ? LifetimeState.Past :
                LifetimeState.Current;
            if (newState == entry.State)
            {
                Debug.Assert(newState != LifetimeState.Future && newState != LifetimeState.Past);
                return false;
            }

            bool aliveChildrenChanged = false;
            if (newState == LifetimeState.Current)
            {
                MakeChildAlive(child);
                aliveChildrenChanged = true;
            }
            else if (entry.State == LifetimeState.Current)
            {
                if (MakeChildDead(child))
                    return true;
                aliveChildrenChanged = true;
            }
            else if (entry.State != LifetimeState.New)
            {
                OnChildLifetimeSkipped(child, entry.State == LifetimeState.Future ? SkipDirection.Forward : SkipDirection.Backward);
            }

            entry.State = newState;
            futureOrPastChildren(newState)?.Add(entry);

            return aliveChildrenChanged;
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
                Debug.Assert(child.LoadState < LoadState.Loaded);

                if (child.LoadState == LoadState.Ready)
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

            for(var i = AliveInternalChildren.Count - 1; i >= 0; -- i)
            {
                FrameStatistics.Increment(StatisticsCounterType.CCL);
                var child = AliveInternalChildren[i];
                Debug.Assert(childStateMap.ContainsKey(child));
                var entry = childStateMap[child];

                aliveChildrenChanged |= updateChildEntry(entry);
            }

            Debug.Assert(newChildren.Count + futureChildren.Count + pastChildren.Count + AliveInternalChildren.Count == InternalChildren.Count);

            return aliveChildrenChanged;
        }

        [CanBeNull] private SortedSet<ChildEntry> futureOrPastChildren(LifetimeState state)
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

            // We need to re-insert to the future/past children set even if state is unchanged.
            if (entry.State == LifetimeState.Future || entry.State == LifetimeState.Past)
                entry.State = LifetimeState.New;
            entry.UpdateLifetime();

            updateChildEntry(entry);
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
        /// Represents a direction of skip.
        /// </summary>
        public enum SkipDirection
        {
            /// <summary>
            /// A skip from past to future.
            /// </summary>
            Forward,

            /// <summary>
            /// A skip from future to past.
            /// </summary>
            Backward,
        }

        /// <summary>
        /// Invoked when the clock is skipped child lifetime interval completely.
        /// For example, when child lifetime is [1,2) and clock is skipped from 0 to 3, it is a <see cref="SkipDirection.Forward"/> skip.
        /// </summary>
        /// <param name="child">The skipped child.</param>
        /// <param name="skipDirection">The direction of the skip.</param>
        protected virtual void OnChildLifetimeSkipped(Drawable child, SkipDirection skipDirection)
        {
        }
    }
}
