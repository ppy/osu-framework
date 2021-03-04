// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using osu.Framework.Bindables;
using osu.Framework.Input.StateChanges;
using osu.Framework.Platform;
using osu.Framework.Statistics;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Input.Handlers.Mouse
{
    public class RawMouseHandler : InputHandler, IHasCursorSensitivity, INeedsMousePositionFeedback
    {
        public BindableDouble Sensitivity { get; } = new BindableDouble(1) { MinValue = 0.1, MaxValue = 10 };

        public override bool IsActive => true;

        public override int Priority => 0;

        private SDL2DesktopWindow window;

        private readonly BindableBool mapAbsoluteInputToWindow = new BindableBool();

        private Vector2? lastPosition;

        private IBindable<bool> isActive;

        public override bool Initialize(GameHost host)
        {
            if (!(host.Window is SDL2DesktopWindow desktopWindow))
                return false;

            window = desktopWindow;

            isActive = window.IsActive.GetBoundCopy();
            isActive.BindValueChanged(_ => updateRelativeMode());

            Enabled.BindValueChanged(enabled =>
            {
                updateRelativeMode();

                if (enabled.NewValue)
                {
                    window.MouseMove += handleMouseMove;
                    window.MouseMoveRelative += handleMouseMoveRelative;
                    window.MouseDown += handleMouseDown;
                    window.MouseUp += handleMouseUp;
                    window.MouseWheel += handleMouseWheel;
                }
                else
                {
                    window.MouseMove -= handleMouseMove;
                    window.MouseMoveRelative -= handleMouseMoveRelative;
                    window.MouseDown -= handleMouseDown;
                    window.MouseUp -= handleMouseUp;
                    window.MouseWheel -= handleMouseWheel;
                }
            }, true);

            return true;
        }

        public void FeedbackMousePositionChange(Vector2 position)
        {
            if (!Enabled.Value)
                return;

            // store the last (final) mouse position to propagate back to the host window manager when required.
            lastPosition = position;
            updateRelativeMode();

            // handle the case where relative / raw input is active, but the cursor may have exited the window
            // bounds and is not intended to be confined.
            if (window.RelativeMouseMode && !window.CursorConfined)
            {
                if (position.X < 0 || position.Y < 0 || position.X > window.Size.Width || position.Y > window.Size.Height)
                {
                    // setting relative mode to false will allow the window manager to take control until the next
                    // updateRelativeMode() call succeeds (likely from the cursor returning inside the window).
                    window.RelativeMouseMode = false;
                    transferLastPositionToHostCursor();
                }
            }
        }

        private void updateRelativeMode()
        {
            window.RelativeMouseMode = Enabled.Value && (isActive.Value && (window.CursorInWindow.Value || window.CursorConfined));

            if (!window.RelativeMouseMode)
                transferLastPositionToHostCursor();
        }

        private void enqueueInput(IInput input)
        {
            PendingInputs.Enqueue(input);
            FrameStatistics.Increment(StatisticsCounterType.MouseEvents);
        }

        private void handleMouseMove(Vector2 position) => enqueueInput(new MousePositionAbsoluteInput { Position = position });

        private void handleMouseMoveRelative(Vector2 delta)
        {
            float mappedInputMultiplier = 1f;

            if (mapAbsoluteInputToWindow.Value)
            {
                const float base_size = 1024f;
                int longestSide = Math.Max(window.Size.Width, window.Size.Height);
                mappedInputMultiplier = longestSide / base_size;
            }

            enqueueInput(new MousePositionRelativeInput { Delta = delta * (float)Sensitivity.Value * mappedInputMultiplier });
        }

        private void handleMouseDown(MouseButton button) => enqueueInput(new MouseButtonInput(button, true));

        private void handleMouseUp(MouseButton button) => enqueueInput(new MouseButtonInput(button, false));

        private void handleMouseWheel(Vector2 delta, bool precise) => enqueueInput(new MouseScrollRelativeInput { Delta = delta, IsPrecise = precise });

        private void transferLastPositionToHostCursor()
        {
            // while a noop on windows, this causes weirdness on (at least) macOS.
            Debug.Assert(!window.RelativeMouseMode);

            if (lastPosition != null)
            {
                window.UpdateMousePosition(lastPosition.Value);
                lastPosition = null;
            }
        }
    }
}
