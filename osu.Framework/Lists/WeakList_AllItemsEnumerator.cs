// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace osu.Framework.Lists
{
    public partial class WeakList<T>
    {
        /// <summary>
        /// An enumerator over all items in a <see cref="WeakList{T}"/>. Does not guarantee the validity of items.
        /// </summary>
        private struct AllItemsEnumerator : IEnumerator<T>
        {
            private readonly WeakList<T> weakList;

            /// <summary>
            /// Creates a new <see cref="AllItemsEnumerator"/>.
            /// </summary>
            /// <param name="weakList">The <see cref="WeakList{T}"/> to enumerate over.</param>
            internal AllItemsEnumerator(WeakList<T> weakList)
            {
                this.weakList = weakList;

                CurrentItemIndex = weakList.listStart - 1; // The first MoveNext() should bring the iterator to the start
            }

            public bool MoveNext()
            {
                while (true)
                {
                    ++CurrentItemIndex;

                    // Check whether we're still within the valid range of the list.
                    if (CurrentItemIndex >= weakList.listEnd)
                        return false;

                    if (weakList.list[CurrentItemIndex].Reference != null)
                        return true;
                }
            }

            public void Reset()
            {
                CurrentItemIndex = weakList.listStart - 1;
            }

            public readonly T Current => throw new NotImplementedException("This enumerator doesn't support retrieving the current item.");

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool CheckEquals(int hashCode)
                => weakList.list[CurrentItemIndex].ObjectHashCode == hashCode;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool CheckEquals(WeakReference<T> weakReference)
                => weakList.list[CurrentItemIndex].Reference == weakReference;

            internal int CurrentItemIndex { get; private set; }

            readonly object IEnumerator.Current => Current;

            public void Dispose()
            {
            }
        }
    }
}
