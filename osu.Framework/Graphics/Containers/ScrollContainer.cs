// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Diagnostics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Transformations;
using osu.Framework.Input;
using osu.Framework.MathUtils;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Allocation;

namespace osu.Framework.Graphics.Containers
{
    public class ScrollContainer : Container
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

        private Container content;
        private ScrollBar scrollDragger;


        private bool scrollbarOverlapsContent = true;

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
        /// Vertical size of available content (content.Size)
        /// </summary>
        private float availableContent = -1;

        private float displayableContent => ChildSize.Y;

        public float MouseWheelScrollDistance = 80;

        /// <summary>
        /// This limits how far out of clamping bounds we allow the target position to be at most.
        /// Effectively, larger values result in bouncier behavior as the scroll boundaries are approached
        /// with high velocity.
        /// </summary>
        private const float CLAMP_EXTENSION = 500;

        /// <summary>
        /// This corresponds to the clamping force. A larger value means more aggressive clamping.
        /// </summary>
        private const double DISTANCE_DECAY_CLAMPING = 0.012;

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

        private float scrollableExtent => (float)Math.Max(availableContent - displayableContent, 0);
        private float clamp(float position, float extension = 0) => MathHelper.Clamp(position, -extension, scrollableExtent + extension);

        protected override Container<Drawable> Content => content;

        private bool isDragging;

        public ScrollContainer()
        {
            RelativeSizeAxes = Axes.Both;
            Masking = true;

            AddInternal(new Drawable[]
            {
                content = new Container {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                },
                scrollDragger = new ScrollBar { Dragged = onScrollbarMovement }
            });
        }


        private void updateSize()
        {
            float contentSize = content.DrawSize.Y;
            if (Precision.AlmostEquals(availableContent, contentSize))
                return;

            availableContent = contentSize;
            updateScrollDragger(); 
        }

        private void updateScrollDragger()
        {
            scrollDragger.ResizeTo(new Vector2(10, Math.Min(1, displayableContent / availableContent)), 200, EasingTypes.OutExpo);
            scrollDragger.Alpha = ScrollDraggerVisible && availableContent > displayableContent ? 1 : 0;
            updatePadding();
        }

        private void updatePadding()
        {
            if (scrollbarOverlapsContent || availableContent <= displayableContent)
                content.Padding = new MarginPadding();
            else
                content.Padding = ScrollDraggerAnchor == Anchor.TopLeft ?
                    new MarginPadding { Left = scrollDragger.Width } :
                    new MarginPadding { Right = scrollDragger.Width };
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
            Debug.Assert(isDragging, "We should never receive OnDrag if we are not dragging.");

            double currentTime = Time.Current;
            double timeDelta = currentTime - lastDragTime;
            double decay = Math.Pow(0.95, timeDelta);

            averageDragTime = averageDragTime * decay + timeDelta;
            averageDragDelta = averageDragDelta * decay - state.Mouse.Delta.Y;

            lastDragTime = currentTime;

            Vector2 childDelta = ToLocalSpace(state.Mouse.NativeState.Position) - ToLocalSpace(state.Mouse.NativeState.LastPosition);

            // If we are dragging past the extent of the scrollable area, half the offset
            // such that the user can feel it.
            if (target != clamp(target))
                childDelta /= 2;

            offset(-childDelta.Y, false);
            return true;
        }

        protected override bool OnDragEnd(InputState state)
        {
            Debug.Assert(isDragging, "We should never receive OnDragEnd if we are not dragging.");

            isDragging = false;

            if (averageDragTime <= 0.0)
                return true;

            double velocity = averageDragDelta / averageDragTime;

            // Detect whether we halted at the end of the drag and in fact should _not_
            // perform a flick event.
            const double VELOCITY_CUTOFF = 0.1;
            if (Math.Abs(Math.Pow(0.95, Time.Current - lastDragTime) * velocity) < VELOCITY_CUTOFF)
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

        private void onScrollbarMovement(float value) => scrollTo(clamp(value / scrollDragger.Size.Y), false);

        private void offset(float value, bool animated, double distanceDecay = float.PositiveInfinity) => scrollTo(target + value, animated, distanceDecay);

        public void ScrollTo(float value, bool animated = true) => scrollTo(value, animated, DistanceDecayJump);

        private void scrollTo(float value, bool animated, double distanceDecay = float.PositiveInfinity)
        {
            target = value;

            if (animated)
                this.distanceDecay = distanceDecay;
            else
                Current = target;
        }

        public void ScrollIntoView(Drawable d) => ScrollTo(GetChildYInContent(d));

        public float GetChildYInContent(Drawable d) => d.Parent.ToSpaceOfOtherDrawable(d.Position, content).Y;

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
                target = clamp(target, CLAMP_EXTENSION);

                // Secondly, we would like to quickly approach the target while we are out of bounds.
                // This is simulating a "strong" clamping force towards the target.
                if ((Current < target && target < 0) || (Current > target && target > scrollableExtent))
                    localDistanceDecay = DISTANCE_DECAY_CLAMPING * 2;

                // Lastly, we gradually nudge the target towards valid bounds.
                target = (float)Interpolation.Lerp(clamp(target), target, Math.Exp(-DISTANCE_DECAY_CLAMPING * Time.Elapsed));

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

            scrollDragger?.MoveToY(Current * scrollDragger.Size.Y);
            content.MoveToY(-Current);
        }

        private class ScrollBar : Container
        {
            public Action<float> Dragged;

            private Color4 hoverColour = Color4.White;
            private Color4 defaultColour = Color4.LightGray;
            private Color4 highlightColour = Color4.GreenYellow;
            private Box box;

            private float dragOffset;

            public ScrollBar()
            {
                Children = new Drawable[]
                {
                    box = new Box
                    {
                        RelativeSizeAxes = Axes.Both
                    }
                };

                RelativeSizeAxes = Axes.Y;
                Size = new Vector2(10, 1);
                Colour = defaultColour;
                CornerRadius = 5;
                Masking = true;

                Anchor = Anchor.TopRight;
                Origin = Anchor.TopRight;
            }

            protected override bool OnHover(InputState state)
            {
                FadeColour(hoverColour, 100);
                return true;
            }

            protected override void OnHoverLost(InputState state)
            {
                FadeColour(defaultColour, 100);
            }

            protected override bool OnDragStart(InputState state)
            {
                dragOffset = state.Mouse.Position.Y - Position.Y;
                return true;
            }

            protected override bool OnMouseDown(InputState state, MouseDownEventArgs args)
            {
                //note that we are changing the colour of the box here as to not interfere with the hover effect.
                box.FadeColour(highlightColour, 100);
                return true;
            }

            protected override bool OnMouseUp(InputState state, MouseUpEventArgs args)
            {
                box.FadeColour(Color4.White, 100);

                return base.OnMouseUp(state, args);
            }

            protected override bool OnDrag(InputState state)
            {
                Dragged?.Invoke(state.Mouse.Position.Y - dragOffset);
                return true;
            }
        }
    }
}
