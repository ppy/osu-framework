// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Diagnostics;
using osu.Framework.Input;
using osu.Framework.MathUtils;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics.Shapes;
using OpenTK.Input;

namespace osu.Framework.Graphics.Containers
{
    public class ScrollContainer : ScrollContainer<Drawable>
    {
        /// <summary>
        /// Creates a scroll container.
        /// </summary>
        /// <param name="scrollDirection">The direction in which should be scrolled. Can be vertical or horizontal. Default is vertical.</param>
        public ScrollContainer(Direction scrollDirection = Direction.Vertical) : base(scrollDirection)
        {
        }
    }

    public class ScrollContainer<T> : Container<T>, DelayedLoadWrapper.IOnScreenOptimisingContainer
        where T : Drawable
    {
        /// <summary>
        /// Determines whether the scroll dragger appears on the left side. If not, then it always appears on the right side.
        /// </summary>
        public Anchor ScrollbarAnchor
        {
            get { return Scrollbar.Anchor; }

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
            get { return scrollbarVisible; }
            set
            {
                scrollbarVisible = value;
                updateScrollbar();
            }
        }

        private readonly Container<T> content;

        protected readonly ScrollbarContainer Scrollbar;

        private bool scrollbarOverlapsContent = true;

        /// <summary>
        /// Whether the scrollbar overlaps the content or resides in its own padded space.
        /// </summary>
        public bool ScrollbarOverlapsContent
        {
            get { return scrollbarOverlapsContent; }
            set
            {
                scrollbarOverlapsContent = value;
                updatePadding();
            }
        }


        /// <summary>
        /// Size of available content (i.e. everything that can be scrolled to) in the scroll direction.
        /// </summary>
        private float availableContent => content.DrawSize[ScrollDim];

        /// <summary>
        /// Size of the viewport in the scroll direction.
        /// </summary>
        private float displayableContent => ChildSize[ScrollDim];

        /// <summary>
        /// Controls the distance scrolled when turning the mouse wheel a single notch.
        /// </summary>
        public float MouseWheelScrollDistance = 80;

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
        /// Controls the rate with which the target position is approached after using the mouse wheel. Default is 0.01
        /// </summary>
        public double DistanceDecayWheel = 0.01;

        /// <summary>
        /// Controls the rate with which the target position is approached after jumping to a specific location. Default is 0.01.
        /// </summary>
        public double DistanceDecayJump = 0.01;

        /// <summary>
        /// Controls the rate with which the target position is approached. It is automatically set after
        /// dragging or using the mouse wheel.
        /// </summary>
        private double distanceDecay;

        /// <summary>
        /// The current scroll position.
        /// </summary>
        public float Current { get; private set; }

        /// <summary>
        /// The target scroll position which is exponentially approached by current via a rate of distanceDecay.
        /// </summary>
        private float target;

        private float scrollableExtent => Math.Max(availableContent - displayableContent, 0);

        /// <summary>
        /// Clamp a value to the available scroll range.
        /// </summary>
        /// <param name="position">The value to clamp.</param>
        /// <param name="extension">An extension value beyond the normal extent.</param>
        /// <returns></returns>
        protected float Clamp(float position, float extension = 0) => MathHelper.Clamp(position, -extension, scrollableExtent + extension);

        protected override Container<T> Content => content;

        /// <summary>
        /// Whether we are currently scrolled as far as possible into the scroll direction.
        /// </summary>
        /// <param name="lenience">How close to the extent we need to be.</param>
        public bool IsScrolledToEnd(float lenience = Precision.FLOAT_EPSILON) => Precision.AlmostBigger(target, scrollableExtent, lenience);

        /// <summary>
        /// The container holding all children which are getting scrolled around.
        /// </summary>
        public Container<T> ScrollContent => content;

        protected virtual bool IsDragging { get; private set; }

        /// <summary>
        /// The direction in which scrolling is supported.
        /// </summary>
        protected readonly Direction ScrollDirection;

        /// <summary>
        /// The direction in which scrolling is supported, converted to an int for array index lookups.
        /// </summary>
        protected int ScrollDim => ScrollDirection == Direction.Horizontal ? 0 : 1;

        /// <summary>
        /// Creates a scroll container.
        /// </summary>
        /// <param name="scrollDirection">The direction in which should be scrolled. Can be vertical or horizontal. Default is vertical.</param>
        public ScrollContainer(Direction scrollDirection = Direction.Vertical)
        {
            ScrollDirection = scrollDirection;

            Masking = true;

            Axes scrollAxis = scrollDirection == Direction.Horizontal ? Axes.X : Axes.Y;
            AddRangeInternal(new Drawable[]
            {
                content = new Container<T>
                {
                    RelativeSizeAxes = Axes.Both & ~scrollAxis,
                    AutoSizeAxes = scrollAxis,
                },
                Scrollbar = new ScrollbarContainer(scrollDirection) { Dragged = onScrollbarMovement }
            });

            ScrollbarAnchor = scrollDirection == Direction.Vertical ? Anchor.TopRight : Anchor.BottomLeft;
        }

        private float lastUpdateDisplayableContent = -1;
        private float lastAvailableContent = -1;

        private void updateSize()
        {
            // ensure we only update scrollbar when something has changed, to avoid transform helpers resetting their transform every frame.
            // also avoids creating many needless Transforms every update frame.
            if (lastAvailableContent != availableContent || lastUpdateDisplayableContent != displayableContent)
            {
                lastAvailableContent = availableContent;
                lastUpdateDisplayableContent = displayableContent;
                updateScrollbar();
            }
        }

        private void updateScrollbar()
        {
            Scrollbar.ResizeTo(Math.Min(1, availableContent > 0 ? displayableContent / availableContent : 0), 200, Easing.OutQuint);
            Scrollbar.FadeTo(ScrollbarVisible && availableContent - 1 > displayableContent ? 1 : 0, 200);
            updatePadding();
        }

        private void updatePadding()
        {
            if (scrollbarOverlapsContent || availableContent <= displayableContent)
                content.Padding = new MarginPadding();
            else
            {
                if (ScrollDirection == Direction.Vertical)
                {
                    content.Padding = ScrollbarAnchor == Anchor.TopLeft
                        ? new MarginPadding { Left = Scrollbar.Width + Scrollbar.Margin.Left }
                        : new MarginPadding { Right = Scrollbar.Width + Scrollbar.Margin.Right };
                }
                else
                {
                    content.Padding = ScrollbarAnchor == Anchor.TopLeft
                        ? new MarginPadding { Top = Scrollbar.Height + Scrollbar.Margin.Top }
                        : new MarginPadding { Bottom = Scrollbar.Height + Scrollbar.Margin.Bottom };
                }
            }
        }

        protected override bool OnDragStart(InputState state)
        {
            if (IsDragging || !state.Mouse.IsPressed(MouseButton.Left)) return false;

            lastDragTime = Time.Current;
            averageDragDelta = averageDragTime = 0;

            IsDragging = true;
            return true;
        }

        protected override bool OnMouseDown(InputState state, MouseDownEventArgs args)
        {
            if (IsDragging || args.Button != MouseButton.Left) return false;

            // Continue from where we currently are scrolled to.
            target = Current;

            return true;
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

        protected override bool OnDrag(InputState state)
        {
            Trace.Assert(IsDragging, "We should never receive OnDrag if we are not dragging.");

            double currentTime = Time.Current;
            double timeDelta = currentTime - lastDragTime;
            double decay = Math.Pow(0.95, timeDelta);

            averageDragTime = averageDragTime * decay + timeDelta;
            averageDragDelta = averageDragDelta * decay - state.Mouse.Delta[ScrollDim];

            lastDragTime = currentTime;

            Vector2 childDelta = ToLocalSpace(state.Mouse.NativeState.Position) - ToLocalSpace(state.Mouse.NativeState.LastPosition);

            float scrollOffset = -childDelta[ScrollDim];
            float clampedScrollOffset = Clamp(target + scrollOffset) - Clamp(target);

            Debug.Assert(Precision.AlmostBigger(Math.Abs(scrollOffset), clampedScrollOffset * Math.Sign(scrollOffset)));

            // If we are dragging past the extent of the scrollable area, half the offset
            // such that the user can feel it.
            scrollOffset = clampedScrollOffset + (scrollOffset - clampedScrollOffset) / 2;

            offset(scrollOffset, false);
            return true;
        }

        protected override bool OnDragEnd(InputState state)
        {
            Trace.Assert(IsDragging, "We should never receive OnDragEnd if we are not dragging.");

            IsDragging = false;

            if (averageDragTime <= 0.0)
                return true;

            double velocity = averageDragDelta / averageDragTime;

            // Detect whether we halted at the end of the drag and in fact should _not_
            // perform a flick event.
            const double velocity_cutoff = 0.1;
            if (Math.Abs(Math.Pow(0.95, Time.Current - lastDragTime) * velocity) < velocity_cutoff)
                velocity = 0;

            // Differentiate f(t) = distance * (1 - exp(-t)) w.r.t. "t" to obtain
            // velocity w.r.t. time. Then rearrange to solve for distance given velocity.
            double distance = velocity / (1 - Math.Exp(-DistanceDecayDrag));

            offset((float)distance, true, DistanceDecayDrag);

            return true;
        }

        protected override bool OnWheel(InputState state)
        {
            offset(-MouseWheelScrollDistance * state.Mouse.WheelDelta, true, DistanceDecayWheel);
            return true;
        }

        private void onScrollbarMovement(float value) => scrollTo(Clamp(value / Scrollbar.Size[ScrollDim]), false);

        /// <summary>
        /// Immediately offsets the current and target scroll position.
        /// </summary>
        /// <param name="offset">The scroll offset.</param>
        public void OffsetScrollPosition(float offset)
        {
            target += offset;
            Current += offset;
        }

        private void offset(float value, bool animated, double distanceDecay = float.PositiveInfinity) => scrollTo(target + value, animated, distanceDecay);

        /// <summary>
        /// Scroll to the end of available content.
        /// </summary>
        /// <param name="animated">Whether to animate the movement.</param>
        /// <param name="allowDuringDrag">Whether we should interrupt a user's active drag.</param>
        public void ScrollToEnd(bool animated = true, bool allowDuringDrag = false)
        {
            if (!IsDragging || allowDuringDrag)
                scrollTo(scrollableExtent, animated, DistanceDecayJump);
        }

        /// <summary>
        /// Scrolls to a new position relative to the current scroll offset.
        /// </summary>
        /// <param name="offset">The amount by which we should scroll.</param>
        /// <param name="animated">Whether to animate the movement.</param>
        public void ScrollBy(float offset, bool animated = true) => scrollTo(target + offset, animated);

        /// <summary>
        /// Scrolls to an absolute position.
        /// </summary>
        /// <param name="value">The position to scroll to.</param>
        /// <param name="animated">Whether to animate the movement.</param>
        /// <param name="distanceDecay">Controls the rate with which the target position is approached after jumping to a specific location. Default is <see cref="DistanceDecayJump"/>.</param>
        public void ScrollTo(float value, bool animated = true, double? distanceDecay = null) => scrollTo(value, animated, distanceDecay ?? DistanceDecayJump);

        private void scrollTo(float value, bool animated, double distanceDecay = float.PositiveInfinity)
        {
            target = value;

            if (animated)
                this.distanceDecay = distanceDecay;
            else
                Current = target;
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

            if (minPos < Current)
                ScrollTo(minPos, animated);
            else if (maxPos > Current + displayableContent)
                ScrollTo(maxPos - displayableContent, animated);
        }

        /// <summary>
        /// Determines the position of a child in the content.
        /// </summary>
        /// <param name="d">The child to get the position from.</param>
        /// <param name="offset">Positional offset in the child's space.</param>
        /// <returns>The position of the child.</returns>
        public float GetChildPosInContent(Drawable d, Vector2 offset) => d.ToSpaceOfOtherDrawable(offset, content)[ScrollDim];

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
            if (!IsDragging && Current != Clamp(Current) && target != Clamp(target, -0.01f))
            {
                // Firstly, we want to limit how far out the target may go to limit overly bouncy
                // behaviour with extreme scroll velocities.
                target = Clamp(target, ClampExtension);

                // Secondly, we would like to quickly approach the target while we are out of bounds.
                // This is simulating a "strong" clamping force towards the target.
                if (Current < target && target < 0 || Current > target && target > scrollableExtent)
                    localDistanceDecay = distance_decay_clamping * 2;

                // Lastly, we gradually nudge the target towards valid bounds.
                target = (float)Interpolation.Lerp(Clamp(target), target, Math.Exp(-distance_decay_clamping * Time.Elapsed));

                float clampedTarget = Clamp(target);
                if (Precision.AlmostEquals(clampedTarget, target))
                    target = clampedTarget;
            }

            // Exponential interpolation between the target and our current scroll position.
            Current = (float)Interpolation.Lerp(target, Current, Math.Exp(-localDistanceDecay * Time.Elapsed));

            // This prevents us from entering the de-normalized range of floating point numbers when approaching target closely.
            if (Precision.AlmostEquals(Current, target))
                Current = target;
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            updateSize();
            updatePosition();

            Scrollbar?.MoveTo(ScrollDirection, Current * Scrollbar.Size[ScrollDim]);
            content.MoveTo(ScrollDirection, -Current);
        }

        protected internal class ScrollbarContainer : Container
        {
            public Action<float> Dragged;

            private readonly Color4 hoverColour = Color4.White;
            private readonly Color4 defaultColour = Color4.Gray;
            private readonly Color4 highlightColour = Color4.GreenYellow;

            private readonly Box box;

            private float dragOffset;

            private readonly int scrollDim;

            public ScrollbarContainer(Direction scrollDir)
            {
                scrollDim = (int)scrollDir;
                RelativeSizeAxes = scrollDir == Direction.Horizontal ? Axes.X : Axes.Y;
                Colour = defaultColour;

                Blending = BlendingMode.Additive;

                CornerRadius = 5;

                const float margin = 3;

                Margin = new MarginPadding
                {
                    Left = scrollDir == Direction.Vertical ? margin : 0,
                    Right = scrollDir == Direction.Vertical ? margin : 0,
                    Top = scrollDir == Direction.Horizontal ? margin : 0,
                    Bottom = scrollDir == Direction.Horizontal ? margin : 0,
                };

                Masking = true;

                Child = box = new Box { RelativeSizeAxes = Axes.Both };

                ResizeTo(1);
            }

            public void ResizeTo(float val, int duration = 0, Easing easing = Easing.None)
            {
                Vector2 size = new Vector2(10)
                {
                    [scrollDim] = val
                };
                this.ResizeTo(size, duration, easing);
            }

            protected override bool OnClick(InputState state) => true;

            protected override bool OnHover(InputState state)
            {
                this.FadeColour(hoverColour, 100);
                return true;
            }

            protected override void OnHoverLost(InputState state)
            {
                this.FadeColour(defaultColour, 100);
            }

            protected override bool OnDragStart(InputState state)
            {
                dragOffset = state.Mouse.Position[scrollDim] - Position[scrollDim];
                return true;
            }

            protected override bool OnMouseDown(InputState state, MouseDownEventArgs args)
            {
                if (args.Button != MouseButton.Left) return false;

                //note that we are changing the colour of the box here as to not interfere with the hover effect.
                box.FadeColour(highlightColour, 100);

                dragOffset = Position[scrollDim];
                Dragged?.Invoke(dragOffset);
                return true;
            }

            protected override bool OnMouseUp(InputState state, MouseUpEventArgs args)
            {
                if (args.Button != MouseButton.Left) return false;

                box.FadeColour(Color4.White, 100);

                return base.OnMouseUp(state, args);
            }

            protected override bool OnDrag(InputState state)
            {
                Dragged?.Invoke(state.Mouse.Position[scrollDim] - dragOffset);
                return true;
            }
        }
    }
}
