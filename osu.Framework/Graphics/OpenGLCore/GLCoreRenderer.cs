// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Text;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGLCore.Batches;
using osu.Framework.Graphics.OpenGLCore.Shaders;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Statistics;
using osuTK;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGLCore
{
    internal class GLCoreRenderer : GLRenderer
    {
        private int lastBoundVertexArray;

#pragma warning disable CS0618
        protected override string GetExtensions()
        {
            GL.GetInteger(All.NumExtensions, out int numExtensions);

            var extensionsBuilder = new StringBuilder();

            for (int i = 0; i < numExtensions; i++)
                extensionsBuilder.Append($"{GL.GetString(StringNameIndexed.Extensions, i)} ");

            return extensionsBuilder.ToString().TrimEnd();
        }
#pragma warning restore CS0618

        protected internal override void BeginFrame(Vector2 windowSize)
        {
            lastBoundVertexArray = 0;
            base.BeginFrame(windowSize);
        }

        public bool BindVertexArray(int vaoId)
        {
            if (lastBoundVertexArray == vaoId)
                return false;

            lastBoundVertexArray = vaoId;
            GL.BindVertexArray(vaoId);

            FrameStatistics.Increment(StatisticsCounterType.VBufBinds);
            return true;
        }

        protected override IShaderPart CreateShaderPart(ShaderManager manager, string name, byte[]? rawData, ShaderPartType partType)
        {
            ShaderType glType;

            switch (partType)
            {
                case ShaderPartType.Fragment:
                    glType = ShaderType.FragmentShader;
                    break;

                case ShaderPartType.Vertex:
                    glType = ShaderType.VertexShader;
                    break;

                default:
                    throw new ArgumentException($"Unsupported shader part type: {partType}", nameof(partType));
            }

            return new GLCoreShaderPart(this, name, rawData, glType, manager);
        }

        protected override IVertexBatch<TVertex> CreateLinearBatch<TVertex>(int size, int maxBuffers, PrimitiveTopology topology)
            => new GLCoreLinearBatch<TVertex>(this, size, maxBuffers, GLUtils.ToPrimitiveType(topology));

        protected override IVertexBatch<TVertex> CreateQuadBatch<TVertex>(int size, int maxBuffers) => new GLCoreQuadBatch<TVertex>(this, size, maxBuffers);
    }
}
