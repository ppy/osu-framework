// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.OpenGL.Buffers;
using osu.Framework.Graphics.Rendering;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Shaders
{
    internal class GLUniformBlock
    {
        private readonly int binding;
        private int assignedBuffer;

        public GLUniformBlock(GLShader shader, int index, int binding)
        {
            this.binding = binding;
            GL.UniformBlockBinding(shader, index, binding);
        }

        public void Assign(IUniformBuffer buffer)
        {
            if (buffer is not IGLUniformBuffer glBuffer)
                throw new ArgumentException($"Provided argument must be a {typeof(GLUniformBuffer<>)}");

            if (assignedBuffer == glBuffer.Id)
                return;

            assignedBuffer = glBuffer.Id;

            Bind();
        }

        public void Bind()
        {
            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, binding, assignedBuffer);
        }
    }
}
