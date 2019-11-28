// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.OML.ValueParsers
{
    public class OmlBoolParser : IOmlValueParser<bool>
    {
        public bool Parse(string value)
        {
            return (bool)Parse(typeof(bool), value);
        }

        public object Parse(Type type, string value)
        {
            return bool.TryParse(value, out var b) && b;
        }
    }
}
