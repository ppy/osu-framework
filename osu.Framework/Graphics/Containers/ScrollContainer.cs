// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Diagnostics;
using osu.Framework.Input;
using osu.Framework.MathUtils;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Graphics.Containers
{
    public class ScrollContainer : Container, DelayedLoadWrapper.IOnScreenOptimisingContainer
    {
        /// <summary>
        /// Determines whether the scroll dragger appears on the left side. If not, then it always appears on the right side.
        /// </summary>
        public Anchor ScrollDraggerAnchor
        {
            get { return scrollDragger.Anchor; }

            set
            {
                scrollDragger.Anchor = value;
                scrollDragger.Origin = value;
                updatePadding();
            }
        }

        private bool scrollDraggerVisible = true;

        public bool ScrollDraggerVisible
        {
            get { return scrollDraggerVisible; }
            set
            {
                scrollDraggerVisible = value;
                updateScrollDragger();
            }
        }

        private readonly Container content;
        private readonly ScrollBar scrollDragger;


        private bool scrollDraggerOverlapsContent = true;

        public bool ScrollDraggerOverlapsContent
        {
            get { return scrollDraggerOverlapsContent; }
            set
            {
                scrollDraggerOverlapsContent = value;
                updatePadding();
            }
        }


        /// <summary>
        /// Vertical size of available content (content.Size)
        /// </summary>
        private float availableContent = -1;

        private float displayableContent => ChildSize[scrollDim];

        public float MouseWheelScrollDistance = 80;

        /// <summary>
        /// This limits how far out of clamping bounds we allow the target position to be at most.
        /// Effectively, larger values result in bouncier behavior as the scroll boundaries are approached
        /// with high velocity.
        /// </summary>
        private const float clamp_extension = 500;

        /// <summary>
        /// This corresponds to the clamping force. A larger value means more aggressive clamping.
        /// </summary>
        private const double distance_decay_clamping = 0.012;

        /// <summary>
        /// Controls the rate with which the target position is approached after ending a drag.
        /// </summary>
        public double DistanceDecayDrag = 0.0035;

        /// <summary>
        /// Controls the rate with which the target position is approached after using the mouse wheel.
        /// </summary>
        public double DistanceDecayWheel = 0.01;

        /// <summary>
        /// Controls the rate with which the target position is approached after jumping to a specific location.
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
        private float clamp(float position, float extension = 0) => MathHelper.Clamp(position, -extension, scrollableExtent + extension);

        protected override Container<Drawable> Content => content;

        public bool IsScrolledToEnd(float lenience = Precision.FLOAT_EPSILON) => Precision.AlmostBigger(target, scrollableExtent, lenience);

        /// <summary>
        /// The container holding all children which are getting scrolled around.
        /// </summary>
        public Container<Drawable> ScrollContent => content;

        private bool isDragging;

        private readonly Direction scrollDir;
        private int scrollDim => (int)scrollDir;

        public ScrollContainer(Direction scrollDir = Direction.Vertical)
        {
            this.scrollDir = scrollDir;

            Masking = true;

            Axes scrollAxis = scrollDir == Direction.Horizontal ? Axes.X : Axes.Y;
            AddInternal(new Drawable[]
            {
                content = new Container
                {
                    RelativeSizeAxes = Axes.Both & ~scrollAxis,
                    AutoSizeAxes = scrollAxis,
                },
                scrollDragger = new ScrollBar(scrollDir) { Dragged = onScrollbarMovement }
            });

            ScrollDraggerAnchor = scrollDir == Direction.Vertical ? Anchor.TopRight : Anchor.BottomLeft;
        }

        private void updateSize()
        {
            //todo: can limit this to when displayableContent or availableContent changed.
            availableContent = content.DrawSize[scrollDim];
            updateScrollDragger();
        }

        private void updateScrollDragger()
        {
            scrollDragger.ResizeTo(Math.Min(1, availableContent > 0 ? displayableContent / availableContent : 0), 200, EasingTypes.OutQuint);
            scrollDragger.FadeTo(ScrollDraggerVisible && availableContent - 1 > displayableContent ? 1 : 0, 200);
            updatePadding();
        }

        private void updatePadding()
        {
            if (scrollDraggerOverlapsContent || availableContent <= displayableContent)
                content.Padding = new MarginPadding();
            else
            {
                if (scrollDir == Direction.Vertical)
                {
                    content.Padding = ScrollDraggerAnchor == Anchor.TopLeft
                        ? new MarginPadding { Left = scrollDragger.Width }
                        : new MarginPadding { Right = scrollDragger.Width };
                }
                else
                {
                    content.Padding = ScrollDraggerAnchor == Anchor.TopLeft
                        ? new MarginPadding { Top = scrollDragger.Height }
                        : new MarginPadding { Bottom = scrollDragger.Height };
                }
            }
        }

        protected override bool OnDragStart(InputState state)
        {
            lastDragTime = Time.Current;
            averageDragDelta = averageDragTime = 0;

            isDragging = true;
            return true;
        }

        protected override bool OnMouseDown(InputState state, MouseDownEventArgs args)
        {
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
            Trace.Assert(isDragging, "We should never receive OnDrag if we are not dragging.");

            double currentTime = Time.Current;
            double timeDelta = currentTime - lastDragTime;
            double decay = Math.Pow(0.95, timeDelta);

            averageDragTime = averageDragTime * decay + timeDelta;
            averageDragDelta = averageDragDelta * decay - state.Mouse.Delta[scrollDim];

            lastDragTime = currentTime;

            Vector2 childDelta = ToLocalSpace(state.Mouse.NativeState.Position) - ToLocalSpace(state.Mouse.NativeState.LastPosition);

            // If we are dragging past the extent of the scrollable area, half the offset
            // such that the user can feel it.
            if (target != clamp(target))
                childDelta /= 2;

            offset(-childDelta[scrollDim], false);
            return true;
        }

        protected override bool OnDragEnd(InputState state)
        {
            Trace.Assert(isDragging, "We should never receive OnDragEnd if we are not dragging.");

            isDragging = false;

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

        private void onScrollbarMovement(float value) => scrollTo(clamp(value / scrollDragger.Size[scrollDim]), false);

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
            if (!isDragging || allowDuringDrag) scrollTo(scrollableExtent, animated, DistanceDecayJump);
        }

        public void ScrollBy(float offset, bool animated = true) => scrollTo(target + offset, animated);

        public void ScrollTo(float value, bool animated = true) => scrollTo(value, animated, DistanceDecayJump);

        private void scrollTo(float value, bool animated, double distanceDecay = float.PositiveInfinity)
        {
            target = value;

            if (animated)
                this.distanceDecay = distanceDecay;
            else
                Current = target;
        }

        public void ScrollIntoView(Drawable d) => ScrollTo(GetChildPosInContent(d));

        public float GetChildPosInContent(Drawable d) => d.Parent.ToSpaceOfOtherDrawable(d.Position, content)[scrollDim];

        private void updatePosition()
        {
            double localDistanceDecay = distanceDecay;

            // If we are not currently dragging the content, and we have scrolled out of bounds,
            // then we should handle the clamping force. Note, that if the target is _within_
            // acceptable bounds, then we do not need special handling of the clamping force, as
            // we will naturally scroll back into acceptable bounds.
            if (!isDragging && Current != clamp(Current) && target != clamp(target, -0.01f))
            {
                // Firstly, we want to limit how far out the target may go to limit overly bouncy
                // behaviour with extreme scroll velocities.
                target = clamp(target, clamp_extension);

                // Secondly, we would like to quickly approach the target while we are out of bounds.
                // This is simulating a "strong" clamping force towards the target.
                if (Current < target && target < 0 || Current > target && target > scrollableExtent)
                    localDistanceDecay = distance_decay_clamping * 2;

                // Lastly, we gradually nudge the target towards valid bounds.
                target = (float)Interpolation.Lerp(clamp(target), target, Math.Exp(-distance_decay_clamping * Time.Elapsed));

                float clampedTarget = clamp(target);
                if (Precision.AlmostEquals(clampedTarget, target))
                    target = clampedTarget;
            }

            // Exponential interpolation between the target and our current scroll position.
            Current = (float)Interpolation.Lerp(target, Current, Math.Exp(-localDistanceDecay * Time.Elapsed));

            // This prevents us from entering the de-normalized range of floating point numbers when approaching target closely.
            if (Precision.AlmostEquals(Current, target))
                Current = target;
        }

        protected override void Update()
        {
            base.Update();

            updateSize();
            updatePosition();

            scrollDragger?.MoveTo(scrollDir, Current * scrollDragger.Size[scrollDim]);
            content.MoveTo(scrollDir, -Current);
        }

        private class ScrollBar : Container
        {
            public Action<float> Dragged;

            private static readonly Color4 hover_colour = Color4.White;
            private static readonly Color4 default_colour = Color4.LightGray;
            private static readonly Color4 highlight_colour = Color4.GreenYellow;
            private readonly Box box;

            private float dragOffset;

            private readonly int scrollDim;

            public ScrollBar(Direction scrollDir)
            {
                scrollDim = (int)scrollDir;
                RelativeSizeAxes = scrollDir == Direction.Horizontal ? Axes.X : Axes.Y;
                Colour = default_colour;
                CornerRadius = 5;
                Masking = true;

                Children = new Drawable[]
                {
                    box = new Box { RelativeSizeAxes = Axes.Both }
                };

                ResizeTo(1);
            }

            public void ResizeTo(float val, int duration = 0, EasingTypes easing = EasingTypes.None)
            {
                Vector2 size = new Vector2(10)
                {
                    [scrollDim] = val
                };
                ResizeTo(size, duration, easing);
            }

            protected override bool OnHover(InputState state)
            {
                FadeColour(hover_colour, 100);
                return true;
            }

            protected override void OnHoverLost(InputState state)
            {
                FadeColour(default_colour, 100);
            }

            protected override bool OnDragStart(InputState state)
            {
                dragOffset = state.Mouse.Position[scrollDim] - Position[scrollDim];
                return true;
            }

            protected override bool OnMouseDown(InputState state, MouseDownEventArgs args)
            {
                //note that we are changing the colour of the box here as to not interfere with the hover effect.
                box.FadeColour(highlight_colour, 100);
                return true;
            }

            protected override bool OnMouseUp(InputState state, MouseUpEventArgs args)
            {
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
