// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace osu.Framework.Lists
{
    /// <summary>
    /// A list that lazily applies a transformation to elements of a source list to a target type when its indexed or iterated.
    /// </summary>
    /// <typeparam name="TSource">The type of the source elements.</typeparam>
    /// <typeparam name="TTarget">The type of the target elements.</typeparam>
    public class LazyList<TSource, TTarget> : IReadOnlyList<TTarget>
    {
        private readonly IReadOnlyList<TSource> source;
        private readonly Func<TSource, TTarget> map;

        /// <summary>
        /// Gets the element at the specified index from source, applies the transformation to it and returns the transformed element.
        /// </summary>
        /// <param name="index">The index of the element.</param>
        /// <returns>The transformed element at the specified index.</returns>
        public TTarget this[int index] => map(source[index]);

        /// <summary>
        /// The number of elements in this lazy list.
        /// </summary>
        public int Count => source.Count;

        /// <summary>
        /// Constructs a new lazy list from the given source list and with the given transformation.
        /// </summary>
        /// <param name="source">The source list to get elements from.</param>
        /// <param name="map"></param>
        public LazyList(IReadOnlyList<TSource> source, Func<TSource, TTarget> map)
        {
            this.source = source;
            this.map = map;
        }

        IEnumerator<TTarget> IEnumerable<TTarget>.GetEnumerator() => source.Select(s => map(s)).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<TTarget>)this).GetEnumerator();
    }
}
