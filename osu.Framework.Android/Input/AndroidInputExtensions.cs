// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Android.Views;
using osuTK.Input;

namespace osu.Framework.Android.Input
{
    public static class AndroidInputExtensions
    {
        /// <summary>
        /// Returns the corresponding <see cref="MouseButton"/> for a mouse button given as a <see cref="MotionEventButtonState"/>.
        /// </summary>
        /// <param name="motionEventMouseButton">The given button. Must not be a raw state or a non-mouse button.</param>
        /// <returns>The corresponding <see cref="MouseButton"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the provided button <paramref name="motionEventMouseButton"/> is not a </exception>
        public static MouseButton ToMouseButton(this MotionEventButtonState motionEventMouseButton)
        {
            switch (motionEventMouseButton)
            {
                case MotionEventButtonState.Primary:
                    return MouseButton.Left;

                case MotionEventButtonState.Secondary:
                    return MouseButton.Right;

                case MotionEventButtonState.Tertiary:
                    return MouseButton.Middle;

                case MotionEventButtonState.Back:
                    return MouseButton.Button1;

                case MotionEventButtonState.Forward:
                    return MouseButton.Button2;

                default:
                    throw new ArgumentOutOfRangeException(nameof(motionEventMouseButton), motionEventMouseButton, "Given button is not a mouse button.");
            }
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
