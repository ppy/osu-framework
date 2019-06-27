// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Graphics.Containers
{
    public class SafeAreaSnappingContainer : SafeAreaSnappingContainer<Drawable>
    {
    }

    /// <summary>
    /// An <see cref="EdgeSnappingContainer{T}"/> that can also apply a padding based on the safe areas of the nearest cached <see cref="SafeAreaTargetContainer{T}"/>.
    /// Padding will only be applied if the contents of this container would otherwise intersect the safe area margins relative to the associated target container.
    /// Padding may be applied to individual edges by setting the <see cref="SafeEdges"/> property.
    /// </summary>
    public class SafeAreaSnappingContainer<T> : EdgeSnappingContainer<T>
        where T : Drawable
    {
        [Resolved]
        private SafeAreaTargetContainer safeAreaTargetContainer { get; set; }

        private readonly IBindable<MarginPadding> safeAreaPadding = new BindableMarginPadding();

        public override ISnapTargetContainer SnapTarget => safeAreaTargetContainer;

        /// <summary>
        /// The <see cref="Edges"/> that should be automatically padded based on the target's <see cref="SafeAreaTargetContainer{T}.SafeAreaPadding"/>
        /// Defaults to <see cref="Edges.All"/>. May not share edges with <see cref="EdgeSnappingContainer{T}.SnappedEdges"/>.
        /// </summary>
        public Edges SafeEdges { get; set; } = Edges.All;

        [BackgroundDependencyLoader]
        private void load()
        {
            safeAreaPadding.ValueChanged += _ => UpdatePadding();
            safeAreaPadding.BindTo(safeAreaTargetContainer.SafeAreaPadding);
        }

        protected override MarginPadding SnappedPadding()
        {
            if ((SafeEdges & SnappedEdges) != Edges.None)
                throw new InvalidOperationException($"A {nameof(SafeAreaSnappingContainer)}'s {nameof(SafeEdges)} may not share edges with its {nameof(SnappedEdges)}.");

            MarginPadding basePadding = base.SnappedPadding();

            if (SnapTarget == null)
                return basePadding;

            var snapRect = SnapTarget.SnapRectangle;
            var safeArea = safeAreaPadding.Value;
            var paddedRect = new Quad(snapRect.X + safeArea.Left, snapRect.Y + safeArea.Top, snapRect.Width - safeArea.TotalHorizontal, snapRect.Height - safeArea.TotalVertical);
            var localRect = ToLocalSpace(paddedRect);

            return new MarginPadding
            {
                Left = SafeEdges.HasFlag(Edges.Left) ? Math.Max(0, localRect.TopLeft.X) : basePadding.Left,
                Right = SafeEdges.HasFlag(Edges.Right) ? Math.Max(0, DrawSize.X - localRect.BottomRight.X) : basePadding.Right,
                Top = SafeEdges.HasFlag(Edges.Top) ? Math.Max(0, localRect.TopLeft.Y) : basePadding.Top,
                Bottom = SafeEdges.HasFlag(Edges.Bottom) ? Math.Max(0, DrawSize.Y - localRect.BottomRight.Y) : basePadding.Bottom
            };
        }
    }
}
