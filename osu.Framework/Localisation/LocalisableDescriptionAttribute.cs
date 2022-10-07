// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Extensions;

namespace osu.Framework.Localisation
{
    /// <summary>
    /// Specifies a <see cref="LocalisableString"/>-based description for the target element.
    /// The description can be retrieved through <see cref="ExtensionMethods.GetLocalisableDescription{T}"/>.
    /// </summary>
    /// <remarks>
    /// The C# language specification
    /// <a href="https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/attributes#attribute-parameter-types">
    /// only permits a limited set of parameter types for attributes,
    /// </a>
    /// and as such <see cref="LocalisableString"/> instances cannot be passed in directly.
    /// Therefore usages must pass both the target type in which the <see cref="LocalisableString"/> description is declared,
    /// as well as the name of the member which contains/returns the <see cref="LocalisableString"/> (using <see langword="nameof"/> for this is strongly encouraged).
    /// </remarks>
    /// <example>
    /// Assuming the following source class from which the <see cref="LocalisableString"/> description should be returned:
    /// <code>
    /// class Strings
    /// {
    ///     public static LocalisableString Example => "example string";
    /// }
    /// </code>
    /// the attribute should be used in the following way:
    /// <code>
    /// [LocalisableDescription(typeof(Strings), nameof(Strings.Example))]
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
