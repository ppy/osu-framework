// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using System.Diagnostics;
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
    /// <see cref="Drawable.ShouldBeAlive"/> is not overrided and lifetimes won't change dynamically.
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

        // Not yet loaded children.
        private readonly List<Drawable> newChildren = new List<Drawable>();

        // Children which is currently dead and becomes alive in future (in the Clock sense).
        // Child with earliest LifetimeStart is placed at the last in order to do RemoveAt(Lengh - 1) in O(1) time rather than RemoveAt(0) in O(length) time.
        private readonly SortedList<Drawable> nextChildren = new SortedList<Drawable>(new LifetimeStartComparator());

        /// Children which is currently dead and becomes alive if the clock is rewinded.
        private readonly SortedList<Drawable> prevChildren = new SortedList<Drawable>(new LifetimeEndComparator());

        // newChildren are not on this map.
        private readonly Dictionary<Drawable, ChildState> childStateMap = new Dictionary<Drawable, ChildState>();

        private enum ChildState
        {
            // This child is in AliveInternalChildren.
            Alive,

            // This child is in nextChildren.
            Next,

            // This child is in prevChildren.
            Prev,
        }

        private ChildState getNewState(Drawable child)
        {
            var currentTime = Time.Current;
            return
                currentTime < child.LifetimeStart ? ChildState.Next :
                child.LifetimeEnd <= currentTime ? ChildState.Prev :
                ChildState.Alive;
        }

        private ChildState updateChildState(Drawable child, ChildState? currentState)
        {
            Debug.Assert(child.LoadState >= LoadState.Ready);
            Debug.Assert(currentState == (childStateMap.TryGetValue(child, out var s) ? s : (ChildState?)null));

            var newState = getNewState(child);
            if (newState != currentState)
            {
                childStateMap[child] = newState;

                if (newState == ChildState.Alive)
                {
                    MakeChildAlive(child);
                }
                else
                {
                    if (currentState == ChildState.Alive)
                    {
                        MakeChildDead(child);
                    }

                    if (newState == ChildState.Next)
                    {
                        nextChildren.Add(child);
                    }
                    else
                    {
                        prevChildren.Add(child);
                    }
                }
            }

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
                    aliveChildrenChanged |= newState == ChildState.Alive;
                }
            }

            var currentTime = Time.Current;

            // Checks for newly alive children when time is increased or new children added.
            while (nextChildren.Count > 0)
            {
                FrameStatistics.Increment(StatisticsCounterType.CCL);
                var child = nextChildren[nextChildren.Count - 1];

                if (currentTime < child.LifetimeStart)
                {
                    // Rest of the array contains children which becomes alive at least as late as this child.
                    // Thus we can skip checking.
                    break;
                }

                nextChildren.RemoveAt(nextChildren.Count - 1);

                var newState = updateChildState(child, ChildState.Next);
                aliveChildrenChanged |= newState == ChildState.Alive;

                // Shouldn't happen but if it happens then it becomes an infinity loop so do a fail-fast.
                Trace.Assert(newState != ChildState.Next);
            }

            // Symmetric to above for rewinding case.
            while (prevChildren.Count > 0)
            {
                FrameStatistics.Increment(StatisticsCounterType.CCL);
                var child = prevChildren[prevChildren.Count - 1];

                if (child.LifetimeEnd <= currentTime)
                {
                    break;
                }

                prevChildren.RemoveAt(prevChildren.Count - 1);

                var newState = updateChildState(child, ChildState.Prev);
                aliveChildrenChanged |= newState == ChildState.Alive;

                Trace.Assert(newState != ChildState.Prev);
            }

            for(var i = AliveInternalChildren.Count - 1; i >= 0; -- i)
            {
                FrameStatistics.Increment(StatisticsCounterType.CCL);
                var child = AliveInternalChildren[i];
                Debug.Assert(childStateMap[child] == ChildState.Alive);

                var newState = updateChildState(child, ChildState.Alive);
                aliveChildrenChanged |= newState != ChildState.Alive;
            }

            return aliveChildrenChanged;
        }

        protected internal override void AddInternal(Drawable drawable)
        {
            newChildren.Add(drawable);
            base.AddInternal(drawable);
        }

        protected internal override bool RemoveInternal(Drawable drawable)
        {
            if (childStateMap.TryGetValue(drawable, out var state))
            {
                childStateMap.Remove(drawable);
                switch (state)
                {
                    case ChildState.Next:
                        nextChildren.Remove(drawable);
                        break;
                    case ChildState.Prev:
                        prevChildren.Remove(drawable);
                        break;
                }
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
            nextChildren.Clear();
            prevChildren.Clear();

            base.ClearInternal(disposeChildren);
        }
    }
}
