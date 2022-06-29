// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Diagnostics;
using osu.Framework.Caching;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Layout;
using osu.Framework.Utils;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Graphics.Containers
{
    public abstract class ScrollContainer<T> : Container<T>, IScrollContainer, DelayedLoadWrapper.IOnScreenOptimisingContainer, IKeyBindingHandler<PlatformAction>
        where T : Drawable
    {
        /// <summary>
        /// Determines whether the scroll dragger appears on the left side. If not, then it always appears on the right side.
        /// </summary>
        public Anchor ScrollbarAnchor
        {
            get => Scrollbar.Anchor;
            set
            {
                Scrollbar.Anchor = value;
                Scrollbar.Origin = value;
                updatePadding();
            }
        }

        private bool scrollbarVisible = true;

        /// <summary>
        /// Whether the scrollbar is visible.
        /// </summary>
        public bool ScrollbarVisible
        {
            get => scrollbarVisible;
            set
            {
                scrollbarVisible = value;
                scrollbarCache.Invalidate();
            }
        }

        protected readonly ScrollbarContainer Scrollbar;

        private bool scrollbarOverlapsContent = true;

        /// <summary>
        /// Whether the scrollbar overlaps the content or resides in its own padded space.
        /// </summary>
        public bool ScrollbarOverlapsContent
        {
            get => scrollbarOverlapsContent;
            set
            {
                scrollbarOverlapsContent = value;
                updatePadding();
            }
        }

        /// <summary>
        /// Size of available content (i.e. everything that can be scrolled to) in the scroll direction.
        /// </summary>
        public float AvailableContent => ScrollContent.DrawSize[ScrollDim];

        /// <summary>
        /// Size of the viewport in the scroll direction.
        /// </summary>
        public float DisplayableContent => ChildSize[ScrollDim];

        /// <summary>
        /// Controls the distance scrolled per unit of mouse scroll.
        /// </summary>
        public float ScrollDistance = 80;

        /// <summary>
        /// This limits how far out of clamping bounds we allow the target position to be at most.
        /// Effectively, larger values result in bouncier behavior as the scroll boundaries are approached
        /// with high velocity.
        /// </summary>
        public float ClampExtension = 500;

        /// <summary>
        /// This corresponds to the clamping force. A larger value means more aggressive clamping. Default is 0.012.
        /// </summary>
        private const double distance_decay_clamping = 0.012;

        /// <summary>
        /// Controls the rate with which the target position is approached after ending a drag. Default is 0.0035.
        /// </summary>
        public double DistanceDecayDrag = 0.0035;

        /// <summary>
        /// Controls the rate with which the target position is approached after scrolling. Default is 0.01
        /// </summary>
        public double DistanceDecayScroll = 0.01;

        /// <summary>
        /// Controls the rate with which the target position is approached after jumping to a specific location. Default is 0.01.
        /// </summary>
        public double DistanceDecayJump = 0.01;

        /// <summary>
        /// Controls the rate with which the target position is approached. It is automatically set after
        /// dragging or scrolling.
        /// </summary>
        private double distanceDecay;

        /// <summary>
        /// The current scroll position.
        /// </summary>
        public float Current { get; private set; }

        /// <summary>
        /// The target scroll position which is exponentially approached by current via a rate of distanceDecay.
        /// </summary>
        protected float Target { get; private set; }

        /// <summary>
        /// The maximum distance that can be scrolled in the scroll direction.
        /// </summary>
        public float ScrollableExtent => Math.Max(AvailableContent - DisplayableContent, 0);

        /// <summary>
        /// The maximum distance that the scrollbar can move in the scroll direction.
        /// </summary>
        public float ScrollbarMovementExtent => Math.Max(DisplayableContent - Scrollbar.DrawSize[ScrollDim], 0);

        /// <summary>
        /// Clamp a value to the available scroll range.
        /// </summary>
        /// <param name="position">The value to clamp.</param>
        /// <param name="extension">An extension value beyond the normal extent.</param>
        protected float Clamp(float position, float extension = 0) => Math.Max(Math.Min(position, ScrollableExtent + extension), -extension);

        protected override Container<T> Content => ScrollContent;

        /// <summary>
        /// Whether we are currently scrolled as far as possible into the scroll direction.
        /// </summary>
        /// <param name="lenience">How close to the extent we need to be.</param>
        public bool IsScrolledToEnd(float lenience = Precision.FLOAT_EPSILON) => Precision.AlmostBigger(Target, ScrollableExtent, lenience);

        /// <summary>
        /// The container holding all children which are getting scrolled around.
        /// </summary>
        public Container<T> ScrollContent { get; }

        protected virtual bool IsDragging { get; private set; }

        public bool IsHandlingKeyboardScrolling
        {
            get
            {
                if (IsHovered)
                    return true;

                InputManager inputManager = GetContainingInputManager();
                return inputManager != null && ReceivePositionalInputAt(inputManager.CurrentState.Mouse.Position);
            }
        }

        public Direction ScrollDirection { get; }

        /// <summary>
        /// The direction in which scrolling is supported, converted to an int for array index lookups.
        /// </summary>
        protected int ScrollDim => ScrollDirection == Direction.Horizontal ? 0 : 1;

        private readonly LayoutValue<IScrollContainer> parentScrollContainerCache = new LayoutValue<IScrollContainer>(Invalidation.Parent);

        private IScrollContainer parentScrollContainer => parentScrollContainerCache.IsValid
            ? parentScrollContainerCache.Value
            : parentScrollContainerCache.Value = this.FindClosestParent<IScrollContainer>();

        /// <summary>
        /// Creates a scroll container.
        /// </summary>
        /// <param name="scrollDirection">The direction in which should be scrolled. Can be vertical or horizontal. Default is vertical.</param>
        protected ScrollContainer(Direction scrollDirection = Direction.Vertical)
        {
            ScrollDirection = scrollDirection;

            Masking = true;

            Axes scrollAxis = scrollDirection == Direction.Horizontal ? Axes.X : Axes.Y;
            AddRangeInternal(new Drawable[]
            {
                ScrollContent = new Container<T>
                {
                    RelativeSizeAxes = Axes.Both & ~scrollAxis,
                    AutoSizeAxes = scrollAxis,
                },
                Scrollbar = CreateScrollbar(scrollDirection)
            });

            Scrollbar.Hide();
            Scrollbar.Dragged = onScrollbarMovement;
            ScrollbarAnchor = scrollDirection == Direction.Vertical ? Anchor.TopRight : Anchor.BottomLeft;

            AddLayout(parentScrollContainerCache);
        }

        private float lastUpdateDisplayableContent = -1;
        private float lastAvailableContent = -1;

        private void updateSize()
        {
            // ensure we only update scrollbar when something has changed, to avoid transform helpers resetting their transform every frame.
            // also avoids creating many needless Transforms every update frame.
            if (lastAvailableContent != AvailableContent || lastUpdateDisplayableContent != DisplayableContent)
            {
                lastAvailableContent = AvailableContent;
                lastUpdateDisplayableContent = DisplayableContent;
                scrollbarCache.Invalidate();
            }
        }

        private readonly Cached scrollbarCache = new Cached();

        private void updatePadding()
        {
            if (scrollbarOverlapsContent || AvailableContent <= DisplayableContent)
                ScrollContent.Padding = new MarginPadding();
            else
            {
                if (ScrollDirection == Direction.Vertical)
                {
                    ScrollContent.Padding = ScrollbarAnchor == Anchor.TopLeft
                        ? new MarginPadding { Left = Scrollbar.Width + Scrollbar.Margin.Left }
                        : new MarginPadding { Right = Scrollbar.Width + Scrollbar.Margin.Right };
                }
                else
                {
                    ScrollContent.Padding = ScrollbarAnchor == Anchor.TopLeft
                        ? new MarginPadding { Top = Scrollbar.Height + Scrollbar.Margin.Top }
                        : new MarginPadding { Bottom = Scrollbar.Height + Scrollbar.Margin.Bottom };
                }
            }
        }

        protected override bool OnDragStart(DragStartEvent e)
        {
            if (IsDragging || e.Button != MouseButton.Left || Content.AliveInternalChildren.Count == 0)
                return false;

            if (parentScrollContainer != null && parentScrollContainer.ScrollDirection != ScrollDirection)
            {
                bool dragWasMostlyHorizontal = Math.Abs(e.Delta.X) > Math.Abs(e.Delta.Y);
                if (dragWasMostlyHorizontal != (ScrollDirection == Direction.Horizontal))
                    return false;
            }

            lastDragTime = Time.Current;
            averageDragDelta = averageDragTime = 0;

            IsDragging = true;

            dragButtonManager = GetContainingInputManager().GetButtonEventManagerFor(e.Button);

            return true;
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (IsHandlingKeyboardScrolling && !IsDragging)
            {
                switch (e.Key)
                {
                    case Key.PageUp:
                        OnUserScroll(Target - DisplayableContent);
                        return true;

                    case Key.PageDown:
                        OnUserScroll(Target + DisplayableContent);
                        return true;
                }
            }

            return base.OnKeyDown(e);
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            if (IsDragging || e.Button != MouseButton.Left)
                return false;

            // Continue from where we currently are scrolled to.
            Target = Current;
            return false;
        }

        // We keep track of this because input events may happen at different intervals than update frames
        // and we are interested in the time difference between drag _input_ events.
        private double lastDragTime;

        // These keep track of a sliding average (w.r.t. time) of the time between drag events
        // and the delta of drag events. Both of these moving averages are decayed at the same
        // rate and thus the velocity remains constant across time. The overall magnitude
        // of averageDragTime and averageDragDelta simple decreases such that more recent movements
        // have a larger weight.
        private double averageDragTime;
        private double averageDragDelta;

        private MouseButtonEventManager dragButtonManager;

        private bool dragBlocksClick;

        public override bool DragBlocksClick => dragBlocksClick;

        protected override void OnDrag(DragEvent e)
        {
            Trace.Assert(IsDragging, "We should never receive OnDrag if we are not dragging.");

            double currentTime = Time.Current;
            double timeDelta = currentTime - lastDragTime;
            double decay = Math.Pow(0.95, timeDelta);

            averageDragTime = averageDragTime * decay + timeDelta;
            averageDragDelta = averageDragDelta * decay - e.Delta[ScrollDim];

            lastDragTime = currentTime;

            Vector2 childDelta = ToLocalSpace(e.ScreenSpaceMousePosition) - ToLocalSpace(e.ScreenSpaceLastMousePosition);

            float scrollOffset = -childDelta[ScrollDim];
            float clampedScrollOffset = Clamp(Target + scrollOffset) - Clamp(Target);

            Debug.Assert(Precision.AlmostBigger(Math.Abs(scrollOffset), clampedScrollOffset * Math.Sign(scrollOffset)));

            // If we are dragging past the extent of the scrollable area, half the offset
            // such that the user can feel it.
            scrollOffset = clampedScrollOffset + (scrollOffset - clampedScrollOffset) / 2;

            // similar calculation to what is already done in MouseButtonEventManager.HandlePositionChange
            // handles the case where a drag was triggered on an axis we are not interested in.
            // can be removed if/when drag events are split out per axis or contain direction information.
            dragBlocksClick |= Math.Abs(e.MouseDownPosition[ScrollDim] - e.MousePosition[ScrollDim]) > dragButtonManager.ClickDragDistance;

            scrollByOffset(scrollOffset, false);
        }

        protected override void OnDragEnd(DragEndEvent e)
        {
            Trace.Assert(IsDragging, "We should never receive OnDragEnd if we are not dragging.");

            dragBlocksClick = false;
            dragButtonManager = null;
            IsDragging = false;

            if (averageDragTime <= 0.0)
                return;

            double velocity = averageDragDelta / averageDragTime;

            // Detect whether we halted at the end of the drag and in fact should _not_
            // perform a flick event.
            const double velocity_cutoff = 0.1;
            if (Math.Abs(Math.Pow(0.95, Time.Current - lastDragTime) * velocity) < velocity_cutoff)
                velocity = 0;

            // Differentiate f(t) = distance * (1 - exp(-t)) w.r.t. "t" to obtain
            // velocity w.r.t. time. Then rearrange to solve for distance given velocity.
            double distance = velocity / (1 - Math.Exp(-DistanceDecayDrag));

            scrollByOffset((float)distance, true, DistanceDecayDrag);
        }

        protected override bool OnScroll(ScrollEvent e)
        {
            if (Content.AliveInternalChildren.Count == 0)
                return false;

            if (parentScrollContainer != null && parentScrollContainer.ScrollDirection != ScrollDirection)
            {
                bool scrollWasMostlyHorizontal = Math.Abs(e.ScrollDelta.X) > Math.Abs(e.ScrollDelta.Y);

                // For horizontal scrolling containers, vertical scroll is also used to perform horizontal traversal.
                // Due to this, we only block horizontal scroll in vertical containers, but not vice-versa.
                if (scrollWasMostlyHorizontal && ScrollDirection == Direction.Vertical)
                    return false;
            }

            bool isPrecise = e.IsPrecise;

            Vector2 scrollDelta = e.ScrollDelta;
            float scrollDeltaFloat = scrollDelta.Y;
            if (ScrollDirection == Direction.Horizontal && scrollDelta.X != 0)
                scrollDeltaFloat = scrollDelta.X;

            scrollByOffset(ScrollDistance * -scrollDeltaFloat, true, isPrecise ? 0.05 : DistanceDecayScroll);
            return true;
        }

        private void onScrollbarMovement(float value) => OnUserScroll(Clamp(fromScrollbarPosition(value)), false);

        /// <summary>
        /// Immediately offsets the current and target scroll position.
        /// </summary>
        /// <param name="offset">The scroll offset.</param>
        public void OffsetScrollPosition(float offset)
        {
            Target += offset;
            Current += offset;
        }

        private void scrollByOffset(float value, bool animated, double distanceDecay = float.PositiveInfinity) =>
            OnUserScroll(Target + value, animated, distanceDecay);

        /// <summary>
        /// Scroll to the start of available content.
        /// </summary>
        /// <param name="animated">Whether to animate the movement.</param>
        /// <param name="allowDuringDrag">Whether we should interrupt a user's active drag.</param>
        public void ScrollToStart(bool animated = true, bool allowDuringDrag = false)
        {
            if (!IsDragging || allowDuringDrag)
                scrollTo(0, animated, DistanceDecayJump);
        }

        /// <summary>
        /// Scroll to the end of available content.
        /// </summary>
        /// <param name="animated">Whether to animate the movement.</param>
        /// <param name="allowDuringDrag">Whether we should interrupt a user's active drag.</param>
        public void ScrollToEnd(bool animated = true, bool allowDuringDrag = false)
        {
            if (!IsDragging || allowDuringDrag)
                scrollTo(ScrollableExtent, animated, DistanceDecayJump);
        }

        /// <summary>
        /// Scrolls to a new position relative to the current scroll offset.
        /// </summary>
        /// <param name="offset">The amount by which we should scroll.</param>
        /// <param name="animated">Whether to animate the movement.</param>
        public void ScrollBy(float offset, bool animated = true) => scrollTo(Target + offset, animated);

        /// <summary>
        /// Handle a scroll to an absolute position from a user input.
        /// </summary>
        /// <param name="value">The position to scroll to.</param>
        /// <param name="animated">Whether to animate the movement.</param>
        /// <param name="distanceDecay">Controls the rate with which the target position is approached after jumping to a specific location. Default is <see cref="DistanceDecayJump"/>.</param>
        protected virtual void OnUserScroll(float value, bool animated = true, double? distanceDecay = null) =>
            ScrollTo(value, animated, distanceDecay);

        /// <summary>
        /// Scrolls to an absolute position.
        /// </summary>
        /// <param name="value">The position to scroll to.</param>
        /// <param name="animated">Whether to animate the movement.</param>
        /// <param name="distanceDecay">Controls the rate with which the target position is approached after jumping to a specific location. Default is <see cref="DistanceDecayJump"/>.</param>
        public void ScrollTo(float value, bool animated = true, double? distanceDecay = null) => scrollTo(value, animated, distanceDecay ?? DistanceDecayJump);

        private void scrollTo(float value, bool animated, double distanceDecay = float.PositiveInfinity)
        {
            Target = Clamp(value, ClampExtension);

            if (animated)
                this.distanceDecay = distanceDecay;
            else
                Current = Target;
        }

        /// <summary>
        /// Scrolls a <see cref="Drawable"/> to the top.
        /// </summary>
        /// <param name="d">The <see cref="Drawable"/> to scroll to.</param>
        /// <param name="animated">Whether to animate the movement.</param>
        public void ScrollTo(Drawable d, bool animated = true) => ScrollTo(GetChildPosInContent(d), animated);

        /// <summary>
        /// Scrolls a <see cref="Drawable"/> into view.
        /// </summary>
        /// <param name="d">The <see cref="Drawable"/> to scroll into view.</param>
        /// <param name="animated">Whether to animate the movement.</param>
        public void ScrollIntoView(Drawable d, bool animated = true)
        {
            float childPos0 = GetChildPosInContent(d);
            float childPos1 = GetChildPosInContent(d, d.DrawSize);

            float minPos = Math.Min(childPos0, childPos1);
            float maxPos = Math.Max(childPos0, childPos1);

            if (minPos < Current || (minPos > Current && d.DrawSize[ScrollDim] > DisplayableContent))
                ScrollTo(minPos, animated);
            else if (maxPos > Current + DisplayableContent)
                ScrollTo(maxPos - DisplayableContent, animated);
        }

        /// <summary>
        /// Determines the position of a child in the content.
        /// </summary>
        /// <param name="d">The child to get the position from.</param>
        /// <param name="offset">Positional offset in the child's space.</param>
        /// <returns>The position of the child.</returns>
        public float GetChildPosInContent(Drawable d, Vector2 offset) => d.ToSpaceOfOtherDrawable(offset, ScrollContent)[ScrollDim];

        /// <summary>
        /// Determines the position of a child in the content.
        /// </summary>
        /// <param name="d">The child to get the position from.</param>
        /// <returns>The position of the child.</returns>
        public float GetChildPosInContent(Drawable d) => GetChildPosInContent(d, Vector2.Zero);

        private void updatePosition()
        {
            double localDistanceDecay = distanceDecay;

            // If we are not currently dragging the content, and we have scrolled out of bounds,
            // then we should handle the clamping force. Note, that if the target is _within_
            // acceptable bounds, then we do not need special handling of the clamping force, as
            // we will naturally scroll back into acceptable bounds.
            if (!IsDragging && Current != Clamp(Current) && Target != Clamp(Target, -0.01f))
            {
                // Firstly, we want to limit how far out the target may go to limit overly bouncy
                // behaviour with extreme scroll velocities.
                Target = Clamp(Target, ClampExtension);

                // Secondly, we would like to quickly approach the target while we are out of bounds.
                // This is simulating a "strong" clamping force towards the target.
                if ((Current < Target && Target < 0) || (Current > Target && Target > ScrollableExtent))
                    localDistanceDecay = distance_decay_clamping * 2;

                // Lastly, we gradually nudge the target towards valid bounds.
                Target = (float)Interpolation.Lerp(Clamp(Target), Target, Math.Exp(-distance_decay_clamping * Time.Elapsed));

                float clampedTarget = Clamp(Target);
                if (Precision.AlmostEquals(clampedTarget, Target))
                    Target = clampedTarget;
            }

            // Exponential interpolation between the target and our current scroll position.
            Current = (float)Interpolation.Lerp(Target, Current, Math.Exp(-localDistanceDecay * Time.Elapsed));

            // This prevents us from entering the de-normalized range of floating point numbers when approaching target closely.
            if (Precision.AlmostEquals(Current, Target))
                Current = Target;
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            updateSize();
            updatePosition();

            if (!scrollbarCache.IsValid)
            {
                float size = ScrollDirection == Direction.Horizontal ? DrawWidth : DrawHeight;
                if (size > 0)
                    Scrollbar.ResizeTo(Math.Clamp(AvailableContent > 0 ? DisplayableContent / AvailableContent : 0, Math.Min(Scrollbar.MinimumDimSize / size, 1), 1), 200, Easing.OutQuint);
                Scrollbar.FadeTo(ScrollbarVisible && AvailableContent - 1 > DisplayableContent ? 1 : 0, 200);
                updatePadding();

                scrollbarCache.Validate();
            }

            if (ScrollDirection == Direction.Horizontal)
            {
                Scrollbar.X = toScrollbarPosition(Current);
                ScrollContent.X = -Current + ScrollableExtent * ScrollContent.RelativeAnchorPosition.X;
            }
            else
            {
                Scrollbar.Y = toScrollbarPosition(Current);
                ScrollContent.Y = -Current + ScrollableExtent * ScrollContent.RelativeAnchorPosition.Y;
            }
        }

        /// <summary>
        /// Converts a scroll position to a scrollbar position.
        /// </summary>
        /// <param name="scrollPosition">The absolute scroll position (e.g. <see cref="Current"/>).</param>
        /// <returns>The scrollbar position.</returns>
        private float toScrollbarPosition(float scrollPosition)
        {
            if (Precision.AlmostEquals(0, ScrollableExtent))
                return 0;

            return ScrollbarMovementExtent * (scrollPosition / ScrollableExtent);
        }

        /// <summary>
        /// Converts a scrollbar position to a scroll position.
        /// </summary>
        /// <param name="scrollbarPosition">The scrollbar position.</param>
        /// <returns>The absolute scroll position.</returns>
        private float fromScrollbarPosition(float scrollbarPosition)
        {
            if (Precision.AlmostEquals(0, ScrollbarMovementExtent))
                return 0;

            return ScrollableExtent * (scrollbarPosition / ScrollbarMovementExtent);
        }

        /// <summary>
        /// Creates the scrollbar for this <see cref="ScrollContainer{T}"/>.
        /// </summary>
        /// <param name="direction">The scrolling direction.</param>
        protected abstract ScrollbarContainer CreateScrollbar(Direction direction);

        protected internal abstract class ScrollbarContainer : Container
        {
            private float dragOffset;

            internal Action<float> Dragged;

            protected readonly Direction ScrollDirection;

            /// <summary>
            /// The minimum size of this <see cref="ScrollbarContainer"/>. Defaults to the size in the non-scrolling direction.
            /// </summary>
            protected internal virtual float MinimumDimSize => Size[ScrollDirection == Direction.Vertical ? 0 : 1];

            protected ScrollbarContainer(Direction direction)
            {
                ScrollDirection = direction;

                RelativeSizeAxes = direction == Direction.Horizontal ? Axes.X : Axes.Y;
            }

            public abstract void ResizeTo(float val, int duration = 0, Easing easing = Easing.None);

            protected override bool OnClick(ClickEvent e) => true;

            protected override bool OnDragStart(DragStartEvent e)
            {
                if (e.Button != MouseButton.Left) return false;

                dragOffset = e.MousePosition[(int)ScrollDirection] - Position[(int)ScrollDirection];
                return true;
            }

            protected override bool OnMouseDown(MouseDownEvent e)
            {
                if (e.Button != MouseButton.Left) return false;

                dragOffset = Position[(int)ScrollDirection];
                Dragged?.Invoke(dragOffset);
                return true;
            }

            protected override void OnDrag(DragEvent e)
            {
                Dragged?.Invoke(e.MousePosition[(int)ScrollDirection] - dragOffset);
            }
        }

        public bool OnPressed(KeyBindingPressEvent<PlatformAction> e)
        {
            if (!IsHandlingKeyboardScrolling)
                return false;

            switch (e.Action)
            {
                case PlatformAction.MoveBackwardLine:
                    ScrollToStart();
                    return true;

                case PlatformAction.MoveForwardLine:
                    ScrollToEnd();
                    return true;

                default:
                    return false;
            }
        }

        public void OnReleased(KeyBindingReleaseEvent<PlatformAction> e)
        {
        }
    }
}
