// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        private readonly VeldridRenderer renderer;

        private Shader[]? shaders;

        private Dictionary<string, IUniform>? uniforms;
        private DeviceBuffer? uniformBuffer;
        private ResourceSet? uniformResourceSet;

        private readonly ScheduledDelegate shaderInitialiseDelegate;

        public bool IsLoaded => shaders != null;

        public bool IsBound { get; private set; }

        /// <summary>
        /// The underlying Veldrid shaders.
        /// </summary>
        public Shader[] Shaders
        {
            get
            {
                if (!IsLoaded)
                    throw new InvalidOperationException("Can not obtain shader parts for an uninitialised shader.");

                Debug.Assert(shaders != null);
                return shaders;
            }
        }

        IReadOnlyDictionary<string, IUniform> IShader.Uniforms
        {
            get
            {
                if (!IsLoaded)
                    throw new InvalidOperationException("Can not obtain uniforms for an uninitialised shader.");

                Debug.Assert(uniforms != null);
                return uniforms;
            }
        }

        /// <summary>
        /// The uniform buffer object for this shader.
        /// </summary>
        public DeviceBuffer UniformBuffer
        {
            get
            {
                if (!IsLoaded)
                    throw new InvalidOperationException("Can not obtain uniform buffer for an uninitialised shader.");

                Debug.Assert(uniformBuffer != null);
                return uniformBuffer;
            }
        }

        /// <summary>
        /// A resource set wrapping the uniform buffer object for binding.
        /// </summary>
        public ResourceSet UniformResourceSet
        {
            get
            {
                if (!IsLoaded)
                    throw new InvalidOperationException("Can not obtain uniform resource set for an uninitialised shader.");

                Debug.Assert(uniformResourceSet != null);
                return uniformResourceSet;
            }
        }

        public VeldridShader(VeldridRenderer renderer, string name, params VeldridShaderPart[] parts)
        {
            this.name = name;
            this.parts = parts;
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

            Debug.Assert(uniforms != null);

            foreach (var uniform in uniforms)
                uniform.Value.Update();

            IsBound = true;
        }

        public void Unbind()
        {
            if (!IsBound)
                return;

            renderer.UnbindShader(this);
            IsBound = false;
        }

        public Uniform<T> GetUniform<T>(string name)
            where T : unmanaged, IEquatable<T>
        {
            if (isDisposed)
                throw new ObjectDisposedException("Can not retrieve uniforms from a disposed shader.");

            EnsureShaderInitialised();

            Debug.Assert(uniforms != null);
            return (Uniform<T>)uniforms[name];
        }

        private void initialise()
        {
            Debug.Assert(parts.Length == 2);

            VeldridShaderPart vertex = parts.Single(p => p.Type == ShaderPartType.Vertex);
            VeldridShaderPart fragment = parts.Single(p => p.Type == ShaderPartType.Fragment);

            var allUniforms = new VeldridUniformGroup(vertex.Uniforms, fragment.Uniforms);
            uniformBuffer = allUniforms.CreateBuffer(renderer, this, out uniforms);
            uniformResourceSet = renderer.CreateUniformResourceSet(uniformBuffer);

            // todo: maybe use better entry point?
            var vertexDescription = new ShaderDescription(ShaderStages.Vertex, vertex.GetData(allUniforms), "main");
            var fragmentDescription = new ShaderDescription(ShaderStages.Fragment, fragment.GetData(allUniforms), "main");

            try
            {
                shaders = renderer.Factory.CreateFromSpirv(vertexDescription, fragmentDescription, new CrossCompileOptions
                {
                    FixClipSpaceZ = !renderer.Device.IsDepthRangeZeroToOne,
                    InvertVertexOutputY = renderer.Device.IsClipSpaceYInverted,
                });

                GlobalPropertyManager.Register(this);
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

            GlobalPropertyManager.Unregister(this);

            if (shaders != null)
            {
                for (int i = 0; i < shaders.Length; i++)
                    shaders[i].Dispose();

                shaders = null;
            }
        }
    }
}
