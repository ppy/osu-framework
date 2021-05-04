// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Extensions.EnumExtensions;

namespace osu.Framework.Utils
{
    /// <summary>
    /// Marker attribute for <see cref="Enum"/> classes whose members are annotated with <see cref="OrderAttribute"/>.
    /// Methods from the <see cref="EnumExtensions"/> static class use the order defined with these attributes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Enum)]
    public class HasOrderedElementsAttribute : Attribute
    {
        /// <summary>
        /// Allow for partially ordering <see cref="Enum"/> members.
        /// Members without an <see cref="OrderAttribute"/> will default to their value as ordering key.
        /// </summary>
        public bool AllowPartialOrdering { get; set; }
    }
}
