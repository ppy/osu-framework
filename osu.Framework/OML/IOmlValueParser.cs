using System;

namespace osu.Framework.OML
{
    public interface IOmlValueParser
    {
        object Parse(Type type, string value);
    }

    public interface IOmlValueParser<out T> : IOmlValueParser
    {
        T Parse(string value);
    }

    public interface IOmlEnumParser
    {
        Te Parse<Te>(string value) where Te : struct;
        object Parse(Type type, string value);
    }
}
