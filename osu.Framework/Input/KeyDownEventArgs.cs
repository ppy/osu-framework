// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using OpenTK.Input;

namespace osu.Framework.Input
{
    public class KeyDownEventArgs : EventArgs
    {
        public Key Key;
        public bool Repeat;
    }
}
