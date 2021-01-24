// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;

namespace osu.Framework.Utils
{
    public static class OrderAttributeUtils
    {
        /// <summary>
        /// Get values of an enum in order. Supports custom ordering via <see cref="OrderAttribute"/>.
        /// </summary>
        public static IEnumerable<T> GetValuesInOrder<T>()
            where T : struct, Enum
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
        public static IEnumerable<T> GetValuesInOrder<T>(IEnumerable<T> items)
            where T : struct, Enum
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

    [AttributeUsage(AttributeTargets.Field)]
    public class OrderAttribute : Attribute
    {
        public readonly int Order;

        public OrderAttribute(int order)
        {
            Order = order;
        }
    }

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
