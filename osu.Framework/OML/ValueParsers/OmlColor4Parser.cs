// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Drawing;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using osu.Framework.OML.Factories;
using osuTK.Graphics;

namespace osu.Framework.OML.ValueParsers
{
    [UsedImplicitly]
    public class OmlColor4Parser : IOmlValueParser<Color4>
    {
        public Color4 Parse(string value)
        {
            return (Color4)Parse(typeof(Color4), value);
        }

        public IOmlValueParserFactory ParserFactory { get; set; }

        public object Parse(Type type, string value)
        {
            var colorConverter = new ColorConverter();

            if (value.StartsWith("rgba"))
            {
                var reg = new Regex(@"rgba\(([^)]+)\)");
                var colValues = reg.Split(value)[1].Split(",");

                var a = 0f;

                var r = float.Parse(colValues[0]);
                var g = float.Parse(colValues[1]);
                var b = float.Parse(colValues[2]);
                if (colValues.Length > 3)
                    a = float.Parse(colValues[3]);

                if (r > 1)
                    r /= byte.MaxValue;
                if (g > 1)
                    g /= byte.MaxValue;
                if (b > 1)
                    b /= byte.MaxValue;
                if (a > 1)
                    a /= byte.MaxValue;

                return new Color4(r, g, b, a);
            }

            if (!value.StartsWith("#"))
                return Color4.FromSrgb(Color.FromName(value));

            var convertedColorObj = colorConverter.ConvertFromString(value);
            return convertedColorObj != null ? Color4.FromSrgb((Color)convertedColorObj) : Color4.White;
        }
    }
}
