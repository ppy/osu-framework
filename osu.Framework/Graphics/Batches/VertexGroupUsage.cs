// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using System;
using osu.Framework.Graphics.Batches.Internal;
using osu.Framework.Graphics.OpenGL.Vertices;

namespace osu.Framework.Graphics.Batches
{
    /// <summary>
    /// A usage of a <see cref="VertexGroup{TVertex}"/>.
    /// </summary>
    /// <typeparam name="TInput">The input vertex type.</typeparam>
    public readonly ref struct VertexGroupUsage<TInput>
        where TInput : struct, IEquatable<TInput>, IVertex
    {
        /// <summary>
        /// Whether vertices need to be uploaded for the batch.
        /// If <c>false</c>, uploads will be omitted either automatically via <see cref="Add"/> or more aggressively via manual calls to <see cref="TrySkip"/>.
        /// </summary>
        private readonly bool uploadRequired;

        private readonly IVertexBatch batch;
        private readonly IVertexGroup vertices;

        internal VertexGroupUsage(IVertexBatch batch, IVertexGroup vertices, bool uploadRequired)
        {
            this.batch = batch;
            this.vertices = vertices;
            this.uploadRequired = uploadRequired;

            batch.UsageStarted();
        }

        public void Add(TInput vertex)
        {
            if (uploadRequired)
                batch.Add(vertices, vertex);
            else
            {
#if DEBUG && !NO_VBO_CONSISTENCY_CHECKS
                batch.EnsureCurrentVertex(vertex, "Vertex addition was skipped, but the contained vertex differs.");
#endif

                batch.Advance(1);
            }
        }

        public bool TrySkip(int count)
        {
#if DEBUG && !NO_VBO_CONSISTENCY_CHECKS
            return false;
#else
            if (uploadRequired)
                return false;

            batch.Advance(count);
            return true;
#endif
        }

        public void Dispose()
        {
            batch.UsageFinished();
        }
    }
}
