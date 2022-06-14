// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace osu.Framework.Lists
{
    /// <summary>
    /// A wrapper for an array that provides notifications when elements are changed.
    /// </summary>
    /// <typeparam name="T">The type of elements stored in the array.</typeparam>
    public class ObservableArray<T> : IReadOnlyList<T>, IEquatable<ObservableArray<T>>, INotifyArrayChanged
    {
        /// <summary>
        /// Invoked when an element of the array is changed via <see cref="this[int]"/>.
        /// </summary>
        public event Action ArrayElementChanged;

        [NotNull]
        private readonly T[] wrappedArray;

        public ObservableArray(T[] arrayToWrap)
        {
            wrappedArray = arrayToWrap ?? throw new ArgumentNullException(nameof(arrayToWrap));
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)wrappedArray).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return wrappedArray.GetEnumerator();
        }

        public bool Equals(ObservableArray<T> other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return wrappedArray == other.wrappedArray;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            return obj.GetType() == GetType() && Equals((ObservableArray<T>)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(wrappedArray);
        }

        public int Count => wrappedArray.Length;

        public T this[int index]
        {
            get => wrappedArray[index];
            set
            {
                if (EqualityComparer<T>.Default.Equals(wrappedArray[index], value))
                    return;

                var previousValue = wrappedArray[index];
                if (previousValue is INotifyArrayChanged previousNotifier)
                    previousNotifier.ArrayElementChanged -= OnArrayElementChanged;

                wrappedArray[index] = value;
                if (value is INotifyArrayChanged notifier)
                    notifier.ArrayElementChanged += OnArrayElementChanged;

                OnArrayElementChanged();
            }
        }

        protected void OnArrayElementChanged()
        {
            ArrayElementChanged?.Invoke();
        }

        public static implicit operator ObservableArray<T>(T[] source) => source == null ? null : new ObservableArray<T>(source);
    }
}
