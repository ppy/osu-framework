// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.CompilerServices;
using osu.Framework.Graphics.OpenGL.Vertices;

namespace osu.Framework.Graphics.Batches
{
    /// <summary>
    /// An <see cref="IVertexGroup{T}"/> which applies a mapping function for incoming vertices to a new <see cref="IVertexGroup{T}"/> of a different type.
    /// </summary>
    /// <typeparam name="TFrom">The input vertex type.</typeparam>
    /// <typeparam name="TTo">The output vertex type.</typeparam>
    public readonly unsafe struct VertexGroupTransformer<TFrom, TTo> : IVertexGroup<TFrom>
        where TFrom : struct, IEquatable<TFrom>, IVertex
        where TTo : struct, IEquatable<TTo>, IVertex
    {
        private readonly void* target;
        private readonly Func<TFrom, TTo> transformer;

        public VertexGroupTransformer(ref VertexGroup<TTo> target, Func<TFrom, TTo> transformer)
        {
            this.target = Unsafe.AsPointer(ref target);
            this.transformer = transformer;
        }

        public void Add(TFrom vertex) => getGroup().Add(transformer(vertex));

        public bool TrySkip(int count) => getGroup().TrySkip(count);

        private ref VertexGroup<TTo> getGroup() => ref Unsafe.AsRef<VertexGroup<TTo>>(target);
    }
}
