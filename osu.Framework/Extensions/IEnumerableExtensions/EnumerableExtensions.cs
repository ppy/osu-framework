// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;

namespace osu.Framework.Extensions.IEnumerableExtensions
{
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Performs an action on all the items in an IEnumerable collection.
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
        public static IEnumerable<T> Yield<T>(this T item) => new[] { item };

        /// <summary>
        /// Retrieves the item after a pivot from an <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of the items stored in the collection.</typeparam>
        /// <param name="collection">The collection to iterate on.</param>
        /// <param name="pivot">The pivot value.</param>
        /// <returns>The item in <paramref name="collection"/> appearing after <paramref name="pivot"/>, or null if no such item exists.</returns>
        public static T GetNext<T>(this IEnumerable<T> collection, T pivot)
        {
            return collection.SkipWhile(i => !EqualityComparer<T>.Default.Equals(i, pivot)).Skip(1).FirstOrDefault();
        }

        /// <summary>
        /// Retrieves the item before a pivot from an <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of the items stored in the collection.</typeparam>
        /// <param name="collection">The collection to iterate on.</param>
        /// <param name="pivot">The pivot value.</param>
        /// <returns>The item in <paramref name="collection"/> appearing before <paramref name="pivot"/>, or null if no such item exists.</returns>
        public static T GetPrevious<T>(this IEnumerable<T> collection, T pivot)
            => collection.Reverse().GetNext(pivot);

        /// <summary>
        /// Returns the most common prefix of every string in this <see cref="IEnumerable{T}"/>
        /// </summary>
        /// <param name="collection">The string <see cref="IEnumerable{T}"/></param>
        /// <returns>The most common prefix, or an empty string if no common prefix could be found.</returns>
        /// <example>
        /// "ab" == { "abc", "abd" }.GetCommonPrefix()
        /// </example>
        public static string GetCommonPrefix(this IEnumerable<string> collection)
        {
            ReadOnlySpan<char> prefix = default;

            foreach (string str in collection)
            {
                if (prefix.IsEmpty) // the first string
                {
                    prefix = str;
                    continue;
                }

                while (!prefix.IsEmpty)
                {
                    if (str.AsSpan().StartsWith(prefix))
                        break;
                    else
                        prefix = prefix[..^1];
                }

                if (prefix.IsEmpty)
                    return string.Empty;
            }

            return new string(prefix);
        }

        /// <summary>
        /// Get all combinations of provided sequences.
        /// </summary>
        public static IEnumerable<IEnumerable<T>> CartesianProduct<T>(this IEnumerable<IEnumerable<T>> sequences)
        {
            // https://stackoverflow.com/a/3098381
            IEnumerable<IEnumerable<T>> emptyProduct = new[] { Enumerable.Empty<T>() };
            return sequences.Aggregate(
                emptyProduct,
                (accumulator, sequence) =>
                    from accseq in accumulator
                    from item in sequence
                    select accseq.Concat(new[] { item })
            );
        }
    }
}
