// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Platform;

namespace osu.Framework.Graphics.Containers
{
    public class SafeAreaContainer : SafeAreaContainer<Drawable>
    {
    }

    /// <summary>
    /// A <see cref="SnapTargetContainer{T}"/> that will dynamically apply a padding based on the current <see cref="GameWindow.SafeAreaPadding"/> value.
    /// It should only be used near the top of the scene graph such that its <see cref="Drawable.DrawRectangle"/> fills the screen,
    /// otherwise the applied padding will be inaccurate for the host device.
    /// Padding may be applied to individual edges by setting the <see cref="AppliedEdges"/> property.
    /// </summary>
    public class SafeAreaContainer<T> : SnapTargetContainer<T>
        where T : Drawable
    {
        [Resolved]
        private GameHost host { get; set; }

        private readonly BindableMarginPadding safeAreaPadding = new BindableMarginPadding();

        private Edges appliedEdges = Edges.All;

        /// <summary>
        /// The <see cref="Edges"/> that should be automatically padded based on the current <see cref="GameWindow.SafeAreaPadding"/>.
        /// </summary>
        public Edges AppliedEdges
        {
            get => appliedEdges;
            set
            {
                if (appliedEdges.Equals(value))
                    return;

                appliedEdges = value;

                updatePadding(safeAreaPadding.Value);
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
            safeAreaPadding.ValueChanged += args => updatePadding(args.NewValue);
        }

        private void updatePadding(MarginPadding padding)
        {
            Padding = new MarginPadding
            {
                Left = AppliedEdges.HasFlag(Edges.Left) ? padding.Left : 0,
                Right = AppliedEdges.HasFlag(Edges.Right) ? padding.Right : 0,
                Top = AppliedEdges.HasFlag(Edges.Top) ? padding.Top : 0,
                Bottom = AppliedEdges.HasFlag(Edges.Bottom) ? padding.Bottom : 0
            };
        }
    }
}
