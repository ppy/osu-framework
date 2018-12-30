// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using osu.Framework.Lists;
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
        private sealed class LifetimeStartComparator : IComparer<Drawable>
        {
            public int Compare(Drawable x, Drawable y)
            {
                // Smallest LifetimeStart is placed on last.
                return (y?.LifetimeStart ?? 0).CompareTo(x?.LifetimeStart ?? 0);
            }
        }

        private sealed class LifetimeEndComparator : IComparer<Drawable>
        {
            public int Compare(Drawable x, Drawable y)
            {
                // Largest LifetimeEnd is placed on last.
                return (x?.LifetimeEnd ?? 0).CompareTo(y?.LifetimeEnd ?? 0);
            }
        }

        /// Not yet loaded children.
        private readonly List<Drawable> newChildren = new List<Drawable>();

        /// Children which is currently dead and becomes alive in future (with respect to <see cref="Drawable.Clock"/>).
        /// Child with earliest LifetimeStart is placed at the last in order to do RemoveAt(Lengh - 1) in O(1) time rather than RemoveAt(0) in O(length) time.
        private readonly SortedList<Drawable> futureChildren = new SortedList<Drawable>(new LifetimeStartComparator());

        /// Children which is currently dead and becomes alive if the clock is rewinded.
        private readonly SortedList<Drawable> pastChildren = new SortedList<Drawable>(new LifetimeEndComparator());

        /// Note: <see cref="newChildren"/> are not on this map.
        private readonly Dictionary<Drawable, ChildState> childStateMap = new Dictionary<Drawable, ChildState>();

        private enum ChildState
        {
            /// This child is in AliveInternalChildren.
            Current,

            /// This child is in futureChildren.
            Future,

            /// This child is in pastChildren.
            Past,
        }

        private ChildState getNewState(Drawable child)
        {
            var currentTime = Time.Current;
            return
                currentTime < child.LifetimeStart ? ChildState.Future :
                child.LifetimeEnd <= currentTime ? ChildState.Past :
                ChildState.Current;
        }

        private ChildState updateChildState(Drawable child, ChildState? currentState)
        {
            Debug.Assert(child.LoadState >= LoadState.Ready);
            Debug.Assert(currentState == (childStateMap.TryGetValue(child, out var s) ? s : (ChildState?)null));

            var newState = getNewState(child);
            if (newState != currentState)
            {
                if (newState == ChildState.Current)
                {
                    MakeChildAlive(child);
                }
                else if (currentState == ChildState.Current)
                {
                    if (MakeChildDead(child))
                        return newState;
                }

                childStateMap[child] = newState;
            }

            getList(newState)?.Add(child);

            return newState;
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
                    newChildren.RemoveAt(i);

                    var newState = updateChildState(child, null);
                    aliveChildrenChanged |= newState == ChildState.Current;
                }
            }

            var currentTime = Time.Current;

            // Checks for newly alive children when time is increased or new children added.
            while (futureChildren.Count > 0)
            {
                FrameStatistics.Increment(StatisticsCounterType.CCL);
                var child = futureChildren[futureChildren.Count - 1];

                if (currentTime < child.LifetimeStart)
                {
                    // Rest of the array contains children which becomes alive at least as late as this child.
                    // Thus we can skip checking.
                    break;
                }

                futureChildren.RemoveAt(futureChildren.Count - 1);

                var newState = updateChildState(child, ChildState.Future);
                aliveChildrenChanged |= newState == ChildState.Current;

                // Shouldn't happen but if it happens then it becomes an infinity loop so do a fail-fast.
                Trace.Assert(newState != ChildState.Future);
            }

            // Symmetric to above for rewinding case.
            while (pastChildren.Count > 0)
            {
                FrameStatistics.Increment(StatisticsCounterType.CCL);
                var child = pastChildren[pastChildren.Count - 1];

                if (child.LifetimeEnd <= currentTime)
                {
                    break;
                }

                pastChildren.RemoveAt(pastChildren.Count - 1);

                var newState = updateChildState(child, ChildState.Past);
                aliveChildrenChanged |= newState == ChildState.Current;

                Trace.Assert(newState != ChildState.Past);
            }

            for(var i = AliveInternalChildren.Count - 1; i >= 0; -- i)
            {
                FrameStatistics.Increment(StatisticsCounterType.CCL);
                var child = AliveInternalChildren[i];
                Debug.Assert(childStateMap.TryGetValue(child, out var s) && s == ChildState.Current);

                var newState = updateChildState(child, ChildState.Current);
                aliveChildrenChanged |= newState != ChildState.Current;
            }

            return aliveChildrenChanged;
        }

        /// <summary>
        /// Returns <see cref="futureChildren"/> or <see cref="pastChildren"/> or null.
        /// </summary>
        [CanBeNull] private SortedList<Drawable> getList(ChildState state)
        {
            switch (state)
            {
                case ChildState.Future:
                    return futureChildren;
                case ChildState.Past:
                    return pastChildren;
                default:
                    return null;
            }
        }

        private void childLifetimeChanged(Drawable child)
        {
            if (!childStateMap.TryGetValue(child, out var state)) return;

            var list = getList(state);
            // We can't use binary search (Remove method) to locate child because lifetime is changed and
            // might be invalid for the list ordering.
            list?.RemoveAt(list.FindIndex(x => x == child));

            updateChildState(child, state);
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

            if (childStateMap.TryGetValue(drawable, out var state))
            {
                childStateMap.Remove(drawable);
                var list = getList(state);
                list?.Remove(drawable);
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
    }
}
