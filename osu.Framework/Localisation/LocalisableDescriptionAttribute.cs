// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using System;

namespace osu.Framework.Localisation
{
    /// <summary>
    /// Specifies a localisable description for a property or event.
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public class LocalisableDescriptionAttribute : Attribute
    {
        public readonly LocalisableString Description;

        public LocalisableDescriptionAttribute(LocalisableString description)
        {
            Description = description;
        }

        public override bool Equals(object? obj)
        {
            if (obj == this)
                return true;

            return obj is LocalisableDescriptionAttribute other
                   && other.Description.Equals(Description);
        }

        public override int GetHashCode() => Description.GetHashCode();
    }
}
