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
            switch (input)
            {
                case string str:
                    if (!Colour4.TryParseHex(str, out Colour4 colour))
                        throw new ArgumentException($"Input string was in wrong format! (expected valid hex colour, actual: '{str}')");

                    Value = colour;
                    break;

                default:
                    base.Parse(input);
                    break;
            }
        }

        protected override Bindable<Colour4> CreateInstance() => new BindableColour4();
    }
}
