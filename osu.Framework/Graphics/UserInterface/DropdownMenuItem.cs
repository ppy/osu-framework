// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Localisation;

namespace osu.Framework.Graphics.UserInterface
{
    public class DropdownMenuItem<T> : MenuItem
    {
        public readonly T Value;

        public DropdownMenuItem(LocalisableString text, T value)
            : base(text)
        {
            Value = value;
        }

        public DropdownMenuItem(LocalisableString text, T value, Action action)
            : base(text, action)
        {
            Value = value;
        }
    }
}
