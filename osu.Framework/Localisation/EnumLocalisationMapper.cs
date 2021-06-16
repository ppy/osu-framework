// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Localisation
{
    /// <summary>
    /// Describes the values of an <see cref="Enum"/> type by <see cref="LocalisableString"/>s.
    /// </summary>
    /// <typeparam name="T">The <see cref="Enum"/> type.</typeparam>
    public abstract class EnumLocalisationMapper<T> : IEnumLocalisationMapper
        where T : Enum
    {
        /// <summary>
        /// Describes a <typeparamref name="T"/> value by a <see cref="LocalisableString"/>.
        /// </summary>
        /// <param name="value">The value to map.</param>
        /// <returns>The <see cref="LocalisableString"/> describing <paramref name="value"/>.</returns>
        public abstract LocalisableString Map(T value);
    }

    /// <summary>
    /// Marker class for <see cref="EnumLocalisationMapper{T}"/>. Do not use.
    /// </summary>
    internal interface IEnumLocalisationMapper
    {
    }
}
