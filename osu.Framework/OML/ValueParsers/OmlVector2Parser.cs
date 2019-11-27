// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osuTK;

namespace osu.Framework.OML.ValueParsers
{
    public class OmlVector2Parser : IOmlValueParser<Vector2>
    {
        public Vector2 Parse(string value)
        {
            return (Vector2) Parse(typeof(Vector2), value);
        }

        public object Parse(Type type, string value)
        {
            var data = value.Split(",");

            return data.Length switch
            {
                1 when float.TryParse(data[0], out var xy) => new Vector2(xy),
                2 when float.TryParse(data[0], out var x) && float.TryParse(data[0], out var y) => new Vector2(x, y),
                _ => Vector2.Zero
            };
        }
    }
}
