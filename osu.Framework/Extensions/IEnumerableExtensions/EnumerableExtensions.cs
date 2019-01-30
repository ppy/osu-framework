// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;

namespace osu.Framework.Extensions.IEnumerableExtensions
{
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Performs an action on all the items in an IEnumearble collection.
        /// </summary>
        /// <typeparam name="T">The type of the items stored in the collection.</typeparam>
        /// <param name="collection">The collection to iterate on.</param>
        /// <param name="action">The action to be performed.</param>
        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            if (collection == null) return;

            foreach (var item in collection)
                action(item);
        }

        /// <summary>
        /// Wraps this object instance into an <see cref="IEnumerable{T}"/>
        /// consisting of a single item.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="item">The instance that will be wrapped.</param>
        /// <returns> An <see cref="IEnumerable{T}"/> consisting of a single item.</returns>
        public static IEnumerable<T> Yield<T>(this T item)
        {
            yield return item;
        }

        /// <summary>
        /// Retrieves the item after a pivot from an <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of the items stored in the collection.</typeparam>
        /// <param name="collection">The collection to iterate on.</param>
        /// <param name="pivot">The pivot value.</param>
        /// <returns>The item in <paramref name="collection"/> appearing after <paramref name="pivot"/>, or null if no such item exists.</returns>
        public static T GetNext<T>(this IEnumerable<T> collection, T pivot)
        {
            return collection.SkipWhile(i => !i.Equals(pivot)).Skip(1).FirstOrDefault();
        }

        /// <summary>
        /// Retrieves the item before a pivot from an <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of the items stored in the collection.</typeparam>
        /// <param name="collection">The collection to iterate on.</param>
        /// <param name="pivot">The pivot value.</param>
        /// <returns>The item in <paramref name="collection"/> appearing before <paramref name="pivot"/>, or null if no such item exists.</returns>
        public static T GetPrevious<T>(this IEnumerable<T> collection, T pivot)
        {
            return collection.TakeWhile(i => !i.Equals(pivot)).LastOrDefault();
        }
    }
}
