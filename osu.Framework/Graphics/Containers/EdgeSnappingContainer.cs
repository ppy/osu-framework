// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;

namespace osu.Framework.Graphics.Containers
{
    public class EdgeSnappingContainer : EdgeSnappingContainer<Drawable>
    {
    }

    /// <summary>
    /// A <see cref="Container{T}"/> that will apply negative <see cref="Container{T}.Padding"/> to snap the edges to
    /// the nearest cached <see cref="ISnapTargetContainer"/>.
    /// Individual <see cref="Edges"/> may be snapped by setting the <see cref="SnappedEdges"/> property.
    /// This class is most useful when used in conjunction with <see cref="SafeAreaContainer{T}"/> to ensure UI elements are
    /// not hidden underneath inaccessible areas of the screen on mobile devices.
    /// Note that children within the negative padding will not be drawn if a parent <see cref="Drawable"/>
    /// has <see cref="CompositeDrawable.Masking"/> set to true.
    /// </summary>
    public class EdgeSnappingContainer<T> : Container<T>
        where T : Drawable
    {
        [Resolved(CanBeNull = true)]
        private ISnapTargetContainer snapTargetContainer { get; set; }

        private Edges snappedEdges = Edges.None;

        /// <summary>
        /// The <see cref="Edges"/> that should be snapped to the nearest <see cref="ISnapTargetContainer"/>.
        /// Defaults to <see cref="Edges.None"/>.
        /// </summary>
        public Edges SnappedEdges
        {
            get => snappedEdges;
            set
            {
                if (snappedEdges.Equals(value))
                    return;

                snappedEdges = value;

                updatePadding();
            }
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();
            updatePadding();
        }

        private void updatePadding()
        {
            if (snapTargetContainer == null)
            {
                Padding = new MarginPadding();
                return;
            }

            var rect = snapTargetContainer.SnapRectangleToSpaceOfOtherDrawable(this);
            var left = SnappedEdges.HasFlag(Edges.Left) ? rect.TopLeft.X : 0;
            var top = SnappedEdges.HasFlag(Edges.Top) ? rect.TopLeft.Y : 0;
            var right = SnappedEdges.HasFlag(Edges.Right) ? DrawRectangle.BottomRight.X - rect.BottomRight.X : 0;
            var bottom = SnappedEdges.HasFlag(Edges.Bottom) ? DrawRectangle.BottomRight.Y - rect.BottomRight.Y : 0;

            Padding = new MarginPadding { Left = left, Right = right, Top = top, Bottom = bottom };
        }
    }
}
