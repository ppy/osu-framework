// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.OpenGL.Vertices;

namespace osu.Framework.Graphics.Batches
{
    public readonly struct DelegatingVertexGroup<T> : IVertexGroup<T>
        where T : struct, IEquatable<T>, IVertex
    {
        private readonly Action<T> action;

        public DelegatingVertexGroup(Action<T> action)
        {
            this.action = action;
        }

        public void Add(T vertex) => action(vertex);
    }
}
