using System;

namespace osu.Framework.OML.ValueParsers
{
    public class OmlIntParser : IOmlValueParser<int>
    {
        public int Parse(string value)
        {
            return (int)Parse(typeof(int), value);
        }

        public object Parse(Type type, string value)
        {
            return int.TryParse(value, out var num) ? num : 0;
        }
    }
}
