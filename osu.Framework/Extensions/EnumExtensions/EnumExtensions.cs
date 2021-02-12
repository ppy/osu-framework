// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Utils;

namespace osu.Framework.Extensions.EnumExtensions
{
    public static class EnumExtensions
    {
        /// <summary>
        /// Get values of an enum in order. Supports custom ordering via <see cref="OrderAttribute"/>.
        /// </summary>
        public static IEnumerable<T> GetValuesInOrder<T>()
        {
            var type = typeof(T);

            if (!type.IsEnum)
                throw new InvalidOperationException($"{typeof(T)} must be an enum");

            IEnumerable<T> items = (T[])Enum.GetValues(type);

            return GetValuesInOrder(items);
        }

        /// <summary>
        /// Get values of a collection of enum values in order. Supports custom ordering via <see cref="OrderAttribute"/>.
        /// </summary>
        public static IEnumerable<T> GetValuesInOrder<T>(this IEnumerable<T> items)
        {
            var type = typeof(T);

            if (!type.IsEnum)
                throw new InvalidOperationException($"{typeof(T)} must be an enum");

            if (!(Attribute.GetCustomAttribute(type, typeof(HasOrderedElementsAttribute)) is HasOrderedElementsAttribute orderedAttr))
                return items;

            return items.OrderBy(i =>
            {
                var fieldInfo = type.GetField(i.ToString());

                if (fieldInfo?.GetCustomAttributes(typeof(OrderAttribute), false).FirstOrDefault() is OrderAttribute attr)
                    return attr.Order;

                if (orderedAttr.AllowPartialOrdering)
                    return Convert.ToInt32(i);

                throw new ArgumentException($"Not all values of {typeof(T)} have {nameof(OrderAttribute)} specified.");
            });
        }
    }
}
