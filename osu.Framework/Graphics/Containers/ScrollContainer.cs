// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Diagnostics;
using osu.Framework.Graphics.Transformations;
using osu.Framework.Input;
using osu.Framework.MathUtils;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Graphics.Containers
{
    public class ScrollContainer : Container
    {
        /// <summary>
        /// Determines whether the scroll dragger appears on the left side. If not, then it always appears on the right side.
        /// </summary>
        public Anchor ScrollDraggerAnchor
        {
            get { return scrollbar.Anchor; }

            set
            {
                scrollbar.Anchor = value;
                scrollbar.Origin = value;
            }
        }

        private Container content;
        private ScrollBar scrollbar;

        /// <summary>
        /// Vertical size of available content (content.Size)
        /// </summary>
        private float availableContent = -1;

        private float displayableContent => ChildSize.Y;

        /// <summary>
        /// This limits how far out of clamping bounds we allow the target position to be at most.
        /// Effectively, larger values result in bouncier behavior as the scroll boundaries are approached
        /// with high velocity.
        /// </summary>
        private const float CLAMP_EXTENSION = 200;

        /// <summary>
        /// This corresponds to the clamping force. A larger value means more aggressive clamping.
        /// </summary>
        private const double DISTANCE_DECAY_CLAMPING = 0.01;

        /// <summary>
        /// Controls the rate with which the target position is approached after ending a drag.
        /// </summary>
        private double distanceDecayDrag = 0.0035;

        /// <summary>
        /// Controls the rate with which the target position is approached after using the mouse wheel.
        /// </summary>
        private double distanceDecayWheel = 0.01;

        /// <summary>
        /// Controls the rate with which the target position is approached. It is automatically set after
        /// dragging or using the mouse wheel.
        /// </summary>
        private double distanceDecay;

        /// <summary>
        /// The current scroll position.
        /// </summary>
        private float current;

        /// <summary>
        /// The target scroll position which is exponentially approached by current via a rate of distanceDecay.
        /// </summary>
        private float target;

        private float scrollableExtent => (float)Math.Max(availableContent - displayableContent, 0);
        private float clamp(float position, float extension = 0) => MathHelper.Clamp(position, -extension, scrollableExtent + extension);

        protected override Container Content => content;

        private bool isDragging;

        public ScrollContainer()
        {
            RelativeSizeAxes = Axes.Both;

            AddInternal(new Drawable[]
            {
                content = new Container {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                },
                scrollbar = new ScrollBar { Dragged = onScrollbarMovement }
            });
        }

        protected override void Load(BaseGame game)
        {
            base.Load(game);

            Masking = true;

            content.OnAutoSize += contentAutoSize;
        }

        private void contentAutoSize()
        {
            if (Precision.AlmostEquals(availableContent, content.DrawSize.Y))
                return;

            availableContent = content.DrawSize.Y;
            updateSize();
            if (!isDragging)
                offset(0);

            scrollbar.Alpha = availableContent > displayableContent ? 1 : 0;
        }

        protected override bool OnDragStart(InputState state)
        {
            lastDragTime = Clock.CurrentTime;
            isDragging = true;
            return true;
        }

        protected override bool OnMouseDown(InputState state, MouseDownEventArgs args)
        {
            // Continue from where we currently are scrolled to.
            target = current;
            return base.OnMouseDown(state, args);
        }

        // We keep track of this because input events may happen at different intervals than update frames
        // and we are interested in the time difference between drag _input_ events.
        private double lastDragTime;
        private double lastDragTimeDelta;

        protected override bool OnDrag(InputState state)
        {
            var clock = Clock;
            lastDragTimeDelta = clock.CurrentTime - lastDragTime;
            lastDragTime = clock.CurrentTime;

            Vector2 childDelta = GetLocalPosition(state.Mouse.NativeState.Position) - GetLocalPosition(state.Mouse.NativeState.LastPosition);

            // If we are dragging past the extent of the scrollable area, half the offset
            // such that the user can feel it.
            if (target != clamp(target))
                childDelta /= 2;

            offset(-childDelta.Y, false);
            return base.OnDrag(state);
        }

        protected override bool OnDragEnd(InputState state)
        {
            if (lastDragTimeDelta == 0.0)
                return base.OnDragEnd(state);

            distanceDecay = distanceDecayDrag;

            // Solve exponential for distance, given delta and elapsed time during delta.
            double distance = -state.Mouse.Delta.Y / (1 - Math.Exp(-distanceDecay * lastDragTimeDelta));
            offset((float)distance);

            isDragging = false;

            return base.OnDragEnd(state);
        }

        protected override bool OnWheelDown(InputState state)
        {
            distanceDecay = distanceDecayWheel;
            offset(80);
            return true;
        }

        protected override bool OnWheelUp(InputState state)
        {
            distanceDecay = distanceDecayWheel;
            offset(-80);
            return true;
        }

        private void onScrollbarMovement(float value)
        {
            scrollTo(clamp(value / scrollbar.Size.Y), false);
        }

        private void offset(float value, bool animated = true)
        {
            scrollTo(target + value, animated);
        }

        public void ScrollTo(float value)
        {
            scrollTo(value);
        }

        private void scrollTo(float value, bool animated = true)
        {
            target = value;

            if (!animated)
                current = target;
        }
        
        public void ScrollIntoView(Drawable d)
        {
            scrollTo(d.Position.Y);
        }

        private void updateSize()
        {
            scrollbar?.ResizeTo(new Vector2(10, Math.Min(1, displayableContent / availableContent)), 200, EasingTypes.OutExpo);
        }

        private void updatePosition()
        {
            double localDistanceDecay = distanceDecay;

            // If we are not currently dragging the content, and we have scrolled out of bounds,
            // then we should handle the clamping force. Note, that if the target is _within_
            // acceptable bounds, then we do not need special handling of the clamping force, as
            // we will naturally scroll back into acceptable bounds.
            if (!isDragging && current != clamp(current) && target != clamp(target, -0.01f))
            {
                // Firstly, we want to limit how far out the target may go to limit overly bouncy
                // behaviour with extreme scroll velocities.
                target = clamp(target, CLAMP_EXTENSION);

                // Secondly, we would like to quickly approach the target while we are out of bounds.
                // This is simulating a "strong" clamping force towards the target.
                localDistanceDecay = DISTANCE_DECAY_CLAMPING * 2;

                // Lastly, we gradually nudge the target towards valid bounds.
                target = (float)Interpolation.Lerp(clamp(target), target, Math.Exp(-DISTANCE_DECAY_CLAMPING * Clock.ElapsedFrameTime));

                float clampedTarget = clamp(target);
                if (Precision.AlmostEquals(clampedTarget, target))
                    target = clampedTarget;
            }

            // Exponential interpolation between the target and our current scroll position.
            current = (float)Interpolation.Lerp(target, current, Math.Exp(-localDistanceDecay * Clock.ElapsedFrameTime));

            // This prevents us from entering the de-normalized range of floating point numbers when approaching target closely.
            if (Precision.AlmostEquals(current, target))
                current = target;
        }

        protected override void Update()
        {
            base.Update();

            updatePosition();

            scrollbar?.MoveToY(current * scrollbar.Size.Y);
            content.MoveToY(-current);
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
