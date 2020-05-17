// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Input
{
    public readonly struct JoystickAxis
    {
        public readonly InputAxis Axis;
        public readonly float Value;

        public JoystickAxis(InputAxis axis, float value)
        {
            Axis = axis;
            Value = value;
        }
    }
}
