// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using Android.Views;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Input;
using osu.Framework.Logging;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Android.Input
{
    public static class AndroidInputExtensions
    {
        /// <summary>
        /// Denotes the current (last) event in a <see cref="MotionEvent"/>'s history.
        /// </summary>
        public const int HISTORY_CURRENT = -1;

        public delegate void MotionEventHandler(MotionEvent motionEvent, int historyPosition);

        /// <summary>
        /// Handles all events in this <see cref="MotionEvent"/>'s history in a chronological fashion (up to and including <see cref="HISTORY_CURRENT"/>).
        /// </summary>
        /// <param name="motionEvent">The <see cref="MotionEvent"/> to handle the history of.</param>
        /// <param name="handler">The <see cref="MotionEventHandler"/> to handle the events.</param>
        /// <remarks>
        /// Used in lieu of <see cref="HandleHistoricallyPerPointer"/> when the input infrastructure can only handle one pointer
        /// and/or when the <see cref="MotionEvent"/> is expected to report only one pointer.
        /// </remarks>
        public static void HandleHistorically(this MotionEvent motionEvent, MotionEventHandler handler)
        {
            if (motionEvent.PointerCount > 1)
            {
                Logger.Log($"{nameof(HandleHistorically)} was used when PointerCount ({motionEvent.PointerCount}) was greater than 1. Events for pointers other than the first have been dropped.");
                Logger.Log($"MotionEvent: {motionEvent}");
            }

            for (int h = 0; h < motionEvent.HistorySize; h++)
            {
                handler(motionEvent, h);
            }

            handler(motionEvent, HISTORY_CURRENT);
        }

        public delegate void MotionEventPerPointerHandler(MotionEvent motionEvent, int historyPosition, int pointerIndex);

        /// <summary>
        /// Handles all events in this <see cref="MotionEvent"/>'s history in a chronological fashion, sequentially calling <paramref name="handler"/> for each pointer.
        /// </summary>
        /// <param name="motionEvent">The <see cref="MotionEvent"/> to handle the history of.</param>
        /// <param name="handler">The <see cref="MotionEventPerPointerHandler"/> to handle the events.</param>
        public static void HandleHistoricallyPerPointer(this MotionEvent motionEvent, MotionEventPerPointerHandler handler)
        {
            for (int h = 0; h < motionEvent.HistorySize; h++)
            {
                for (int p = 0; p < motionEvent.PointerCount; p++)
                {
                    handler(motionEvent, h, p);
                }
            }

            for (int p = 0; p < motionEvent.PointerCount; p++)
            {
                handler(motionEvent, HISTORY_CURRENT, p);
            }
        }

        /// <summary>
        /// Returns the value of the requested axis.
        /// </summary>
        /// <param name="motionEvent">The <see cref="MotionEvent"/> to get the value from.</param>
        /// <param name="axis">The <see cref="Axis"/> identifier for the axis value to retrieve.</param>
        /// <param name="historyPosition">Which historical value to return; must be in range [<c>0</c>, <see cref="MotionEvent.HistorySize"/>), or the constant <see cref="HISTORY_CURRENT"/>.</param>
        /// <param name="pointerIndex">Raw index of pointer to retrieve; must be in range [<c>0</c>, <see cref="MotionEvent.PointerCount"/>).</param>
        /// <returns>The value of the axis, or <c>0</c> if the axis is not available.</returns>
        /// <remarks><paramref name="historyPosition"/>s different from <see cref="HISTORY_CURRENT"/> are valid only for <see cref="MotionEventActions.Move"/> events.</remarks>
        public static float Get(this MotionEvent motionEvent, Axis axis, int historyPosition = HISTORY_CURRENT, int pointerIndex = 0)
            => historyPosition == HISTORY_CURRENT
                ? motionEvent.GetAxisValue(axis, pointerIndex)
                : motionEvent.GetHistoricalAxisValue(axis, pointerIndex, historyPosition);

        /// <summary>
        /// Gets the <paramref name="value"/> of the requested axis, returning <c>true</c> if it's valid.
        /// </summary>
        /// <param name="motionEvent">The <see cref="MotionEvent"/> to get the value from.</param>
        /// <param name="axis">The <see cref="Axis"/> identifier for the axis value to retrieve.</param>
        /// <param name="value">The value of the axis, or <c>0</c> if the axis is not available.</param>
        /// <param name="historyPosition">Which historical <paramref name="value"/> to return; must be in range [<c>0</c>, <see cref="MotionEvent.HistorySize"/>),
        /// or the constant <see cref="HISTORY_CURRENT"/>.</param>
        /// <param name="pointerIndex">Raw index of pointer to retrieve; must be in range [<c>0</c>, <see cref="MotionEvent.PointerCount"/>).</param>
        /// <returns>Whether the returned <paramref name="value"/> is valid.</returns>
        /// <remarks><paramref name="historyPosition"/>s different from <see cref="HISTORY_CURRENT"/> are valid only for <see cref="MotionEventActions.Move"/> events.</remarks>
        public static bool TryGet(this MotionEvent motionEvent, Axis axis, out float value, int historyPosition = HISTORY_CURRENT, int pointerIndex = 0)
        {
            value = historyPosition == HISTORY_CURRENT
                ? motionEvent.GetAxisValue(axis, pointerIndex)
                : motionEvent.GetHistoricalAxisValue(axis, pointerIndex, historyPosition);

            return float.IsFinite(value);
        }

        /// <summary>
        /// Gets the <see cref="Axis.X"/> and <see cref="Axis.Y"/> axes of the event, returning <c>true</c> if they're valid.
        /// </summary>
        /// <param name="motionEvent">The <see cref="MotionEvent"/> to get the axes from.</param>
        /// <param name="position"><see cref="Vector2"/> containing the <see cref="Axis.X"/> and <see cref="Axis.Y"/> axes of the event.</param>
        /// <param name="historyPosition">Which historical <paramref name="position"/> to return; must be in range [<c>0</c>, <see cref="MotionEvent.HistorySize"/>),
        /// or the constant <see cref="HISTORY_CURRENT"/>.</param>
        /// <param name="pointerIndex">Raw index of pointer to retrieve; must be in range [<c>0</c>, <see cref="MotionEvent.PointerCount"/>).</param>
        /// <returns>Whether the returned <paramref name="position"/> is valid.</returns>
        /// <remarks><paramref name="historyPosition"/>s different from <see cref="HISTORY_CURRENT"/> are valid only for <see cref="MotionEventActions.Move"/> events.</remarks>
        public static bool TryGetPosition(this MotionEvent motionEvent, out Vector2 position, int historyPosition = HISTORY_CURRENT, int pointerIndex = 0)
        {
            if (motionEvent.TryGet(Axis.X, out float x, historyPosition, pointerIndex)
                && motionEvent.TryGet(Axis.Y, out float y, historyPosition, pointerIndex))
            {
                position = new Vector2(x, y);
                return true;
            }

            position = Vector2.Zero;
            return false;
        }

        /// <summary>
        /// Returns the corresponding <see cref="MouseButton"/>s for a mouse button given as a <see cref="MotionEventButtonState"/>.
        /// </summary>
        /// <param name="motionEventMouseButton">The given button state. Must not be a raw state or a non-mouse button.</param>
        /// <returns>The corresponding <see cref="MouseButton"/>s.</returns>
        public static IEnumerable<MouseButton> ToMouseButtons(this MotionEventButtonState motionEventMouseButton)
        {
            if (motionEventMouseButton.HasFlagFast(MotionEventButtonState.Primary))
                yield return MouseButton.Left;

            if (motionEventMouseButton.HasFlagFast(MotionEventButtonState.Secondary))
                yield return MouseButton.Right;

            if (motionEventMouseButton.HasFlagFast(MotionEventButtonState.Tertiary))
                yield return MouseButton.Middle;

            if (motionEventMouseButton.HasFlagFast(MotionEventButtonState.Back))
                yield return MouseButton.Button1;

            if (motionEventMouseButton.HasFlagFast(MotionEventButtonState.Forward))
                yield return MouseButton.Button2;
        }

        /// <summary>
        /// Returns the corresponding <see cref="MouseButton"/> for a mouse button given as a <see cref="Keycode"/>.
        /// </summary>
        /// <param name="keycode">The given keycode. Should be <see cref="Keycode.Back"/> or <see cref="Keycode.Forward"/>.</param>
        /// <param name="button">The corresponding <see cref="MouseButton"/>.</param>
        /// <returns><c>true</c> if this <paramref name="keycode"/> is a valid <see cref="MouseButton"/>.</returns>
        public static bool TryGetMouseButton(this Keycode keycode, out MouseButton button)
        {
            switch (keycode)
            {
                case Keycode.Back:
                    button = MouseButton.Button1;
                    return true;

                case Keycode.Forward:
                    button = MouseButton.Button2;
                    return true;
            }

            button = MouseButton.LastButton;
            return false;
        }

        public static bool TryGetJoystickButton(this KeyEvent e, out JoystickButton button)
        {
            var keycode = e.KeyCode;

            if (keycode >= Keycode.Button1 && keycode <= Keycode.Button16)
            {
                // JoystickButtons 1-16 are used below.
                button = JoystickButton.Button17 + (keycode - Keycode.Button1);
                return true;
            }

            switch (keycode)
            {
                // Dpad keycodes are _not_ joystick buttons, but are instead used for arrow keys on a keyboard.
                // as evident from KeyEvent.isGamePadButton():
                // https://cs.android.com/android/platform/superproject/+/master:frameworks/base/core/java/android/view/KeyEvent.java;l=1899-1936;drc=11e61ab1fd1f868ee8ddd6fc86662f4f09df1a6a
                case Keycode.DpadUp:
                case Keycode.DpadDown:
                case Keycode.DpadLeft:
                case Keycode.DpadRight:
                case Keycode.Back when e.Source == InputSourceType.Keyboard:
                default:
                    button = JoystickButton.FirstButton;
                    return false;

                case Keycode.ButtonA:
                    button = JoystickButton.GamePadA;
                    return true;

                case Keycode.ButtonB:
                    button = JoystickButton.GamePadB;
                    return true;

                case Keycode.ButtonC:
                    button = JoystickButton.Button14; // generic button
                    return true;

                case Keycode.ButtonX:
                    button = JoystickButton.GamePadX;
                    return true;

                case Keycode.ButtonY:
                    button = JoystickButton.GamePadY;
                    return true;

                case Keycode.ButtonZ:
                    button = JoystickButton.Button15; // generic button
                    return true;

                case Keycode.ButtonL1:
                    button = JoystickButton.GamePadLeftShoulder;
                    return true;

                case Keycode.ButtonR1:
                    button = JoystickButton.GamePadRightShoulder;
                    return true;

                case Keycode.ButtonL2:
                    button = JoystickButton.GamePadLeftTrigger;
                    return true;

                case Keycode.ButtonR2:
                    button = JoystickButton.GamePadRightTrigger;
                    return true;

                case Keycode.ButtonThumbl:
                    button = JoystickButton.GamePadLeftStick;
                    return true;

                case Keycode.ButtonThumbr:
                    button = JoystickButton.GamePadRightStick;
                    return true;

                case Keycode.ButtonStart:
                    button = JoystickButton.GamePadStart;
                    return true;

                case Keycode.Back:
                case Keycode.ButtonSelect:
                    button = JoystickButton.GamePadBack;
                    return true;

                case Keycode.ButtonMode:
                    button = JoystickButton.Button16; // generic button
                    return true;
            }
        }

        /// <summary>
        /// All axes supported by <see cref="TryGetJoystickAxisSource"/>.
        /// </summary>
        public static readonly IEnumerable<Axis> ALL_AXES = new[]
        {
            Axis.X,
            Axis.Y,
            Axis.Ltrigger,
            Axis.Z,
            Axis.Rz,
            Axis.Rtrigger,
            Axis.Rx,
            Axis.Ry,
            Axis.Rudder,
            Axis.Wheel,
        };

        /// <summary>
        /// Returns the corresponding <see cref="JoystickAxisSource"/> for an <see cref="Axis"/>.
        /// </summary>
        /// <returns><c>true</c> if provided <paramref name="axis"/> maps to a <see cref="JoystickAxisSource"/>.</returns>
        /// <remarks>
        /// <see cref="Axis.Gas"/> and <see cref="Axis.Brake"/> are deliberately excluded as those axes are 1:1 mirrors of the <see cref="Axis.Rtrigger"/> and <see cref="Axis.Ltrigger"/>.
        /// </remarks>
        public static bool TryGetJoystickAxisSource(this Axis axis, out JoystickAxisSource joystickAxis)
        {
            switch (axis)
            {
                case Axis.X:
                    joystickAxis = JoystickAxisSource.GamePadLeftStickX;
                    return true;

                case Axis.Y:
                    joystickAxis = JoystickAxisSource.GamePadLeftStickY;
                    return true;

                case Axis.Ltrigger:
                    joystickAxis = JoystickAxisSource.GamePadLeftTrigger;
                    return true;

                case Axis.Z:
                    joystickAxis = JoystickAxisSource.GamePadRightStickX;
                    return true;

                case Axis.Rz:
                    joystickAxis = JoystickAxisSource.GamePadRightStickY;
                    return true;

                case Axis.Rtrigger:
                    joystickAxis = JoystickAxisSource.GamePadRightTrigger;
                    return true;

                case Axis.Rx:
                    joystickAxis = JoystickAxisSource.Axis7;
                    return true;

                case Axis.Ry:
                    joystickAxis = JoystickAxisSource.Axis8;
                    return true;

                case Axis.Rudder:
                    joystickAxis = JoystickAxisSource.Axis9;
                    return true;

                case Axis.Wheel:
                    joystickAxis = JoystickAxisSource.Axis10;
                    return true;
            }

            joystickAxis = JoystickAxisSource.AxisCount;
            return false;
        }

        /// <summary>
        /// Whether this <see cref="MotionEventActions"/> is a touch down action.
        /// </summary>
        /// <param name="action">The <see cref="MotionEvent.ActionMasked"/> to check.</param>
        /// <returns>
        /// <c>true</c> if this is a touch down action.
        /// <c>false</c> if this is a touch up action.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">If this action is not a touch action.</exception>
        public static bool IsTouchDownAction(this MotionEventActions action)
        {
            switch (action)
            {
                case MotionEventActions.Down:
                case MotionEventActions.PointerDown:
                case MotionEventActions.Move:
                    return true;

                case MotionEventActions.PointerUp:
                case MotionEventActions.Up:
                case MotionEventActions.Cancel:
                    return false;

                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, "Motion event action is not a touch action.");
            }
        }
    }
}
