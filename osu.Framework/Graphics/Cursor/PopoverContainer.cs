// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Extensions;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Utils;
using osuTK;

namespace osu.Framework.Graphics.Cursor
{
    public class PopoverContainer : Container
    {
        private readonly Container content;
        private readonly Container dismissOnMouseDownContainer;

        private IHasPopover? target;
        private Popover? currentPopover;

        protected override Container<Drawable> Content => content;

        public PopoverContainer()
        {
            InternalChildren = new Drawable[]
            {
                content = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                },
                dismissOnMouseDownContainer = new DismissOnMouseDownContainer
                {
                    RelativeSizeAxes = Axes.Both
                }
            };
        }

        /// <summary>
        /// Sets the target drawable for this <see cref="PopoverContainer"/> to <paramref name="newTarget"/>.
        /// </summary>
        /// <remarks>
        /// After calling this method, the previous popover shown in this <see cref="PopoverContainer"/> will be hidden.
        /// This method can be called with a <see langword="null"/> argument to hide the currently-visible popover.
        /// </remarks>
        /// <returns><see langword="true"/> if a new popover was shown, <see langword="false"/> otherwise.</returns>
        internal bool SetTarget(IHasPopover? newTarget)
        {
            currentPopover?.Hide();
            currentPopover?.Expire();

            target = newTarget;

            var newPopover = target?.GetPopover();
            if (newPopover == null)
                return false;

            dismissOnMouseDownContainer.Add(currentPopover = newPopover);
            currentPopover.Show();
            return true;
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            if ((target as Drawable)?.FindClosestParent<PopoverContainer>() != this || target?.IsPresent != true)
            {
                SetTarget(null);
                return;
            }

            updatePopoverPositioning();
        }

        protected override void OnSizingChanged()
        {
            base.OnSizingChanged();

            // reset to none to prevent exceptions
            content.RelativeSizeAxes = Axes.None;
            content.AutoSizeAxes = Axes.None;

            // in addition to using this.RelativeSizeAxes, sets RelativeSizeAxes on every axis that is neither relative size nor auto size
            content.RelativeSizeAxes = Axes.Both & ~AutoSizeAxes;
            content.AutoSizeAxes = AutoSizeAxes;
        }

        /// <summary>
        /// The <see cref="Anchor"/>s to consider when auto-layouting the popover.
        /// <see cref="Anchor.Centre"/> is not included, as it is used as a fallback if any other anchor fails.
        /// </summary>
        private static readonly Anchor[] candidate_anchors =
        {
            Anchor.TopLeft,
            Anchor.TopCentre,
            Anchor.TopRight,
            Anchor.CentreLeft,
            Anchor.CentreRight,
            Anchor.BottomLeft,
            Anchor.BottomCentre,
            Anchor.BottomRight
        };

        private void updatePopoverPositioning()
        {
            if (target == null || currentPopover == null)
                return;

            var targetLocalQuad = ToLocalSpace(target.ScreenSpaceDrawQuad);

            Anchor bestAnchor = Anchor.Centre;
            float biggestArea = 0;

            float totalSize = Math.Max(DrawSize.X * DrawSize.Y, 1);

            foreach (var anchor in candidate_anchors)
            {
                // Compute how much free space is available on this side of the target.
                var availableSize = availableSizeAroundTargetForAnchor(targetLocalQuad, anchor);
                float area = availableSize.X * availableSize.Y / totalSize;

                // If the free space is insufficient for the popover to fit in, do not consider this anchor further.
                if (availableSize.X < currentPopover.BoundingBoxContainer.DrawWidth || availableSize.Y < currentPopover.BoundingBoxContainer.DrawHeight)
                    continue;

                // The heuristic used to find the "best" anchor is the biggest area of free space available in the popover container
                // on the side of the anchor.
                if (Precision.DefinitelyBigger(area, biggestArea, 0.01f))
                {
                    biggestArea = area;
                    bestAnchor = anchor;
                }
            }

            currentPopover.PopoverAnchor = bestAnchor.Opposite();

            var positionOnQuad = bestAnchor.PositionOnQuad(targetLocalQuad);
            currentPopover.Position = new Vector2(positionOnQuad.X - Padding.Left, positionOnQuad.Y - Padding.Top);

            // While the side has been chosen to maximise the area of free space available, that doesn't mean that the popover's body
            // will still fit in its entirety in the default configuration.
            // To avoid this, offset the popover so that it fits in the bounds of this container.
            var adjustment = new Vector2();

            var popoverContentLocalQuad = ToLocalSpace(currentPopover.Body.ScreenSpaceDrawQuad);
            if (popoverContentLocalQuad.TopLeft.X < 0)
                adjustment.X = -popoverContentLocalQuad.TopLeft.X;
            else if (popoverContentLocalQuad.BottomRight.X > DrawWidth)
                adjustment.X = DrawWidth - popoverContentLocalQuad.BottomRight.X;
            if (popoverContentLocalQuad.TopLeft.Y < 0)
                adjustment.Y = -popoverContentLocalQuad.TopLeft.Y;
            else if (popoverContentLocalQuad.BottomRight.Y > DrawHeight)
                adjustment.Y = DrawHeight - popoverContentLocalQuad.BottomRight.Y;

            currentPopover.Position += adjustment;

            // Even if the popover was moved, the arrow should stay fixed in place and point at the target's centre.
            // In such a case, apply a counter-adjustment to the arrow position.
            // The reason why just the body isn't moved is that the popover's autosize does not play well with that
            // (setting X/Y on the body can lead BoundingBox to be larger than it actually needs to be, causing 1-frame-errors)
            currentPopover.Arrow.Position = -adjustment;
        }

        /// <summary>
        /// Computes the available size around the <paramref name="targetLocalQuad"/> on the side of it indicated by <paramref name="anchor"/>
        /// </summary>
        private Vector2 availableSizeAroundTargetForAnchor(Quad targetLocalQuad, Anchor anchor)
        {
            Vector2 availableSize = new Vector2();

            // left anchor = area to the left of the quad, right anchor = area to the right of the quad.
            // for horizontal centre assume we have the whole quad width to work with.
            if (anchor.HasFlagFast(Anchor.x0))
                availableSize.X = MathF.Max(0, targetLocalQuad.TopLeft.X);
            else if (anchor.HasFlagFast(Anchor.x2))
                availableSize.X = MathF.Max(0, DrawWidth - targetLocalQuad.BottomRight.X);
            else
                availableSize.X = DrawWidth;

            // top anchor = area above quad, bottom anchor = area below quad.
            // for vertical centre assume we have the whole quad height to work with.
            if (anchor.HasFlagFast(Anchor.y0))
                availableSize.Y = MathF.Max(0, targetLocalQuad.TopLeft.Y);
            else if (anchor.HasFlagFast(Anchor.y2))
                availableSize.Y = MathF.Max(0, DrawHeight - targetLocalQuad.BottomRight.Y);
            else
                availableSize.Y = DrawHeight;

            // the final size is the intersection of the X/Y areas.
            return availableSize;
        }

        private class DismissOnMouseDownContainer : Container
        {
            protected override bool OnMouseDown(MouseDownEvent e)
            {
                this.HidePopover();
                return false;
            }
        }
    }
}
