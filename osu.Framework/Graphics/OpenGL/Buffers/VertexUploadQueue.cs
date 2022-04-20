// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Statistics;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    /// <summary>
    /// A vertex upload queue for <see cref="VertexBuffer{T}"/>s.
    /// </summary>
    internal static class VertexUploadQueue<T>
        where T : struct, IEquatable<T>, IVertex
    {
        private static readonly DepthWrappingVertex<T>[] upload_queue = new DepthWrappingVertex<T>[1024];

        /// <summary>
        /// The index in the target vertex buffer where the vertices are to be uploaded.
        /// </summary>
        // ReSharper disable once StaticMemberInGenericType
        private static int uploadStart = int.MaxValue;

        /// <summary>
        /// The number of vertices to be uploaded.
        /// </summary>
        // ReSharper disable once StaticMemberInGenericType
        private static int uploadLength;

        /// <summary>
        /// The target vertex buffer.
        /// </summary>
        private static VertexBuffer<T> uploadTarget;

        /// <summary>
        /// Enqueues a vertex to be uploaded to the vertex buffer.
        /// </summary>
        /// <param name="buffer">The vertex buffer which the vertex is to be uploaded to.</param>
        /// <param name="index">The index in the vertex buffer to insert the vertex at.</param>
        /// <param name="vertex">The vertex to upload.</param>
        public static void Enqueue(VertexBuffer<T> buffer, int index, T vertex)
        {
            // Flush the existing queue if the buffer is filled up.
            if (uploadLength == upload_queue.Length
                // Or if two non-contiguous sets of vertices are to be uploaded.
                // This could happen if e.g. three sprites are batched together but only the outer two have changed their vertices.
                || (uploadLength > 0 && index != uploadStart + uploadLength)
                // Or if the vertices are to be uploaded to a different target.
                || buffer != uploadTarget)
            {
                Upload();
            }

            uploadTarget = buffer;
            uploadStart = uploadLength > 0 ? uploadStart : index;
            upload_queue[uploadLength] = new DepthWrappingVertex<T>
            {
                Vertex = vertex,
                BackbufferDrawDepth = GLWrapper.BackbufferDrawDepth
            };

#if DEBUG && !NO_VBO_CONSISTENCY_CHECKS
            buffer.Vertices[index] = upload_queue[uploadLength];
#endif

            uploadLength += 1;
        }

        /// <summary>
        /// Uploads the enqueued vertices to the vertex buffer.
        /// </summary>
        public static void Upload()
        {
            if (uploadLength == 0)
                return;

            uploadTarget.Bind(false);
            GL.BufferSubData(BufferTarget.ArrayBuffer, (IntPtr)(uploadStart * VertexBuffer<T>.STRIDE), (IntPtr)(uploadLength * VertexBuffer<T>.STRIDE), ref upload_queue[0]);
            uploadTarget.Unbind();

            FrameStatistics.Add(StatisticsCounterType.VerticesUpl, uploadLength);

            uploadStart = 0;
            uploadLength = 0;
        }
    }
}
