// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.OpenGL.Vertices;

namespace osu.Framework.Graphics.Batches
{
    /// <summary>
    /// An <see cref="IVertexGroup{T}"/> which applies a mapping function for incoming vertices to a new <see cref="IVertexGroup{T}"/> of a different type.
    /// </summary>
    /// <typeparam name="TFrom">The input vertex type.</typeparam>
    /// <typeparam name="TTo">The output vertex type.</typeparam>
    public class VertexGroupTransformer<TFrom, TTo> : IVertexGroup<TFrom>
        where TFrom : struct, IEquatable<TFrom>, IVertex
        where TTo : struct, IEquatable<TTo>, IVertex
    {
        private readonly VertexGroup<TTo> target;
        private readonly Func<TFrom, TTo> transformer;

        public VertexGroupTransformer(VertexGroup<TTo> target, Func<TFrom, TTo> transformer)
        {
            this.target = target;
            this.transformer = transformer;
        }

        public void Add(TFrom vertex) => target.Add(transformer(vertex));

        public bool TrySkip(int count) => target.TrySkip(count);
    }
}
