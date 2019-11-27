using System;

namespace osu.Framework.OML.ValueParsers
{
    public class OmlFloatParser : IOmlValueParser<float>
    {
        public float Parse(string value)
        {
            return (float) Parse(typeof(float), value);
        }

        public object Parse(Type type, string value)
        {
            return float.TryParse(value, out var num) ? num : 0f;
        }
    }
}
