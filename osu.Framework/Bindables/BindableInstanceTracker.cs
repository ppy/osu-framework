// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace osu.Framework.Bindables
{
    /// <summary>
    /// Facilitates tracking of bindable instances through recursive invocations on bound bindables.
    /// Can be provided a small array for fast/naive de-duplication while falling back to <see cref="HashSet{T}"/> when there are too many bindable instances.
    /// </summary>
    internal ref struct BindableInstanceTracker
    {
        private readonly Span<int> fastStorage;
        private HashSet<int>? slowStorage;
        private int count;

        /// <summary>
        /// Creates a bindable instance tracker.
        /// </summary>
        /// <param name="fastStorage">A small array for far de-duplication. Usually allocated on the stack.</param>
        public BindableInstanceTracker(Span<int> fastStorage)
        {
            this.fastStorage = fastStorage;
        }

        /// <summary>
        /// Adds an instance to the tracker.
        /// </summary>
        /// <param name="id">A unique instance ID.</param>
        /// <returns>Whether the instance was previously seen.</returns>
        public bool Add(int id)
        {
            Debug.Assert(id != 0);

            bool result = count < fastStorage.Length ? addFast(id) : addSlow(id);
            count += result ? 1 : 0;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool addSlow(int id)
        {
            if (slowStorage == null)
            {
                slowStorage = [];
                for (int i = 0; i < count; i++)
                    slowStorage.Add(fastStorage[i]);
            }

            return slowStorage.Add(id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool addFast(int id)
        {
            if (fastStorage[..count].Contains(id))
                return false;

            fastStorage[count] = id;
            return true;
        }
    }
}
