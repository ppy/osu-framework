// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;

namespace osu.Framework.Lists
{
    /// <summary>
    /// A slim interface for a list which stores weak references of objects.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IWeakList<T>
        where T : class
    {
        /// <summary>
        /// Adds an item to this list. The item is added as a weak reference.
        /// </summary>
        /// <param name="item">The item to add.</param>
        void Add(T item);

        /// <summary>
        /// Adds a weak reference to this list.
        /// </summary>
        /// <param name="weakReference">The weak reference to add.</param>
        void Add(WeakReference<T> weakReference);

        /// <summary>
        /// Removes an item from this list.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <returns>Whether the item was removed.</returns>
        bool Remove(T item);

        /// <summary>
        /// Removes a weak reference from this list.
        /// </summary>
        /// <param name="weakReference"></param>
        bool Remove(WeakReference<T> weakReference);

        /// <summary>
        /// Removes an item at an index from this list.
        /// </summary>
        /// <param name="index">The index of the item to remove.</param>
        void RemoveAt(int index);

        /// <summary>
        /// Searches for an item in the list.
        /// </summary>
        /// <param name="item">The item to search for.</param>
        /// <returns>Whether the item is alive and in this list.</returns>
        bool Contains(T item);

        /// <summary>
        /// Searches for a weak reference in the list.
        /// </summary>
        /// <param name="weakReference">The weak reference to search for.</param>
        /// <returns>Whether the weak reference is in the list.</returns>
        bool Contains(WeakReference<T> weakReference);

        /// <summary>
        /// Clears all items from this list.
        /// </summary>
        void Clear();
    }
}
