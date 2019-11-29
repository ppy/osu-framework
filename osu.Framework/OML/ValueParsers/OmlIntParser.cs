// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using JetBrains.Annotations;
using osu.Framework.OML.Factories;

namespace osu.Framework.OML.ValueParsers
{
    [UsedImplicitly]
    public class OmlIntParser : IOmlValueParser<int>
    {
        public int Parse(string value)
        {
            return (int)Parse(typeof(int), value);
        }

        public IOmlValueParserFactory ParserFactory { get; set; }

        public object Parse(Type type, string value)
        {
            return int.TryParse(value, out var num) ? num : 0;
        }
    }
}
