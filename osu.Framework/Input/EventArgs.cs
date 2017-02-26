// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK.Input;
using System;

namespace osu.Framework.Input
{
    public class KeyDownEventArgs : EventArgs
    {
        public Key Key;
        public bool Repeat;
    }

    public class MouseUpEventArgs : MouseEventArgs
    {
    }

    public class MouseDownEventArgs : MouseEventArgs
    {
    }

    public class MouseEventArgs : EventArgs
    {
        public MouseButton Button;
    }

    public class KeyUpEventArgs : EventArgs
    {
        public Key Key;
    }
}
