// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Input.Commands;

namespace osu.Framework.Graphics.UserInterface
{
    public class DropdownMenuItem<T> : MenuItem
    {
        public readonly T Value;

        public DropdownMenuItem(string text, T value)
            : base(text)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            Value = value;
        }

        public DropdownMenuItem(string text, T value, ICommand command)
            : base(text, command)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            Value = value;
        }
    }
}
