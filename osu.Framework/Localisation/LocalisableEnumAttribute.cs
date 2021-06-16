// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Localisation
{
    [AttributeUsage(AttributeTargets.Enum)]
    public sealed class LocalisableEnumAttribute : Attribute
    {
        public readonly Type MapperType;

        public LocalisableEnumAttribute(Type mapperType)
        {
            MapperType = mapperType;
        }
    }
}
