// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using Android.Views;
using osu.Framework.Extensions.EnumExtensions;
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
    }
}
