// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Graphics.Containers
{
    public class GridContainerContent
    {
        public event Action ContentChanged;

        public GridContainerContent(Drawable[][] drawables)
        {
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
                        this[i] = new ArrayWrapper<Drawable> { _ = new Drawable[drawables[i].Length] };

                        this[i].ArrayElementChanged += onArrayElementChanged;

                        for (int j = 0; j < drawables[i].Length; j++)
                        {
                            this[i][j] = drawables[i][j];
                        }
                    }
                }
            }
        }

        private void onArrayElementChanged()
        {
            ContentChanged?.Invoke();
        }

        public ArrayWrapper<ArrayWrapper<Drawable>> _ { get; }

        public ArrayWrapper<Drawable> this[int index]
        {
            get => _[index];
            set => _[index] = value;
        }

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
