// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.OML.Factories;

namespace osu.Framework.OML.ValueParsers
{
    public class OmlUriParser : IOmlValueParser<Uri>
    {
        public Uri Parse(string value)
        {
            return new Uri(value);
        }

        public IOmlValueParserFactory ParserFactory { get; set; }

        public object Parse(Type type, string value)
        {
            return new Uri(value);
        }
    }
}
