// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using Guards;

namespace osu.Framework.Graphics.UserInterface
{
    public class DropdownMenuItem<T> : MenuItem
    {
        public readonly T Value;

        public DropdownMenuItem(string text, T value)
            : base(text)
        {
            Guard.ArgumentNotNull(value, nameof(value));
            Value = value;
        }

        public DropdownMenuItem(string text, T value, Action action)
            : base(text, action)
        {
            Guard.ArgumentNotNull(value, nameof(value));
            Value = value;
        }
    }
}
