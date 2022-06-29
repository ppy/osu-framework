// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using Android.Views;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Input;
using osuTK.Input;

namespace osu.Framework.Android.Input
{
    public static class AndroidInputExtensions
    {
        /// <summary>
        /// Returns the corresponding <see cref="MouseButton"/>s for a mouse button given as a <see cref="MotionEventButtonState"/>.
        /// </summary>
        /// <param name="motionEventMouseButton">The given button state. Must not be a raw state or a non-mouse button.</param>
        /// <returns>The corresponding <see cref="MouseButton"/>s.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the provided button <paramref name="motionEventMouseButton"/> is not a </exception>
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

        public static bool TryGetJoystickButton(this Keycode keycode, out JoystickButton button)
        {
            switch (keycode)
            {
                case Keycode.DpadUp:
                    button = JoystickButton.GamePadDPadUp;
                    return true;

                case Keycode.DpadDown:
                    button = JoystickButton.GamePadDPadDown;
                    return true;

                case Keycode.DpadLeft:
                    button = JoystickButton.GamePadDPadLeft;
                    return true;

                case Keycode.DpadRight:
                    button = JoystickButton.GamePadDPadRight;
                    return true;

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

                case Keycode.ButtonSelect:
                    button = JoystickButton.GamePadBack;
                    return true;

                case Keycode.ButtonMode:
                    button = JoystickButton.Button16; // generic button
                    return true;
            }

            if (keycode >= Keycode.Button1 && keycode <= Keycode.Button16)
            {
                // JoystickButtons 1-16 are used above.
                button = JoystickButton.Button17 + (keycode - Keycode.Button1);
                return true;
            }

            button = JoystickButton.FirstButton;
            return false;
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
    }
}
