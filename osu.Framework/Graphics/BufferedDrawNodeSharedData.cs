// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Buffers;
using osuTK.Graphics.ES30;

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
        /// The version of drawn contents currently present in <see cref="MainBuffer"/> and <see cref="effectBuffers"/>.
        /// This should only be modified by <see cref="BufferedDrawNode"/>.
        /// </summary>
        internal long DrawVersion = -1;

        /// <summary>
        /// The <see cref="FrameBuffer"/> which contains the original version of the rendered <see cref="Drawable"/>.
        /// </summary>
        public FrameBuffer MainBuffer { get; }

        /// <summary>
        /// Whether the frame buffer position should be snapped to the nearest pixel when blitting.
        /// This amounts to setting the texture filtering mode to "nearest".
        /// </summary>
        public readonly bool PixelSnapping;

        /// <summary>
        /// Whether the frame buffer should be clipped to be contained in the root node.
        /// </summary>
        public readonly bool ClipToRootNode;

        /// <summary>
        /// A set of <see cref="FrameBuffer"/>s which are used in a ping-pong manner to render effects to.
        /// </summary>
        private readonly FrameBuffer[] effectBuffers;

        /// <summary>
        /// Creates a new <see cref="BufferedDrawNodeSharedData"/> with no effect buffers.
        /// </summary>
        public BufferedDrawNodeSharedData(RenderbufferInternalFormat[] formats = null, bool pixelSnapping = false, bool clipToRootNode = false)
            : this(0, formats, pixelSnapping, clipToRootNode)
        {
        }

        /// <summary>
        /// Creates a new <see cref="BufferedDrawNodeSharedData"/> with a specific amount of effect buffers.
        /// </summary>
        /// <param name="effectBufferCount">The number of effect buffers.</param>
        /// <param name="formats">The render buffer formats to attach to each frame buffer.</param>
        /// <param name="pixelSnapping">Whether the frame buffer position should be snapped to the nearest pixel when blitting.
        /// This amounts to setting the texture filtering mode to "nearest".</param>
        /// <param name="clipToRootNode">Whether the frame buffer should be clipped to be contained in the root node..</param>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="effectBufferCount"/> is less than 0.</exception>
        public BufferedDrawNodeSharedData(int effectBufferCount, RenderbufferInternalFormat[] formats = null, bool pixelSnapping = false, bool clipToRootNode = false)
        {
            if (effectBufferCount < 0)
                throw new ArgumentOutOfRangeException(nameof(effectBufferCount), "Must be positive.");

            PixelSnapping = pixelSnapping;
            All filterMode = pixelSnapping ? All.Nearest : All.Linear;

            ClipToRootNode = clipToRootNode;

            MainBuffer = new FrameBuffer(formats, filterMode);
            effectBuffers = new FrameBuffer[effectBufferCount];

            for (int i = 0; i < effectBufferCount; i++)
                effectBuffers[i] = new FrameBuffer(formats, filterMode);
        }

        private int currentEffectBuffer = -1;

        /// <summary>
        /// The <see cref="FrameBuffer"/> which contains the most up-to-date drawn effect.
        /// </summary>
        public FrameBuffer CurrentEffectBuffer => currentEffectBuffer == -1 ? MainBuffer : effectBuffers[currentEffectBuffer];

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

        /// <summary>
        /// Resets <see cref="CurrentEffectBuffer"/>.
        /// This should only be called by <see cref="BufferedDrawNode"/>.
        /// </summary>
        internal void ResetCurrentEffectBuffer() => currentEffectBuffer = -1;

        public void Dispose()
        {
            GLWrapper.ScheduleDisposal(d => d.Dispose(true), this);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            MainBuffer.Dispose();

            for (int i = 0; i < effectBuffers.Length; i++)
                effectBuffers[i].Dispose();
        }
    }
}
