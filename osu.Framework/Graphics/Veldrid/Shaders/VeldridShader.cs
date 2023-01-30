// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Logging;
using osu.Framework.Threading;
using Veldrid;
using Veldrid.SPIRV;
using static osu.Framework.Threading.ScheduledDelegate;

namespace osu.Framework.Graphics.Veldrid.Shaders
{
    internal class VeldridShader : IShader
    {
        private readonly string name;
        private readonly VeldridShaderPart[] parts;
        private readonly IUniformBuffer<GlobalUniformData> globalUniformBuffer;
        private readonly VeldridRenderer renderer;

        public Shader[]? Shaders;

        private readonly ScheduledDelegate shaderInitialiseDelegate;

        public bool IsLoaded => Shaders != null;

        public bool IsBound { get; private set; }

        IReadOnlyDictionary<string, IUniform> IShader.Uniforms => throw new NotSupportedException();

        private readonly Dictionary<string, VeldridUniformBlock> uniformBlocks = new Dictionary<string, VeldridUniformBlock>();

        public VeldridShader(VeldridRenderer renderer, string name, VeldridShaderPart[] parts, IUniformBuffer<GlobalUniformData> globalUniformBuffer)
        {
            this.name = name;
            this.parts = parts;
            this.globalUniformBuffer = globalUniformBuffer;
            this.renderer = renderer;

            renderer.ScheduleExpensiveOperation(shaderInitialiseDelegate = new ScheduledDelegate(initialise));
        }

        internal void EnsureShaderInitialised()
        {
            if (isDisposed)
                throw new ObjectDisposedException(ToString(), "Can not compile a disposed shader.");

            if (shaderInitialiseDelegate.State == RunState.Waiting)
                shaderInitialiseDelegate.RunTask();
        }

        public void Bind()
        {
            if (IsBound)
                return;

            EnsureShaderInitialised();

            renderer.BindShader(this);

            foreach (var block in uniformBlocks.Values)
                renderer.SetResource(block);

            IsBound = true;
        }

        public void Unbind()
        {
            if (!IsBound)
                return;

            renderer.UnbindShader(this);
            IsBound = false;
        }

        public Uniform<T> GetUniform<T>(string name) where T : unmanaged, IEquatable<T> => throw new NotSupportedException();

        public void AssignUniformBlock(string blockName, IUniformBuffer buffer) => uniformBlocks[blockName].Assign(buffer);

        private void initialise()
        {
            Debug.Assert(parts.Length == 2);

            VeldridShaderPart vertex = parts.Single(p => p.Type == ShaderPartType.Vertex);
            VeldridShaderPart fragment = parts.Single(p => p.Type == ShaderPartType.Fragment);

            try
            {
                VertexFragmentCompilationResult compilationResult = SpirvCompilation.CompileVertexFragment(
                    vertex.GetData(),
                    fragment.GetData(),
                    CrossCompileTarget.GLSL,
                    new CrossCompileOptions
                    {
                        FixClipSpaceZ = !renderer.Device.IsDepthRangeZeroToOne,
                        InvertVertexOutputY = renderer.Device.IsClipSpaceYInverted,
                    });

                Shader vertexShader = renderer.Factory.CreateShader(new ShaderDescription(
                    ShaderStages.Vertex,
                    Encoding.UTF8.GetBytes(compilationResult.VertexShader),
                    "main"));

                Shader fragmentShader = renderer.Factory.CreateShader(new ShaderDescription(
                    ShaderStages.Vertex,
                    Encoding.UTF8.GetBytes(compilationResult.FragmentShader),
                    "main"));

                Shaders = new[] { vertexShader, fragmentShader };

                for (int set = 0; set < compilationResult.Reflection.ResourceLayouts.Length; set++)
                {
                    ResourceLayoutDescription layout = compilationResult.Reflection.ResourceLayouts[set];

                    if (layout.Elements.Length == 0)
                        continue;

                    foreach (ResourceLayoutElementDescription element in layout.Elements)
                    {
                        if (element.Kind != ResourceKind.UniformBuffer)
                            continue;

                        uniformBlocks[element.Name] = new VeldridUniformBlock(renderer, set, element.Name);
                    }
                }

                AssignUniformBlock("g_GlobalUniforms", globalUniformBuffer);
            }
            catch (SpirvCompilationException e)
            {
                Logger.Error(e, $"Failed to initialise shader \"{name}\"");
            }
        }

        private bool isDisposed;

        ~VeldridShader()
        {
            renderer.ScheduleDisposal(s => s.Dispose(false), this);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (isDisposed)
                return;

            isDisposed = true;

            if (Shaders != null)
            {
                for (int i = 0; i < Shaders.Length; i++)
                    Shaders[i].Dispose();

                Shaders = null;
            }
        }
    }
}
