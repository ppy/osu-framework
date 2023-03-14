// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.OpenGL.Buffers;
using osu.Framework.Graphics.Rendering;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Shaders
{
    /// <summary>
    /// A uniform block of an OpenGL shader.
    /// </summary>
    internal class GLUniformBlock
    {
        private readonly GLRenderer renderer;
        private readonly int binding;
        private int assignedBuffer = -1;

        /// <summary>
        /// Creates a new uniform block.
        /// </summary>
        /// <param name="renderer">The renderer.</param>
        /// <param name="shader">The shader.</param>
        /// <param name="index">The index (location) of this block in the GL shader.</param>
        /// <param name="binding">A unique index for this block to bound to in the GL program.</param>
        public GLUniformBlock(GLRenderer renderer, GLShader shader, int index, int binding)
        {
            this.renderer = renderer;
            this.binding = binding;

            // This creates a mapping in the shader program that binds the block at location `index` to location `binding` in the GL pipeline.
            GL.UniformBlockBinding(shader, index, binding);
        }

        /// <summary>
        /// Assigns an <see cref="IUniformBuffer{TData}"/> to this uniform block.
        /// </summary>
        /// <param name="buffer">The buffer to assign.</param>
        /// <exception cref="ArgumentException">If the provided buffer is not a <see cref="GLUniformBuffer{TData}"/>.</exception>
        public void Assign(IUniformBuffer buffer)
        {
            if (buffer is not IGLUniformBuffer glBuffer)
                throw new ArgumentException($"Provided argument must be a {typeof(GLUniformBuffer<>)}");

            if (assignedBuffer == glBuffer.Id)
                return;

            assignedBuffer = glBuffer.Id;

            // If the shader was bound prior to this buffer being assigned, then the buffer needs to be bound immediately.
            Bind();
        }

        public void Bind()
        {
            if (assignedBuffer == -1)
                return;

            renderer.FlushCurrentBatch(FlushBatchSource.BindBuffer);

            // Bind the assigned buffer to the correct slot in the GL pipeline.
            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, binding, assignedBuffer);
        }
    }
}
