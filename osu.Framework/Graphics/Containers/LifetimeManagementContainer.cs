// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Performance;

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
        private readonly LifetimeEntryManager manager = new LifetimeEntryManager();
        private readonly Dictionary<Drawable, DrawableLifetimeEntry> drawableMap = new Dictionary<Drawable, DrawableLifetimeEntry>();

        public LifetimeManagementContainer()
        {
            manager.EntryBecameAlive += entryBecameAlive;
            manager.EntryBecameDead += entryBecameDead;
            manager.EntryCrossedBoundary += entryCrossedBoundary;
        }

        protected internal override void AddInternal(Drawable drawable)
        {
            var entry = new DrawableLifetimeEntry(drawable);

            manager.AddEntry(entry);
            drawableMap[drawable] = entry;

            base.AddInternal(drawable);
        }

        protected internal override bool RemoveInternal(Drawable drawable)
        {
            if (!drawableMap.TryGetValue(drawable, out var entry))
                return false;

            manager.RemoveEntry(entry);
            drawableMap.Remove(drawable);

            entry.Dispose();

            return base.RemoveInternal(drawable);
        }

        protected internal override void ClearInternal(bool disposeChildren = true)
        {
            foreach (var (_, entry) in drawableMap)
                entry.Dispose();
            drawableMap.Clear();

            base.ClearInternal(disposeChildren);
        }

        protected override bool CheckChildrenLife() => manager.Update(Time.Current);

        private void entryBecameAlive(LifetimeEntry entry)
        {
            var drawable = ((DrawableLifetimeEntry)entry).Drawable;

            if (drawable.Parent != this)
                base.AddInternal(drawable);

            MakeChildAlive(drawable);
        }

        private void entryBecameDead(LifetimeEntry entry) => MakeChildDead(((DrawableLifetimeEntry)entry).Drawable);

        private void entryCrossedBoundary(LifetimeEntry entry, LifetimeBoundaryKind kind, LifetimeBoundaryCrossingDirection direction)
            => OnChildLifetimeBoundaryCrossed(new LifetimeBoundaryCrossedEvent(((DrawableLifetimeEntry)entry).Drawable, kind, direction));

        /// <summary>
        /// Called when the clock is crossed child lifetime boundary.
        /// If child's lifetime is changed during this callback and that causes additional crossings,
        /// those events are queued and this method will be called later (on the same frame).
        /// </summary>
        protected virtual void OnChildLifetimeBoundaryCrossed(LifetimeBoundaryCrossedEvent e)
        {
        }

        private class DrawableLifetimeEntry : LifetimeEntry, IDisposable
        {
            public readonly Drawable Drawable;

            public DrawableLifetimeEntry(Drawable drawable)
            {
                Drawable = drawable;

                Drawable.LifetimeChanged += drawableLifetimeChanged;
                drawableLifetimeChanged(drawable);
            }

            private void drawableLifetimeChanged(Drawable drawable)
            {
                LifetimeStart = drawable.LifetimeStart;
                LifetimeEnd = drawable.LifetimeEnd;
            }

            public void Dispose()
            {
                if (Drawable != null)
                    Drawable.LifetimeChanged -= drawableLifetimeChanged;
            }
        }
    }

    /// <summary>
    /// Represents that the clock is crossed <see cref="LifetimeManagementContainer"/>'s child lifetime boundary i.e. <see cref="Drawable.LifetimeStart"/> or <see cref="Drawable.LifetimeEnd"/>,
    /// </summary>
    public readonly struct LifetimeBoundaryCrossedEvent
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
