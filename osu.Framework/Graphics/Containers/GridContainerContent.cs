// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections;
using System.Collections.Generic;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// Implements a jagged array behavior with element change notifications
    /// </summary>
    public class GridContainerContent : IReadOnlyList<GridContainerContent.ArrayWrapper<Drawable>>, IEquatable<GridContainerContent>
    {
        public event Action ContentChanged;

        public ArrayWrapper<Drawable> this[int index]
        {
            get => wrappedArray[index];
            set => wrappedArray[index] = value;
        }

        public static implicit operator Drawable[][](GridContainerContent content) => content.source;

        public static implicit operator GridContainerContent(Drawable[][] drawables) => new GridContainerContent(drawables);

        private readonly Drawable[][] source;

        private ArrayWrapper<ArrayWrapper<Drawable>> wrappedArray { get; }

        private GridContainerContent(Drawable[][] drawables)
        {
            source = drawables;

            wrappedArray = new ArrayWrapper<ArrayWrapper<Drawable>>(new ArrayWrapper<Drawable>[drawables?.Length ?? 0]);

            wrappedArray.ArrayElementChanged += onArrayElementChanged;

            if (drawables != null)
            {
                for (int i = 0; i < drawables.Length; i++)
                {
                    if (drawables[i] != null)
                    {
                        this[i] = new ArrayWrapper<Drawable>(drawables[i]);
                        this[i].ArrayElementChanged += onArrayElementChanged;
                    }
                }
            }
        }

        private void onArrayElementChanged()
        {
            ContentChanged?.Invoke();
        }

        /// <summary>
        /// Wraps an array and provides a custom indexer with element change notification
        /// </summary>
        /// <typeparam name="T">An array data type</typeparam>
        public class ArrayWrapper<T> : IReadOnlyList<T>
        {
            public event Action ArrayElementChanged;

            private T[] wrappedArray { get; set; }

            public ArrayWrapper(T[] arrayToWrap)
            {
                wrappedArray = arrayToWrap;
            }

            public T this[int index]
            {
                get => wrappedArray[index];
                set
                {
                    if (EqualityComparer<T>.Default.Equals(wrappedArray[index], value))
                        return;

                    wrappedArray[index] = value;
                    ArrayElementChanged?.Invoke();
                }
            }

            public IEnumerator<T> GetEnumerator()
            {
                return ((IEnumerable<T>)wrappedArray).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return wrappedArray.GetEnumerator();
            }

            public int Count => wrappedArray.Length;
        }

        public IEnumerator<ArrayWrapper<Drawable>> GetEnumerator()
        {
            return ((IEnumerable<ArrayWrapper<Drawable>>)wrappedArray).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return wrappedArray.GetEnumerator();
        }

        public int Count => wrappedArray.Count;

        public bool Equals(GridContainerContent other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return source == other.source;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            return obj.GetType() == GetType() && Equals((GridContainerContent)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(source);
        }
    }
}
