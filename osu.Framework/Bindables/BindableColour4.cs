// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;

namespace osu.Framework.Bindables
{
    public class BindableColour4 : Bindable<Colour4>
    {
        public BindableColour4(Colour4 value = default)
            : base(value)
        {
        }

        // 8-bit precision should probably be enough for serialization.
        public override string ToString(string format, IFormatProvider formatProvider) => Value.ToHex();

        public override void Parse(object input)
        {
            if (input is string hex && Colour4.TryParseHex(hex, out Colour4 colour))
                Value = colour;
            else
                base.Parse(input);
        }

        protected override Bindable<Colour4> CreateInstance() => new BindableColour4();
    }
}
