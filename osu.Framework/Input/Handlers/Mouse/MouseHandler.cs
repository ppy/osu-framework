// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Diagnostics;
using osu.Framework.Bindables;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Input.StateChanges;
using osu.Framework.Platform;
using osu.Framework.Statistics;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Input.Handlers.Mouse
{
    /// <summary>
    /// Handles mouse events from an <see cref="SDL2DesktopWindow"/>.
    /// Will use relative mouse mode where possible.
    /// </summary>
    public class MouseHandler : InputHandler, IHasCursorSensitivity, INeedsMousePositionFeedback
    {
        /// <summary>
        /// Whether relative mode should be preferred when the window has focus, the cursor is contained and the OS cursor is not visible.
        /// </summary>
        public BindableBool UseRelativeMode { get; } = new BindableBool(true)
        {
            Description = "Allows for sensitivity adjustment and tighter control of input",
        };

        public BindableDouble Sensitivity { get; } = new BindableDouble(1)
        {
            MinValue = 0.1,
            MaxValue = 10,
            Precision = 0.01
        };

        public override string Description => "Mouse";

        public override bool IsActive => true;

        private SDL2DesktopWindow window;

        private Vector2? lastPosition;

        private IBindable<bool> isActive;
        private IBindable<bool> cursorInWindow;
        private Bindable<CursorState> cursorState;

        /// <summary>
        /// Whether a non-relative mouse event has ever been received.
        /// This is used as a starting location for relative movement.
        /// </summary>
        private bool absolutePositionReceived;

        /// <summary>
        /// Whether the application should be handling the cursor.
        /// </summary>
        private bool cursorCaptured => isActive.Value && (window.CursorInWindow.Value || window.CursorState.HasFlagFast(CursorState.Confined));

        /// <summary>
        /// Whether the last position (as reported by <see cref="FeedbackMousePositionChange"/>)
        /// was outside the window.
        /// </summary>
        private bool previousPositionOutsideWindow = true;

        /// <summary>
        /// Set to true to unconditionally update relative mode on the next <see cref="FeedbackMousePositionChange"/>
        /// </summary>
        private bool pendingUpdateRelativeMode;

        public override bool Initialize(GameHost host)
        {
            if (!base.Initialize(host))
                return false;

            if (!(host.Window is SDL2DesktopWindow desktopWindow))
                return false;

            window = desktopWindow;

            isActive = window.IsActive.GetBoundCopy();
            isActive.BindValueChanged(_ => updateRelativeMode());

            cursorInWindow = host.Window.CursorInWindow.GetBoundCopy();
            cursorInWindow.BindValueChanged(e =>
            {
                if (e.NewValue)
                    // don't immediately update if the cursor has just entered the window
                    pendingUpdateRelativeMode = true;
                else
                    updateRelativeMode();
            });

            cursorState = desktopWindow.CursorStateBindable.GetBoundCopy();
            cursorState.BindValueChanged(_ => updateRelativeMode());

            UseRelativeMode.BindValueChanged(e =>
            {
                window.MouseAutoCapture = !e.NewValue;
                updateRelativeMode();
            }, true);

            Enabled.BindValueChanged(enabled =>
            {
                updateRelativeMode();

                if (enabled.NewValue)
                {
                    window.MouseMove += HandleMouseMove;
                    window.MouseMoveRelative += HandleMouseMoveRelative;
                    window.MouseDown += handleMouseDown;
                    window.MouseUp += handleMouseUp;
                    window.MouseWheel += handleMouseWheel;
                }
                else
                {
                    window.MouseMove -= HandleMouseMove;
                    window.MouseMoveRelative -= HandleMouseMoveRelative;
                    window.MouseDown -= handleMouseDown;
                    window.MouseUp -= handleMouseUp;
                    window.MouseWheel -= handleMouseWheel;
                }
            }, true);

            window.Exited += () =>
            {
                if (window.RelativeMouseMode && cursorCaptured)
                {
                    window.RelativeMouseMode = false;
                    transferLastPositionToHostCursor();
                }
            };

            return true;
        }

        public virtual void FeedbackMousePositionChange(Vector2 position, bool isSelfFeedback)
        {
            if (!Enabled.Value)
                return;

            if (!isSelfFeedback && isActive.Value)
                // if another handler has updated the cursor position, handle updating the OS cursor so we can seamlessly revert
                // to mouse control at any point.
                window.UpdateMousePosition(position);

            if (pendingUpdateRelativeMode)
            {
                updateRelativeMode();
                pendingUpdateRelativeMode = false;
            }

            bool positionOutsideWindow = position.X < 0 || position.Y < 0 || position.X >= window.Size.Width || position.Y >= window.Size.Height;

            if (window.RelativeMouseMode)
            {
                updateRelativeMode();

                // store the last mouse position to propagate back to the host window manager when exiting relative mode.
                lastPosition = position;

                // handle the case where relative / raw input is active, but the cursor may have exited the window
                // bounds and is not intended to be confined.
                if (!window.CursorState.HasFlagFast(CursorState.Confined) && positionOutsideWindow && !previousPositionOutsideWindow)
                {
                    // setting relative mode to false will allow the window manager to take control until the next
                    // updateRelativeMode() call succeeds (likely from the cursor returning inside the window).
                    window.RelativeMouseMode = false;
                    transferLastPositionToHostCursor();
                }
            }

            previousPositionOutsideWindow = positionOutsideWindow;
        }

        public override void Reset()
        {
            Sensitivity.SetDefault();
            base.Reset();
        }

        private void updateRelativeMode()
        {
            window.RelativeMouseMode =
                // check whether this handler is actually enabled.
                Enabled.Value
                // check whether the consumer has requested to use relative mode when feasible.
                && UseRelativeMode.Value
                // relative mode requires at least one absolute input to arrive, to gain an additional position to work with.
                && absolutePositionReceived
                // relative mode only works when the window is active and the cursor is contained. aka the OS cursor isn't being displayed outside the window.
                && cursorCaptured
                // relative mode shouldn't ever be enabled if the framework or a consumer has chosen not to hide the cursor.
                && window.CursorState.HasFlagFast(CursorState.Hidden);

            if (!window.RelativeMouseMode)
                transferLastPositionToHostCursor();
        }

        protected virtual void HandleMouseMove(Vector2 position)
        {
            absolutePositionReceived = true;
            enqueueInput(new MousePositionAbsoluteInput { Position = position });
        }

        protected virtual void HandleMouseMoveRelative(Vector2 delta)
        {
            enqueueInput(new MousePositionRelativeInput { Delta = delta * (float)Sensitivity.Value });
        }

        private void handleMouseDown(MouseButton button) => enqueueInput(new MouseButtonInput(button, true));

        private void handleMouseUp(MouseButton button) => enqueueInput(new MouseButtonInput(button, false));

        private void handleMouseWheel(Vector2 delta, bool precise) => enqueueInput(new MouseScrollRelativeInput { Delta = delta, IsPrecise = precise });

        private void enqueueInput(IInput input)
        {
            PendingInputs.Enqueue(input);
            FrameStatistics.Increment(StatisticsCounterType.MouseEvents);
        }

        private void transferLastPositionToHostCursor()
        {
            // while a noop on windows, some platforms (macOS) will not warp the host mouse when in relative mode.
            Debug.Assert(!window.RelativeMouseMode);

            if (lastPosition != null)
            {
                window.UpdateMousePosition(lastPosition.Value);
                lastPosition = null;
            }
        }
    }
}
