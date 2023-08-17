// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Graphics.Rendering.Dummy
{
    public class DummyShaderStorageBufferObject<T> : IShaderStorageBufferObject<T>
        where T : unmanaged, IEquatable<T>
    {
        private readonly T[] data;

        public DummyShaderStorageBufferObject(int size)
        {
            Size = size;
            data = new T[size];
        }

        public int Size { get; }

        public T this[int index]
        {
            get => data[index];
            set => data[index] = value;
        }

        public void Dispose()
        {
        }
    }
}
