// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Input.States
{
    public class JoystickState
    {
        public const int MAX_AXES = 64;

        public readonly ButtonStates<JoystickButton> Buttons = new ButtonStates<JoystickButton>();
        public readonly float[] Axes = new float[MAX_AXES];
    }
}
