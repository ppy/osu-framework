// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Drawing;

namespace osu.Framework.Configuration
{
    public class BindableSize : Bindable<Size>
    {
        public BindableSize(Size value = default(Size))
            : base(value)
        {
        }

        public override string ToString() => $"{Value.Width}x{Value.Height}";

        public override void Parse(object input)
        {
            switch (input)
            {
                case string str:
                    if (!str.Contains("x"))
                        throw new ArgumentException($"Input string was in wrong format! (expected: '<width>x<height>', actual: '{str}')");

                    var split = str.Split('x');
                    Value = new Size(int.Parse(split[0]), int.Parse(split[1]));
                    break;
                default:
                    base.Parse(input);
                    break;
            }
        }
    }
}
