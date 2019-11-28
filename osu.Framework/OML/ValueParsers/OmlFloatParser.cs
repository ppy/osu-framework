// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using JetBrains.Annotations;

namespace osu.Framework.OML.ValueParsers
{
    [UsedImplicitly]
    public class OmlFloatParser : IOmlValueParser<float>
    {
        public float Parse(string value)
        {
            return (float)Parse(typeof(float), value);
        }

        public object Parse(Type type, string value)
        {
            return float.TryParse(value, out var num) ? num : 0f;
        }
    }
}
