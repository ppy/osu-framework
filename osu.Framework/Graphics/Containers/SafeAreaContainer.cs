// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// An <see cref="EdgeSnappingContainer"/> that can also apply a padding based on the safe areas of the nearest cached <see cref="SafeAreaTargetContainer"/>.
    /// Padding will only be applied if the contents of this container would otherwise intersect the safe area margins relative to the associated target container.
    /// Padding may be applied to individual edges by setting the <see cref="SafeEdges"/> property.
    /// </summary>
    public class SafeAreaContainer : EdgeSnappingContainer
    {
        [Resolved]
        private SafeAreaTargetContainer safeAreaTargetContainer { get; set; }

        private readonly BindableSafeArea safeAreaPadding = new BindableSafeArea();

        public override ISnapTargetContainer SnapTarget => safeAreaTargetContainer;

        /// <summary>
        /// The <see cref="Edges"/> that should be automatically padded based on the target's <see cref="SafeAreaTargetContainer.SafeAreaPadding"/>
        /// Defaults to <see cref="Edges.All"/>. May not share edges with <see cref="EdgeSnappingContainer.SnappedEdges"/>.
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
                throw new InvalidOperationException($"A {nameof(SafeAreaContainer)}'s {nameof(SafeEdges)} may not share edges with its {nameof(SnappedEdges)}.");

            MarginPadding basePadding = base.SnappedPadding();

            if (SnapTarget == null)
                return basePadding;

            var snapRect = SnapTarget.SnapRectangle;
            var safeArea = safeAreaPadding.Value;
            var paddedRect = new RectangleF(snapRect.X + safeArea.Left, snapRect.Y + safeArea.Top, snapRect.Width - safeArea.TotalHorizontal, snapRect.Height - safeArea.TotalVertical);
            var localTopLeft = SnapTarget.ToSpaceOfOtherDrawable(paddedRect.TopLeft, this);
            var localBottomRight = SnapTarget.ToSpaceOfOtherDrawable(paddedRect.BottomRight, this);

            return new MarginPadding
            {
                Left = SafeEdges.HasFlag(Edges.Left) ? Math.Max(0, localTopLeft.X) : basePadding.Left,
                Right = SafeEdges.HasFlag(Edges.Right) ? Math.Max(0, DrawSize.X - localBottomRight.X) : basePadding.Right,
                Top = SafeEdges.HasFlag(Edges.Top) ? Math.Max(0, localTopLeft.Y) : basePadding.Top,
                Bottom = SafeEdges.HasFlag(Edges.Bottom) ? Math.Max(0, DrawSize.Y - localBottomRight.Y) : basePadding.Bottom
            };
        }
    }
}
