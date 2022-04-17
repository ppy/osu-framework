// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using System;
using System.Diagnostics;
using osu.Framework.Graphics.Batches.Internal;
using osu.Framework.Graphics.OpenGL.Vertices;

namespace osu.Framework.Graphics.Batches
{
    /// <summary>
    /// A grouping of vertices in a <see cref="DrawNode"/>.
    /// </summary>
    /// <remarks>
    /// Ensure to store this object in the <see cref="DrawNode"/>.
    /// </remarks>
    /// <typeparam name="TVertex">The input/output vertex type.</typeparam>
    public class VertexGroup<TVertex> : VertexGroup<TVertex, TVertex>
        where TVertex : struct, IEquatable<TVertex>, IVertex
    {
        public VertexGroup()
            : base(v => v)
        {
        }
    }

    /// <summary>
    /// A grouping of vertices in a <see cref="DrawNode"/> where the incoming vertices need to be converted in order to be added to the group.
    /// </summary>
    /// <remarks>
    /// Ensure to store this object in the <see cref="DrawNode"/>.
    /// </remarks>
    /// <typeparam name="TInput">The input vertex type.</typeparam>
    /// <typeparam name="TOutput">The output vertex type.</typeparam>
    public class VertexGroup<TInput, TOutput> : IVertexGroup
        where TInput : struct, IEquatable<TInput>, IVertex
        where TOutput : struct, IEquatable<TOutput>, IVertex
    {
        /// <summary>
        /// The <see cref="VertexBatch{T}"/> to which this group's vertices were last added.
        /// </summary>
        internal VertexBatch<TOutput>? Batch;

        /// <summary>
        /// The <see cref="DrawNode"/> invalidation ID when this group was last used.
        /// </summary>
        internal long InvalidationID;

        /// <summary>
        /// The index inside the <see cref="VertexBatch{T}"/> where this group last had its vertices placed.
        /// </summary>
        internal int StartIndex;

        /// <summary>
        /// The <see cref="DrawNode"/> draw depth when this group was last used.
        /// </summary>
        internal float DrawDepth;

        /// <summary>
        /// The draw frame when this group was last used.
        /// </summary>
        internal ulong FrameIndex;

        private readonly Func<TInput, TOutput> transformer;

        /// <summary>
        /// Creates a new <see cref="VertexGroup{TVertex}"/>.
        /// </summary>
        /// <param name="transformer">A function which transforms a given vertex to one compatible with this group.</param>
        public VertexGroup(Func<TInput, TOutput> transformer)
        {
            this.transformer = transformer;
        }

        TTransformOutput IVertexGroup.Transform<TTransformInput, TTransformOutput>(TTransformInput vertex)
        {
            Debug.Assert(typeof(TTransformInput) == typeof(TInput));
            Debug.Assert(typeof(TTransformOutput) == typeof(TOutput));

            return (TTransformOutput)(object)transformer((TInput)(object)vertex);
        }
    }
}
