// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections;
using System.Collections.Generic;

namespace osu.Framework.Lists
{
    public partial class WeakList<T>
    {
        /// <summary>
        /// An enumerator over only the valid items of a <see cref="WeakList{T}"/>.
        /// </summary>
        public struct ValidItemsEnumerator : IEnumerator<T>
        {
            private readonly WeakList<T> weakList;
            private int currentItemIndex;

            /// <summary>
            /// Creates a new <see cref="ValidItemsEnumerator"/>.
            /// </summary>
            /// <param name="weakList">The <see cref="WeakList{T}"/> to enumerate over.</param>
            internal ValidItemsEnumerator(WeakList<T> weakList)
            {
                this.weakList = weakList;

                currentItemIndex = weakList.listStart - 1; // The first MoveNext() should bring the iterator to the start
                Current = default!;
            }

            public bool MoveNext()
            {
                while (true)
                {
                    ++currentItemIndex;

                    // Check whether we're still within the valid range of the list.
                    if (currentItemIndex >= weakList.listEnd)
                        return false;

                    var weakReference = weakList.list[currentItemIndex].Reference;

                    // Check whether the reference exists.
                    if (weakReference == null || !weakReference.TryGetTarget(out var obj))
                    {
                        // If the reference doesn't exist, it must have previously been removed and can be skipped.
                        continue;
                    }

                    Current = obj;
                    return true;
                }
            }

            public void Reset()
            {
                currentItemIndex = weakList.listStart - 1;
                Current = default!;
            }

            public T Current { get; private set; }

            readonly object IEnumerator.Current => Current;

            public void Dispose()
            {
                Current = default!;
            }
        }
    }
}
