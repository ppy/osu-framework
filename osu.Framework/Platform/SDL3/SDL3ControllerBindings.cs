// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using SDL;
using static SDL.SDL3;

namespace osu.Framework.Platform.SDL3
{
    /// <summary>
    /// Maintain a copy of the SDL-provided bindings for the given controller.
    /// Used to determine whether a given event's joystick button or axis is unmapped.
    /// </summary>
    internal unsafe class SDL3ControllerBindings
    {
        public readonly SDL_Joystick* JoystickHandle;
        public readonly SDL_Gamepad* GamepadHandle;

        /// <summary>
        /// Bindings returned from <see cref="SDL_GetGamepadBindings(SDL_Gamepad*)"/>.
        /// Empty if the joystick does not have a corresponding GamepadHandle.
        /// </summary>
        public SDL_GamepadBinding[] Bindings;

        public SDL3ControllerBindings(SDL_Joystick* joystickHandle, SDL_Gamepad* gamepadHandle)
        {
            JoystickHandle = joystickHandle;
            GamepadHandle = gamepadHandle;

            PopulateBindings();
        }

        public void PopulateBindings()
        {
            if (GamepadHandle == null)
            {
                Bindings = Array.Empty<SDL_GamepadBinding>();
                return;
            }

            using var bindings = SDL_GetGamepadBindings(GamepadHandle);

            if (bindings == null)
            {
                Bindings = Array.Empty<SDL_GamepadBinding>();
                return;
            }

            Bindings = new SDL_GamepadBinding[bindings.Count];

            for (int i = 0; i < bindings.Count; i++)
                Bindings[i] = bindings[i];
        }

        public bool IsJoystickButtonBound(byte buttonIndex)
        {
            for (int i = 0; i < Bindings.Length; i++)
            {
                if (Bindings[i].input_type == SDL_GamepadBindingType.SDL_GAMEPAD_BINDTYPE_BUTTON && Bindings[i].input.button == buttonIndex)
                    return true;
            }

            return false;
        }

        public bool IsJoystickAxisBound(byte axisIndex)
        {
            for (int i = 0; i < Bindings.Length; i++)
            {
                if (Bindings[i].input_type == SDL_GamepadBindingType.SDL_GAMEPAD_BINDTYPE_AXIS && Bindings[i].input.axis.axis == axisIndex)
                    return true;
            }

            return false;
        }
    }
}
