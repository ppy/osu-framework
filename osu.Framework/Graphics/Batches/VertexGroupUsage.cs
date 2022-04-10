// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.OpenGL.Vertices;

namespace osu.Framework.Graphics.Batches
{
    /// <summary>
    /// A usage of a <see cref="VertexGroup{TVertex}"/>.
    /// </summary>
    /// <typeparam name="T">The vertex type.</typeparam>
    public readonly ref struct VertexGroupUsage<T>
        where T : struct, IEquatable<T>, IVertex
    {
        private readonly VertexBatch<T> batch;

        internal VertexGroupUsage(VertexBatch<T> batch)
        {
            this.batch = batch;
        }

        public void Dispose()
        {
            batch.GroupInUse = false;
        }
    }
}
