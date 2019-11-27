// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
