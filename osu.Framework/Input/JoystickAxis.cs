// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Input
{
    public readonly struct JoystickAxis
    {
        public readonly JoystickAxisSource Source;
        public readonly float Value;

        public JoystickAxis(JoystickAxisSource source, float value)
        {
            Source = source;
            Value = value;
        }
    }
}
