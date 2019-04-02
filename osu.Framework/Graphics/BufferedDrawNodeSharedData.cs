// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.OpenGL.Buffers;

namespace osu.Framework.Graphics
{
    /// <summary>
    /// Contains data which is shared between all <see cref="BufferedDrawNode"/>s of a <see cref="Drawable"/>.
    /// </summary>
    /// <remarks>
    /// This should be constructed _once_ per <see cref="Drawable"/>, and given to the constructor of <see cref="BufferedDrawNode"/>.
    /// </remarks>
    public class BufferedDrawNodeSharedData : IDisposable
    {
        /// <summary>
        /// The version of drawn contents currently present in <see cref="mainBuffer"/> and <see cref="effectBuffers"/>.
        /// This should only be modified by <see cref="BufferedDrawNode"/>.
        /// </summary>
        internal long DrawVersion = -1;

        /// <summary>
        /// A <see cref="FrameBuffer"/> which contains the original version of the rendered <see cref="Drawable"/>.
        /// </summary>
        private readonly FrameBuffer mainBuffer;

        /// <summary>
        /// A set of <see cref="FrameBuffer"/>s which are used in a ping-pong manner to render effects to.
        /// </summary>
        private readonly FrameBuffer[] effectBuffers;

        /// <summary>
        /// Creates a new <see cref="BufferedDrawNodeSharedData"/> with no effect buffers.
        /// </summary>
        public BufferedDrawNodeSharedData()
            : this(0)
        {
        }

        /// <summary>
        /// Creates a new <see cref="BufferedDrawNodeSharedData"/> with a specific amount of effect buffers.
        /// </summary>
        /// <param name="effectBufferCount">The number of effect buffers.</param>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="effectBufferCount"/> is less than 0.</exception>
        public BufferedDrawNodeSharedData(int effectBufferCount)
        {
            if (effectBufferCount < 0)
                throw new ArgumentOutOfRangeException(nameof(effectBufferCount), "Must be positive.");

            mainBuffer = new FrameBuffer();
            effectBuffers = new FrameBuffer[effectBufferCount];

            for (int i = 0; i < effectBufferCount; i++)
                effectBuffers[i] = new FrameBuffer();
        }

        /// <summary>
        /// Retrieves the <see cref="FrameBuffer"/> which contains the original version of the rendered <see cref="Drawable"/>.
        /// </summary>
        public FrameBuffer GetMainBuffer() => mainBuffer;

        private int currentEffectBuffer = -1;

        /// <summary>
        /// Retrieves the next <see cref="FrameBuffer"/> which effects can be rendered to.
        /// </summary>
        /// <exception cref="InvalidOperationException">If there are no available effect buffers.</exception>
        public FrameBuffer GetNextEffectBuffer()
        {
            if (effectBuffers.Length == 0)
                throw new InvalidOperationException($"The {nameof(BufferedDrawNode)} requested an effect buffer, but none were available.");

            if (++currentEffectBuffer >= effectBuffers.Length)
                currentEffectBuffer = 0;
            return effectBuffers[currentEffectBuffer];
        }

        ~BufferedDrawNodeSharedData()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            mainBuffer.Dispose();

            for (int i = 0; i < effectBuffers.Length; i++)
                effectBuffers[i].Dispose();
        }
    }
}
