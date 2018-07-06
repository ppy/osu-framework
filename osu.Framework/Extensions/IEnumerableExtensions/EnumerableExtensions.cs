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
        /// Get next value in an IEnumearble collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="guidList"></param>
        /// <param name="current"></param>
        /// <returns></returns>
        public static T GetNext<T>(this IEnumerable<T> guidList, T current)
        {
            return guidList.SkipWhile(i => !i.Equals(current)).Skip(1).FirstOrDefault();
        }

        /// <summary>
        /// Get pervious value in an IEnumearble collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="guidList"></param>
        /// <param name="current"></param>
        /// <returns></returns>
        public static T GetPrevious<T>(this IEnumerable<T> guidList, T current)
        {
            return guidList.TakeWhile(i => !i.Equals(current)).LastOrDefault();
        }
    }
}
