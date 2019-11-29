// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using JetBrains.Annotations;
using osu.Framework.OML.Factories;

namespace osu.Framework.OML.ValueParsers
{
    [UsedImplicitly]
    public class OmlFloatParser : IOmlValueParser<float>
    {
        public float Parse(string value)
        {
            return (float)Parse(typeof(float), value);
        }

        public IOmlValueParserFactory ParserFactory { get; set; }

        public object Parse(Type type, string value)
        {
            var doubleParser = ParserFactory.Create<double>();

            return (float)doubleParser.Parse(value);
        }
    }
}
