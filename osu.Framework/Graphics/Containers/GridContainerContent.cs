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
    public class GridContainerContent : IEnumerable<GridContainerContent.ArrayWrapper<Drawable>>
    {
        public event Action ContentChanged;

        public ArrayWrapper<ArrayWrapper<Drawable>> WrappedArray { get; }

        public ArrayWrapper<Drawable> this[int index]
        {
            get => WrappedArray[index];
            set => WrappedArray[index] = value;
        }

        public static implicit operator Drawable[][](GridContainerContent content) => content.source;

        public static implicit operator GridContainerContent(Drawable[][] drawables) => new GridContainerContent(drawables);

        private readonly Drawable[][] source;

        private GridContainerContent(Drawable[][] drawables)
        {
            source = drawables;

            WrappedArray = new ArrayWrapper<ArrayWrapper<Drawable>>
            {
                WrappedArray = new ArrayWrapper<Drawable>[drawables?.Length ?? 0]
            };

            WrappedArray.ArrayElementChanged += onArrayElementChanged;

            if (drawables != null)
            {
                for (int i = 0; i < drawables.Length; i++)
                {
                    if (drawables[i] != null)
                    {
                        this[i] = new ArrayWrapper<Drawable> { WrappedArray = drawables[i] };
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
        public class ArrayWrapper<T> : IEnumerable<T>
        {
            public event Action ArrayElementChanged;

            public T[] WrappedArray { get; set; }

            public T this[int index]
            {
                get => WrappedArray[index];
                set
                {
                    if (EqualityComparer<T>.Default.Equals(WrappedArray[index], value))
                        return;

                    WrappedArray[index] = value;
                    ArrayElementChanged?.Invoke();
                }
            }

            public IEnumerator<T> GetEnumerator()
            {
                return ((IEnumerable<T>)WrappedArray).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return WrappedArray.GetEnumerator();
            }
        }

        public IEnumerator<ArrayWrapper<Drawable>> GetEnumerator()
        {
            return ((IEnumerable<ArrayWrapper<Drawable>>)WrappedArray.WrappedArray).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return WrappedArray.WrappedArray.GetEnumerator();
        }
    }
}
