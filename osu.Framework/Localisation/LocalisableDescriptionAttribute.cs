// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Extensions;

namespace osu.Framework.Localisation
{
    /// <summary>
    /// Specifies a <see cref="LocalisableString"/>-based description for the target element.
    /// The description can be retrieved through <see cref="ExtensionMethods.GetLocalisableDescription{T}"/>.
    /// </summary>
    /// <example>
    /// Since attribute parameter types are limited and <see cref="LocalisableString"/>s can't be directly passed to the attribute,
    /// the type declaring the static member which holds the <see cref="LocalisableString"/> and the member's name will both be required.
    ///
    /// <code>
    /// class Strings
    /// {
    ///     public static LocalisableString Xyz => ...;
    /// }
    ///
    /// [LocalisableDescription(typeof(Strings), nameof(Strings.Xyz))]
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.All)]
    public sealed class LocalisableDescriptionAttribute : Attribute
    {
        /// <summary>
        /// The type declaring the static member providing the localisable description.
        /// </summary>
        public readonly Type DeclaringType;

        /// <summary>
        /// The name of the static member providing the localisable description.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Creates a new <see cref="LocalisableDescriptionAttribute"/>.
        /// </summary>
        public LocalisableDescriptionAttribute(Type declaringType, string name)
        {
            DeclaringType = declaringType;
            Name = name;
        }
    }
}
