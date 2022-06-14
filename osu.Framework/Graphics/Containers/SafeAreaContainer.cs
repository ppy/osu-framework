// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Layout;
using osuTK;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// An <see cref="Container"/> that can also apply a padding based on the safe areas of the nearest cached <see cref="SafeAreaDefiningContainer"/>.
    /// Padding will only be applied if the contents of this container would otherwise intersect the safe area margins relative to the associated target container.
    /// </summary>
    public class SafeAreaContainer : Container
    {
        private readonly BindableSafeArea safeAreaPadding = new BindableSafeArea();

        public SafeAreaContainer()
        {
            AddLayout(PaddingCache);
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            safeAreaPadding.ValueChanged += _ => PaddingCache.Invalidate();
            safeAreaPadding.BindTo(safeArea.SafeAreaPadding);
        }

        [Resolved(CanBeNull = true)]
        private ISafeArea safeArea { get; set; }

        /// <summary>
        /// The <see cref="Edges"/> that should bypass the defined <see cref="ISafeArea" /> to bleed to the screen edge.
        /// Defaults to <see cref="Edges.None"/>.
        /// </summary>
        public Edges SafeAreaOverrideEdges
        {
            get => safeAreaOverrideEdges;
            set
            {
                safeAreaOverrideEdges = value;
                PaddingCache.Invalidate();
            }
        }

        private Edges safeAreaOverrideEdges = Edges.None;

        protected readonly LayoutValue PaddingCache = new LayoutValue(Invalidation.DrawInfo | Invalidation.RequiredParentSizeToFit);

        protected override void UpdateAfterChildrenLife()
        {
            base.UpdateAfterChildrenLife();

            if (!PaddingCache.IsValid)
            {
                Padding = getPadding();
                PaddingCache.Validate();
            }
        }

        private MarginPadding getPadding()
        {
            if (safeArea == null)
                return new MarginPadding();

            var nonSafeLocalSpace = safeArea.ExpandRectangleToSpaceOfOtherDrawable(this);

            var nonSafe = safeArea.AvailableNonSafeSpace;

            var paddedRect = new RectangleF(nonSafe.X + safeAreaPadding.Value.Left, nonSafe.Y + safeAreaPadding.Value.Top, nonSafe.Width - safeAreaPadding.Value.TotalHorizontal, nonSafe.Height - safeAreaPadding.Value.TotalVertical);
            var localTopLeft = safeArea.ToSpaceOfOtherDrawable(paddedRect.TopLeft, this);
            var localBottomRight = safeArea.ToSpaceOfOtherDrawable(paddedRect.BottomRight, this);

            Vector2 absDrawSize = new Vector2(Math.Abs(DrawWidth), Math.Abs(DrawHeight));

            return new MarginPadding
            {
                Left = SafeAreaOverrideEdges.HasFlagFast(Edges.Left) ? nonSafeLocalSpace.TopLeft.X : Math.Clamp(localTopLeft.X, 0, absDrawSize.X),
                Right = SafeAreaOverrideEdges.HasFlagFast(Edges.Right) ? DrawRectangle.BottomRight.X - nonSafeLocalSpace.BottomRight.X : Math.Clamp(absDrawSize.X - localBottomRight.X, 0, absDrawSize.X),
                Top = SafeAreaOverrideEdges.HasFlagFast(Edges.Top) ? nonSafeLocalSpace.TopLeft.Y : Math.Clamp(localTopLeft.Y, 0, absDrawSize.Y),
                Bottom = SafeAreaOverrideEdges.HasFlagFast(Edges.Bottom) ? DrawRectangle.BottomRight.Y - nonSafeLocalSpace.BottomRight.Y : Math.Clamp(absDrawSize.Y - localBottomRight.Y, 0, absDrawSize.Y)
            };
        }
    }
}
