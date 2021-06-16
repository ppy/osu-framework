// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Localisation
{
    public abstract class EnumLocalisationMapper<T>
        where T : Enum
    {
        public abstract LocalisableString Map(T value);
    }
}
