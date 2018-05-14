// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Input
{
    public struct JoystickAxis
    {
        public readonly int Axis;
        public readonly float Value;

        public JoystickAxis(int axis, float value)
        {
            Axis = axis;
            Value = value;
        }
    }
}
