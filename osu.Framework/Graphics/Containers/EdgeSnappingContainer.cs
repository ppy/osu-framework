// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A <see cref="Container"/> that will apply negative <see cref="Container.Padding"/> to snap the edges to
    /// the nearest cached <see cref="ISnapTargetContainer"/>.
    /// Individual <see cref="Edges"/> may be snapped by setting the <see cref="SnappedEdges"/> property.
    /// Note that children within the negative padding will not be drawn if a parent <see cref="Drawable"/>
    /// has <see cref="CompositeDrawable.Masking"/> set to true.
    /// </summary>
    public class EdgeSnappingContainer : Container<Drawable>
    {
        [Resolved(CanBeNull = true)]
        private ISnapTargetContainer snapTargetContainer { get; set; }

        public virtual ISnapTargetContainer SnapTarget => snapTargetContainer;

        /// <summary>
        /// The <see cref="Edges"/> that should be snapped to the nearest <see cref="ISnapTargetContainer"/>.
        /// Defaults to <see cref="Edges.None"/>.
        /// </summary>
        public Edges SnappedEdges { get; set; } = Edges.None;

        protected override void UpdateAfterChildrenLife()
        {
            base.UpdateAfterChildrenLife();
            UpdatePadding();
        }

        protected void UpdatePadding() => Padding = SnappedPadding();

        protected virtual MarginPadding SnappedPadding()
        {
            if (SnapTarget == null)
                return new MarginPadding();

            var rect = SnapTarget.SnapRectangleToSpaceOfOtherDrawable(this);
            var left = SnappedEdges.HasFlag(Edges.Left) ? rect.TopLeft.X : 0;
            var top = SnappedEdges.HasFlag(Edges.Top) ? rect.TopLeft.Y : 0;
            var right = SnappedEdges.HasFlag(Edges.Right) ? DrawRectangle.BottomRight.X - rect.BottomRight.X : 0;
            var bottom = SnappedEdges.HasFlag(Edges.Bottom) ? DrawRectangle.BottomRight.Y - rect.BottomRight.Y : 0;

            return new MarginPadding { Left = left, Right = right, Top = top, Bottom = bottom };
        }
    }
}
