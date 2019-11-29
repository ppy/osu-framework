// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using JetBrains.Annotations;
using osu.Framework.OML.Factories;
using osuTK;

namespace osu.Framework.OML.ValueParsers
{
    [UsedImplicitly]
    public class OmlVector2Parser : IOmlValueParser<Vector2>
    {
        public Vector2 Parse(string value)
        {
            return (Vector2)Parse(typeof(Vector2), value);
        }

        public IOmlValueParserFactory ParserFactory { get; set; }

        public object Parse(Type type, string value)
        {
            var floatParser = ParserFactory.Create<float>();

            var data = value.Split(",");

            return data.Length switch
            {
                1 => new Vector2(floatParser.Parse(data[0])),
                2 => new Vector2(floatParser.Parse(data[0]), floatParser.Parse(data[1])),
                _ => Vector2.Zero
            };
        }
    }
}
