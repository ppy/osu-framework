// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Platform;

namespace osu.Framework.Graphics.Containers
{
    public class SafeAreaContainer : SafeAreaContainer<Drawable>
    {
    }

    /// <summary>
    /// A <see cref="SnapTargetContainer{T}"/> that will dynamically apply a padding based on the current <see cref="GameWindow.SafeAreaPadding"/> value.
    /// Padding will only be applied if the contents of the container would otherwise intersect the safe area margins of the host <see cref="GameWindow"/>.
    /// Padding may be applied to individual edges by setting the <see cref="AppliedEdges"/> property.
    /// </summary>
    public class SafeAreaContainer<T> : SnapTargetContainer<T>
        where T : Drawable
    {
        [Resolved]
        private GameHost host { get; set; }

        private readonly IBindable<MarginPadding> safeAreaPadding = new BindableMarginPadding();

        private Edges appliedEdges = Edges.All;

        /// <summary>
        /// The <see cref="Edges"/> that should be automatically padded based on the current <see cref="GameWindow.SafeAreaPadding"/>.
        /// Defaults to <see cref="Edges.All"/>.
        /// </summary>
        public Edges AppliedEdges
        {
            get => appliedEdges;
            set
            {
                if (appliedEdges.Equals(value))
                    return;

                appliedEdges = value;

                updatePadding();
            }
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            safeAreaPadding.BindTo(host.Window.SafeAreaPadding);
        }

        public SafeAreaContainer()
        {
            RelativeSizeAxes = Axes.Both;
            safeAreaPadding.ValueChanged += _ => updatePadding();
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();
            updatePadding();
        }

        private void updatePadding()
        {
            if (host?.Window == null)
                return;

            var clientRect = host.Window.ClientRectangle;
            var safeArea = safeAreaPadding.Value;
            var paddedRect = new Quad(clientRect.X + safeArea.Left, clientRect.Y + safeArea.Top, clientRect.Width - safeArea.TotalHorizontal, clientRect.Height - safeArea.TotalVertical);
            var localRect = ToLocalSpace(paddedRect);

            Padding = new MarginPadding
            {
                Left = AppliedEdges.HasFlag(Edges.Left) ? Math.Max(0, localRect.TopLeft.X) : 0,
                Right = AppliedEdges.HasFlag(Edges.Right) ? Math.Max(0, DrawSize.X - localRect.BottomRight.X) : 0,
                Top = AppliedEdges.HasFlag(Edges.Top) ? Math.Max(0, localRect.TopLeft.Y) : 0,
                Bottom = AppliedEdges.HasFlag(Edges.Bottom) ? Math.Max(0, DrawSize.Y - localRect.BottomRight.Y) : 0
            };
        }
    }
}
