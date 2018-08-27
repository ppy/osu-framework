// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

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
        /// Retrieves the value after a pivot from an <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of the items stored in the collection.</typeparam>
        /// <param name="collection">The collection to iterate on.</param>
        /// <param name="current">The pivot value.</param>
        /// <returns></returns>
        public static T GetNext<T>(this IEnumerable<T> collection, T current)
        {
            return collection.SkipWhile(i => !i.Equals(current)).Skip(1).FirstOrDefault();
        }

        /// <summary>
        /// Retrieves the value before a pivot from an <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of the items stored in the collection.</typeparam>
        /// <param name="collection">The collection to iterate on.</param>
        /// <param name="current">The pivot value.</param>
        /// <returns></returns>
        public static T GetPrevious<T>(this IEnumerable<T> collection, T current)
        {
            return collection.TakeWhile(i => !i.Equals(current)).LastOrDefault();
        }
    }
}
