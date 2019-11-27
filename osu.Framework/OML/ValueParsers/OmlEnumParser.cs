using System;

namespace osu.Framework.OML.ValueParsers
{
    public class OmlEnumParser : IOmlEnumParser
    {
        public Te Parse<Te>(string value)
            where Te : struct
        {
            return (Te)Parse(typeof(Te), value);
        }

        public object Parse(Type type, string value)
        {
            return Enum.TryParse(type, value, out var enm) ? enm : default;
        }
    }
}
