// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        /// <summary>
        /// List of drawables that do not have their lifetime managed by us, but still need to have their aliveness processed once.
        /// </summary>
        private readonly List<Drawable> unmanagedDrawablesToProcess = new List<Drawable>();

        public LifetimeManagementContainer()
        {
            manager.EntryBecameAlive += entryBecameAlive;
            manager.EntryBecameDead += entryBecameDead;
            manager.EntryCrossedBoundary += entryCrossedBoundary;
        }

        protected internal override void AddInternal(Drawable drawable) => AddInternal(drawable, true);

        /// <summary>
        /// Adds a <see cref="Drawable"/> to this <see cref="LifetimeManagementContainer"/>.
        /// </summary>
        /// <param name="drawable">The <see cref="Drawable"/> to add.</param>
        /// <param name="withManagedLifetime">Whether the lifetime of <paramref name="drawable"/> should be managed by this <see cref="LifetimeManagementContainer"/>.</param>
        protected internal void AddInternal(Drawable drawable, bool withManagedLifetime)
        {
            Trace.Assert(!drawable.RemoveWhenNotAlive, $"{nameof(RemoveWhenNotAlive)} is not supported for {nameof(LifetimeManagementContainer)}");

            base.AddInternal(drawable);

            if (withManagedLifetime)
                manager.AddEntry(drawableMap[drawable] = new DrawableLifetimeEntry(drawable));
            else if (drawable.LoadState >= LoadState.Ready)
                MakeChildAlive(drawable);
            else
                unmanagedDrawablesToProcess.Add(drawable);
        }

        protected internal override bool RemoveInternal(Drawable drawable)
        {
            unmanagedDrawablesToProcess.Remove(drawable);

            if (!drawableMap.TryGetValue(drawable, out var entry))
                return base.RemoveInternal(drawable);

            manager.RemoveEntry(entry);
            drawableMap.Remove(drawable);

            entry.Dispose();

            return base.RemoveInternal(drawable);
        }

        protected internal override void ClearInternal(bool disposeChildren = true)
        {
            manager.ClearEntries();
            unmanagedDrawablesToProcess.Clear();

            foreach (var (_, entry) in drawableMap)
                entry.Dispose();
            drawableMap.Clear();

            base.ClearInternal(disposeChildren);
        }

        protected override bool CheckChildrenLife()
        {
            bool aliveChanged = unmanagedDrawablesToProcess.Count > 0;

            foreach (var d in unmanagedDrawablesToProcess)
                MakeChildAlive(d);
            unmanagedDrawablesToProcess.Clear();

            aliveChanged |= manager.Update(Time.Current);

            return aliveChanged;
        }

        private void entryBecameAlive(LifetimeEntry entry) => MakeChildAlive(((DrawableLifetimeEntry)entry).Drawable);

        private void entryBecameDead(LifetimeEntry entry)
        {
            bool removed = MakeChildDead(((DrawableLifetimeEntry)entry).Drawable);
            Trace.Assert(!removed, $"{nameof(RemoveWhenNotAlive)} is not supported for children of {nameof(LifetimeManagementContainer)}");
        }

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
