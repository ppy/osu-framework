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

        // ReSharper disable once StaticMemberInGenericType
        private static int uploadStart = int.MaxValue;

        // ReSharper disable once StaticMemberInGenericType
        private static int uploadLength;

        /// <summary>
        /// Enqueues a vertex to be uploaded to the vertex buffer.
        /// </summary>
        /// <param name="buffer">The vertex buffer which the vertex is to be uploaded to.</param>
        /// <param name="index">The index in the vertex buffer to insert the vertex at.</param>
        /// <param name="vertex">The vertex to upload.</param>
        public static void Enqueue(VertexBuffer<T> buffer, int index, T vertex)
        {
            // A new upload must be started if the queue can't hold any more vertices, or if the enqueued index is disjoint from the current to-be-uploaded set.
            if (uploadLength == upload_queue.Length || (uploadLength > 0 && index > uploadStart + uploadLength))
                Upload(buffer);

            uploadStart = Math.Min(uploadStart, index);
            upload_queue[uploadLength++] = new DepthWrappingVertex<T>
            {
                Vertex = vertex,
                BackbufferDrawDepth = GLWrapper.BackbufferDrawDepth
            };

#if DEBUG && !NO_VBO_CONSISTENCY_CHECKS
            currentBuffer.Vertices[index] = UploadQueue[UploadLength - 1];
#endif
        }

        /// <summary>
        /// Uploads the enqueued vertices to the vertex buffer.
        /// </summary>
        /// <param name="buffer">The vertex buffer to upload to.</param>
        public static void Upload(VertexBuffer<T> buffer)
        {
            if (uploadLength == 0)
                return;

            buffer.Bind(false);
            GL.BufferSubData(BufferTarget.ArrayBuffer, (IntPtr)(uploadStart * VertexBuffer<T>.STRIDE), (IntPtr)(uploadLength * VertexBuffer<T>.STRIDE), ref upload_queue[0]);
            buffer.Unbind();

            FrameStatistics.Add(StatisticsCounterType.VerticesUpl, uploadLength);

            uploadStart = int.MaxValue;
            uploadLength = 0;
        }
    }
}
