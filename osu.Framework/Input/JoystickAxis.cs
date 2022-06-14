// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

namespace osu.Framework.Input
{
    public readonly struct JoystickAxis
    {
        /// <summary>
        /// The source of this axis.
        /// </summary>
        public readonly JoystickAxisSource Source;

        /// <summary>
        /// The value of this axis.
        /// </summary>
        public readonly float Value;

        public JoystickAxis(JoystickAxisSource source, float value)
        {
            Source = source;
            Value = value;
        }
    }
}
