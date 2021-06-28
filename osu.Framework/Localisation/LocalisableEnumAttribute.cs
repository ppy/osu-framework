// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Extensions;
using osu.Framework.Extensions.TypeExtensions;

namespace osu.Framework.Localisation
{
    /// <summary>
    /// Indicates that the values of an enum have <see cref="LocalisableString"/> descriptions.
    /// The descriptions can be retrieved through <see cref="ExtensionMethods.GetLocalisableDescription{T}"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Enum)]
    public sealed class LocalisableEnumAttribute : Attribute
    {
        /// <summary>
        /// The <see cref="EnumLocalisationMapper{T}"/> type that maps enum values to <see cref="LocalisableString"/>s.
        /// </summary>
        public readonly Type MapperType;

        /// <summary>
        /// Creates a new <see cref="LocalisableEnumAttribute"/>.
        /// </summary>
        /// <param name="mapperType">The <see cref="EnumLocalisationMapper{T}"/> type that maps enum values to <see cref="LocalisableString"/>s.</param>
        public LocalisableEnumAttribute(Type mapperType)
        {
            MapperType = mapperType;

            if (!typeof(IEnumLocalisationMapper).IsAssignableFrom(mapperType))
                throw new ArgumentException($"Type \"{mapperType.ReadableName()}\" must inherit from {nameof(EnumLocalisationMapper<Enum>)}.", nameof(mapperType));
        }
    }
}
