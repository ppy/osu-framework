// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering.Vertices;

namespace osu.Framework.Graphics.Rendering.Dummy
{
    internal class DummyVertexBatch<T> : IVertexBatch<T>
        where T : unmanaged, IEquatable<T>, IVertex
    {
        public void Dispose()
        {
        }

        public int Size => 0;

        public int Draw() => 0;

        void IVertexBatch.ResetCounters()
        {
        }

        public Action<T> AddAction { get; } = _ => { };

        public void Add(T vertex)
        {
        }
    }
}
