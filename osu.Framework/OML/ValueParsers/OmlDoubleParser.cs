// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Text.RegularExpressions;
using osu.Framework.MathUtils;
using osu.Framework.OML.Factories;

namespace osu.Framework.OML.ValueParsers
{
    public class OmlDoubleParser : IOmlValueParser<double>
    {
        private readonly Regex nmRegex = new Regex(@"(\d+(\.\d+)?)");
        private readonly Regex unitRegex = new Regex(@"[^\d\W]+");

        public double Parse(string value)
        {
            return (double)Parse(typeof(double), value);
        }

        public IOmlValueParserFactory ParserFactory { get; set; }

        public object Parse(Type type, string value)
        {
            var nm = nmRegex.Match(value).Value;
            var unit = unitRegex.Match(value).Value.ToLower();

            var f = double.TryParse(nm, out var num) ? num : 0f;

            var pixel = unit switch
            {
                "cm" => UnitConverter.Cm2Pixel(f),
                "in" => UnitConverter.Inch2Pixel(f),
                "pt" => UnitConverter.Point2Pixel(f),
                "pc" => UnitConverter.Pica2Pixel(f),
                _ => f
            };

            return pixel;
        }
    }
}
