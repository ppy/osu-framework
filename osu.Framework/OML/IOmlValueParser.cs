// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using JetBrains.Annotations;
using osu.Framework.OML.Factories;

namespace osu.Framework.OML
{
    [UsedImplicitly]
    public interface IOmlValueParser
    {
        IOmlValueParserFactory ParserFactory { get; set; }
        object Parse(Type type, string value);
    }

    [UsedImplicitly]
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
