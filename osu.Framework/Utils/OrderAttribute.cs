// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Extensions.EnumExtensions;

namespace osu.Framework.Utils
{
    /// <summary>
    /// Allows specifying ordering of <see cref="Enum"/> members, separate from the actual enum values.
    /// Only has an effect on members of <see cref="Enum"/> classes annotated with <see cref="HasOrderedElementsAttribute"/>.
    /// </summary>
    /// <remarks>
    /// Usually used for pretty-printing purposes.
    /// Methods from the <see cref="EnumExtensions"/> static class can be used to leverage the order defined with these attributes.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field)]
    public class OrderAttribute : Attribute
    {
        /// <summary>
        /// The sorting order of the annotated enum member.
        /// </summary>
        public readonly int Order;

        public OrderAttribute(int order)
        {
            Order = order;
        }
    }
}
