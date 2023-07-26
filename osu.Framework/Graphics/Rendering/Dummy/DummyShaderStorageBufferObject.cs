// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Graphics.Rendering.Dummy
{
    public class DummyShaderStorageBufferObject<T> : IShaderStorageBufferObject<T>
        where T : unmanaged, IEquatable<T>
    {
        public DummyShaderStorageBufferObject(int size)
        {
            Size = size;
        }

        public int Size { get; }

        public T this[int index]
        {
            get => default;
            set { }
        }

        public void Dispose()
        {
        }
    }
}
