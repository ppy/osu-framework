// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using osu.Framework.Extensions.TypeExtensions;
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
        private readonly int blockBinding;
        private readonly int blockSize;

        private int ubo = -1;
        private object? data;
        private byte[]? dataBuffer;

        public GLUniformBlock(GLRenderer renderer, GLShader shader, string uniformName, int blockIndex, int blockBinding, int blockSize)
        {
            this.renderer = renderer;
            this.shader = shader;
            this.uniformName = uniformName;
            this.blockIndex = blockIndex;
            this.blockBinding = blockBinding;
            this.blockSize = blockSize;
        }

        public void SetValue<T>(T value)
            where T : unmanaged, IEquatable<T>
        {
            if (Marshal.SizeOf(value) != blockSize)
                throw new ArgumentException($"Managed object \"{typeof(T).ReadableName()}\" does not match the size of uniform block \"{uniformName}\" in {shader}.");

            if (data is T tData && EqualityComparer<T>.Default.Equals(value, tData))
                return;

            data = value;
            dataBuffer = ArrayPool<byte>.Shared.Rent(blockSize);
            MemoryMarshal.Write(dataBuffer, ref value);
        }

        public void Bind()
        {
            if (dataBuffer == null)
                return;

            if (ubo == -1)
                GL.GenBuffers(1, out ubo);

            GL.BindBuffer(BufferTarget.UniformBuffer, ubo);
            GL.BufferData(BufferTarget.UniformBuffer, blockSize, ref dataBuffer[0], BufferUsageHint.DynamicDraw);
            GL.BindBuffer(BufferTarget.UniformBuffer, 0);

            GL.UniformBlockBinding(shader, blockIndex, blockBinding);
            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, blockBinding, ubo);

            returnBuffer();
        }

        private void returnBuffer()
        {
            if (dataBuffer == null)
                return;

            ArrayPool<byte>.Shared.Return(dataBuffer);
            dataBuffer = null;
        }
    }
}
