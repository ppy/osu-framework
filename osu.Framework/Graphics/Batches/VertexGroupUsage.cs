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
    // This object MUST be readonly and is recommended to be passed around as an `in` parameter.
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

        /// <summary>
        /// Adds a vertex to the group.
        /// </summary>
        /// <param name="vertex">The vertex to add.</param>
        public void Add(TInput vertex)
        {
            if (uploadRequired)
                batch.Add(vertices, vertex);
            else
            {
#if DEBUG && !NO_VBO_CONSISTENCY_CHECKS
                batch.EnsureCurrentVertex(vertices, vertex, "Vertex addition was skipped, but the contained vertex differs.");
#endif

                batch.Advance(1);
            }
        }

        /// <summary>
        /// Attempts to skip a number of vertices from the group.
        /// </summary>
        /// <remarks>
        /// This may be used to skip construction of vertices. If this returns <c>true</c>, any calls to <see cref="Add"/> MUST be omitted for the relevant vertices.
        /// <br />
        /// <br />
        /// IMPORTANT: Other states such as active textures and shaders must be brought into the correct states regardless of return value.
        /// </remarks>
        /// <param name="count">The number of vertices to skip.</param>
        /// <returns>Whether the vertices were skipped.</returns>
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
