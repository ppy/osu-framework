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
    public class GridContainerContent : IList<IList<Drawable>>, IEquatable<GridContainerContent>
    {
        public event Action ContentChanged;

        public int IndexOf(IList<Drawable> item)
        {
            throw new NotSupportedException();
        }

        public void Insert(int index, IList<Drawable> item)
        {
            throw new NotSupportedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        public IList<Drawable> this[int index]
        {
            get => wrappedArray[index];
            set
            {
                if (value is ArrayWrapper<Drawable> drawables)
                {
                    if (wrappedArray[index] != null)
                        wrappedArray[index].ArrayElementChanged -= onArrayElementChanged;

                    wrappedArray[index] = drawables;
                    drawables.ArrayElementChanged += onArrayElementChanged;
                }
            }
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
                        var arrayWrapper = new ArrayWrapper<Drawable>(drawables[i]);
                        this[i] = arrayWrapper;
                        arrayWrapper.ArrayElementChanged += onArrayElementChanged;
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
        private class ArrayWrapper<T> : IList<T>
        {
            public event Action ArrayElementChanged;

            private T[] wrappedArray { get; set; }

            public ArrayWrapper(T[] arrayToWrap)
            {
                wrappedArray = arrayToWrap;
            }

            public int IndexOf(T item)
            {
                throw new NotSupportedException();
            }

            public void Insert(int index, T item)
            {
                throw new NotSupportedException();
            }

            public void RemoveAt(int index)
            {
                throw new NotSupportedException();
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

            public void Add(T item)
            {
                throw new NotSupportedException();
            }

            public void Clear()
            {
                throw new NotSupportedException();
            }

            public bool Contains(T item)
            {
                throw new NotSupportedException();
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                throw new NotSupportedException();
            }

            public bool Remove(T item)
            {
                throw new NotSupportedException();
            }

            public int Count => wrappedArray.Length;
            public bool IsReadOnly => true;
        }

        public IEnumerator<IList<Drawable>> GetEnumerator()
        {
            return wrappedArray.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return wrappedArray.GetEnumerator();
        }

        public void Add(IList<Drawable> item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(IList<Drawable> item)
        {
            throw new NotSupportedException();
        }

        public void CopyTo(IList<Drawable>[] array, int arrayIndex)
        {
            throw new NotSupportedException();
        }

        public bool Remove(IList<Drawable> item)
        {
            throw new NotSupportedException();
        }

        public int Count => wrappedArray.Count;
        public bool IsReadOnly => true;

        public bool Equals(GridContainerContent other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return source == other.source;
        }

        IEnumerator<IList<Drawable>> IEnumerable<IList<Drawable>>.GetEnumerator()
        {
            return GetEnumerator();
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
