// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// Implements a jagged array behavior with element change notifications
    /// </summary>
    public class GridContainerContent
    {
        public event Action ContentChanged;

        public GridContainerContent(Drawable[][] drawables)
        {
            source = drawables;

            _ = new ArrayWrapper<ArrayWrapper<Drawable>>
            {
                _ = new ArrayWrapper<Drawable>[drawables?.Length ?? 0]
            };

            _.ArrayElementChanged += onArrayElementChanged;

            if (drawables != null)
            {
                for (int i = 0; i < drawables.Length; i++)
                {
                    if (drawables[i] != null)
                    {
                        this[i] = new ArrayWrapper<Drawable> { _ = drawables[i] };
                        this[i].ArrayElementChanged += onArrayElementChanged;
                    }
                }
            }
        }

        private readonly Drawable[][] source;

        private void onArrayElementChanged()
        {
            ContentChanged?.Invoke();
        }

        public ArrayWrapper<ArrayWrapper<Drawable>> _ { get; }

        public static implicit operator Drawable[][](GridContainerContent content) => content.source;

        public static implicit operator GridContainerContent(Drawable[][] drawables) => new GridContainerContent(drawables);

        public ArrayWrapper<Drawable> this[int index]
        {
            get => _[index];
            set => _[index] = value;
        }

        /// <summary>
        /// Wraps an array and provides a custom indexer with element change notification
        /// </summary>
        /// <typeparam name="T">An array data type</typeparam>
        public class ArrayWrapper<T>
        {
            public event Action ArrayElementChanged;

            public T[] _ { get; set; }

            public T this[int index]
            {
                get => _[index];
                set
                {
                    _[index] = value;
                    ArrayElementChanged?.Invoke();
                }
            }
        }
    }
}
