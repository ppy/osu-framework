// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Containers
{
    public class GridContainerContent
    {
        public GridContainerContent(Drawable[][] drawables)
        {
            _ = new ArrayWrapper<Drawable>[drawables?.Length ?? 0];

            if (drawables != null)
            {
                for (int i = 0; i < drawables.Length; i++)
                {
                    if (drawables[i] != null)
                    {
                        _[i] = new ArrayWrapper<Drawable> { _ = new Drawable[drawables[i].Length] };

                        for (int j = 0; j < drawables[i].Length; j++)
                        {
                            _[i][j] = drawables[i][j];
                        }
                    }
                }
            }
        }

        public ArrayWrapper<Drawable>[] _ { get; set; }

        public ArrayWrapper<Drawable> this[int index]
        {
            get => _[index];
            set => _[index] = value;
        }

        public class ArrayWrapper<T>
        {
            public T[] _ { get; set; }

            public T this[int index]
            {
                get => _[index];
                set => _[index] = value;
            }
        }
    }
}
