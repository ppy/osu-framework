// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections;
using System.Collections.Generic;

namespace osu.Framework.Lists
{
    /// <summary>
    /// A list maintaining weak reference of objects.
    /// </summary>
    /// <typeparam name="T">Type of items tracked by weak reference.</typeparam>
    public partial class WeakList<T> : IWeakList<T>, IEnumerable<T>
        where T : class
    {
        /// <summary>
        /// The number of items that can be added or removed from this <see cref="WeakList{T}"/> before the next <see cref="Add(T)"/> to cause the list to be trimmed.
        /// </summary>
        private const int opportunistic_trim_threshold = 100;

        private readonly List<InvalidatableWeakReference> list = new List<InvalidatableWeakReference>();
        private int listStart; // The inclusive starting index in the list.
        private int listEnd; // The exclusive ending index in the list.

        /// <summary>
        /// The number of items that have been added or removed from this <see cref="WeakList{T}"/> since it was last trimmed.
        /// Upon reaching the <see cref="opportunistic_trim_threshold"/>, this list will be trimmed on the next <see cref="Add(T)"/>.
        /// </summary>
        private int countChangesSinceTrim;

        public void Add(T obj) => add(new InvalidatableWeakReference(obj));

        public void Add(WeakReference<T> weakReference) => add(new InvalidatableWeakReference(weakReference));

        private void add(in InvalidatableWeakReference item)
        {
            if (countChangesSinceTrim > opportunistic_trim_threshold)
                trim();

            if (listEnd < list.Count)
            {
                list[listEnd] = item;
                countChangesSinceTrim--;
            }
            else
            {
                list.Add(item);
                countChangesSinceTrim++;
            }

            listEnd++;
        }

        public bool Remove(T item)
        {
            int hashCode = EqualityComparer<T>.Default.GetHashCode(item);

            for (int i = listStart; i < listEnd; i++)
            {
                var reference = list[i].Reference;

                // Check if the object is valid.
                if (reference == null)
                    continue;

                // Compare by hash code (fast).
                if (list[i].ObjectHashCode != hashCode)
                    continue;

                // Compare by object equality (slow).
                if (!reference.TryGetTarget(out var target) || target != item)
                    continue;

                RemoveAt(i - listStart);
                return true;
            }

            return false;
        }

        public bool Remove(WeakReference<T> weakReference)
        {
            for (int i = listStart; i < listEnd; i++)
            {
                // Check if the object is valid.
                if (list[i].Reference != weakReference)
                    continue;

                RemoveAt(i - listStart);
                return true;
            }

            return false;
        }

        public void RemoveAt(int index)
        {
            // Move the index to the valid range of the list.
            index += listStart;

            if (index < listStart || index >= listEnd)
                throw new ArgumentOutOfRangeException(nameof(index));

            list[index] = default;

            if (index == listStart)
                listStart++;
            else if (index == listEnd - 1)
                listEnd--;

            countChangesSinceTrim++;
        }

        public bool Contains(T item)
        {
            int hashCode = EqualityComparer<T>.Default.GetHashCode(item);

            for (int i = listStart; i < listEnd; i++)
            {
                var reference = list[i].Reference;

                // Check if the object is valid.
                if (reference == null)
                    continue;

                // Compare by hash code (fast).
                if (list[i].ObjectHashCode != hashCode)
                    continue;

                // Compare by object equality (slow).
                if (!reference.TryGetTarget(out var target) || target != item)
                    continue;

                return true;
            }

            return false;
        }

        public bool Contains(WeakReference<T> weakReference)
        {
            for (int i = listStart; i < listEnd; i++)
            {
                // Check if the object is valid.
                if (list[i].Reference == weakReference)
                    return true;
            }

            return false;
        }

        public void Clear()
        {
            listStart = listEnd = 0;
            countChangesSinceTrim = list.Count;
        }

        public ValidItemsEnumerator GetEnumerator()
        {
            trim();
            return new ValidItemsEnumerator(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private void trim()
        {
            // Trim from the sides - items that have been removed.
            list.RemoveRange(listEnd, list.Count - listEnd);
            list.RemoveRange(0, listStart);

            // Trim all items whose references are no longer alive.
            list.RemoveAll(item => item.Reference == null || !item.Reference.TryGetTarget(out _));

            // After the trim, the valid range represents the full list.
            listStart = 0;
            listEnd = list.Count;
            countChangesSinceTrim = 0;
        }

        private readonly struct InvalidatableWeakReference
        {
            public readonly WeakReference<T>? Reference;

            /// <summary>
            /// Hash code of the target of <see cref="Reference"/>.
            /// </summary>
            public readonly int ObjectHashCode;

            public InvalidatableWeakReference(T reference)
            {
                Reference = new WeakReference<T>(reference);
                ObjectHashCode = EqualityComparer<T>.Default.GetHashCode(reference);
            }

            public InvalidatableWeakReference(WeakReference<T> weakReference)
            {
                Reference = weakReference;
                ObjectHashCode = !weakReference.TryGetTarget(out var target) ? 0 : EqualityComparer<T>.Default.GetHashCode(target);
            }
        }
    }
}
