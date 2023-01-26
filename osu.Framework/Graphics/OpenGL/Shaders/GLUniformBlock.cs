// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.Shaders;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Shaders
{
    // TODO: Disposal
    internal class GLUniformBlock : IUniformBlock
    {
        private readonly GLRenderer renderer;
        private readonly GLShader shader;
        private readonly string uniformName;
        private readonly int blockIndex;
        private readonly int ubo;
        private byte[]? data;

        public GLUniformBlock(GLRenderer renderer, GLShader shader, string uniformName, int blockIndex)
        {
            this.renderer = renderer;
            this.shader = shader;
            this.uniformName = uniformName;
            this.blockIndex = blockIndex;

            GL.GenBuffers(1, out ubo);
        }

        public void SetValue<T>(T value)
            where T : unmanaged, IEquatable<T>
        {
            data = new byte[Marshal.SizeOf(value)];
            MemoryMarshal.Write(data, ref value);
        }

        public void Bind()
        {
            if (data == null)
                return;

            GL.BindBuffer(BufferTarget.UniformBuffer, ubo);
            GL.BufferData(BufferTarget.UniformBuffer, data.Length, ref data[0], BufferUsageHint.StreamDraw);
            GL.BindBuffer(BufferTarget.UniformBuffer, 0);

            GL.UniformBlockBinding(shader, blockIndex, 0);
            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 0, ubo);
        }
    }
}
