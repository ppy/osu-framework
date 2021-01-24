// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Utils
{
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
