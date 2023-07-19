// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics.OpenGL.Buffers;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Threading;
using osuTK.Graphics.ES30;
using Veldrid;
using Veldrid.SPIRV;
using static osu.Framework.Threading.ScheduledDelegate;

namespace osu.Framework.Graphics.OpenGL.Shaders
{
    internal class GLShader : IShader
    {
        private readonly GLRenderer renderer;
        private readonly string name;
        private readonly IUniformBuffer<GlobalUniformData> globalUniformBuffer;
        private readonly GLShaderPart[] parts;

        private readonly ScheduledDelegate shaderCompileDelegate;

        internal readonly Dictionary<string, IUniform> Uniforms = new Dictionary<string, IUniform>();

        IReadOnlyDictionary<string, IUniform> IShader.Uniforms => Uniforms;

        private readonly Dictionary<string, GLUniformBlock> uniformBlocks = new Dictionary<string, GLUniformBlock>();
        private readonly List<Uniform<int>> textureUniforms = new List<Uniform<int>>();

        public bool IsLoaded { get; private set; }

        public bool IsBound { get; private set; }

        private int programID = -1;

        private readonly GLShaderPart vertexPart;
        private readonly GLShaderPart fragmentPart;
        private readonly VertexFragmentShaderCompilation compilation;

        internal GLShader(GLRenderer renderer, string name, GLShaderPart[] parts, IUniformBuffer<GlobalUniformData> globalUniformBuffer, ShaderCompilationStore compilationStore)
        {
            this.renderer = renderer;
            this.name = name;
            this.globalUniformBuffer = globalUniformBuffer;
            this.parts = parts;

            vertexPart = parts.Single(p => p.Type == ShaderType.VertexShader);
            fragmentPart = parts.Single(p => p.Type == ShaderType.FragmentShader);

            // This part of the compilation is quite CPU expensive.
            // Running it in the constructor will ensure that BDL usages can correctly offload this as an async operation.
            try
            {
                // Shaders are in "Vulkan GLSL" format. They need to be cross-compiled to GLSL.
                compilation = compilationStore.CompileVertexFragment(
                    vertexPart.GetRawText(),
                    fragmentPart.GetRawText(),
                    renderer.IsEmbedded ? CrossCompileTarget.ESSL : CrossCompileTarget.GLSL);
            }
            catch (Exception e)
            {
                throw new ProgramLinkingFailedException(name, e.ToString());
            }

            // Final GPU level compilation needs to be run on the draw thread.
            renderer.ScheduleExpensiveOperation(shaderCompileDelegate = new ScheduledDelegate(compile));
        }

        private void compile()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not compile a disposed shader.");

            if (IsLoaded)
                throw new InvalidOperationException("Attempting to compile an already-compiled shader.");

            if (parts.Length == 0)
                return;

            programID = CreateProgram();

            if (!CompileInternal())
                throw new ProgramLinkingFailedException(name, GetProgramLog());

            IsLoaded = true;

            BindUniformBlock("g_GlobalUniforms", globalUniformBuffer);
        }

        internal void EnsureShaderCompiled()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not compile a disposed shader.");

            if (shaderCompileDelegate.State == RunState.Waiting)
                shaderCompileDelegate.RunTask();
        }

        public void Bind()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not bind a disposed shader.");

            if (IsBound)
                return;

            EnsureShaderCompiled();

            renderer.BindShader(this);

            for (int i = 0; i < textureUniforms.Count; i++)
                textureUniforms[i].Update();

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
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not retrieve uniforms from a disposed shader.");

            EnsureShaderCompiled();

            return (Uniform<T>)Uniforms[name];
        }

        public virtual void BindUniformBlock(string blockName, IUniformBuffer buffer)
        {
            if (buffer is not IGLUniformBuffer glBuffer)
                throw new ArgumentException($"Buffer must be an {nameof(IGLUniformBuffer)}.");

            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not retrieve uniforms from a disposed shader.");

            EnsureShaderCompiled();

            renderer.FlushCurrentBatch(FlushBatchSource.BindBuffer);
            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, uniformBlocks[blockName].Binding, glBuffer.Id);
        }

        private protected virtual bool CompileInternal()
        {
            vertexPart.Compile(compilation.VertexText);
            fragmentPart.Compile(compilation.FragmentText);

            foreach (GLShaderPart p in parts)
                GL.AttachShader(this, p);

            GL.LinkProgram(this);
            GL.GetProgram(this, GetProgramParameterName.LinkStatus, out int linkResult);

            foreach (var part in parts)
                GL.DetachShader(this, part);

            if (linkResult != 1)
                return false;

            int blockBindingIndex = 0;
            int textureIndex = 0;

            foreach (ResourceLayoutDescription layout in compilation.Reflection.ResourceLayouts)
            {
                if (layout.Elements.Length == 0)
                    continue;

                if (layout.Elements.Any(e => e.Kind == ResourceKind.TextureReadOnly || e.Kind == ResourceKind.TextureReadWrite))
                {
                    ResourceLayoutElementDescription textureElement = layout.Elements.First(e => e.Kind == ResourceKind.TextureReadOnly || e.Kind == ResourceKind.TextureReadWrite);

                    if (layout.Elements.All(e => e.Kind != ResourceKind.Sampler))
                        throw new ProgramLinkingFailedException(name, $"Texture {textureElement.Name} has no associated sampler.");

                    textureUniforms.Add(new Uniform<int>(renderer, this, textureElement.Name, GL.GetUniformLocation(this, textureElement.Name))
                    {
                        Value = textureIndex++
                    });
                }
                else if (layout.Elements[0].Kind == ResourceKind.UniformBuffer)
                {
                    var block = new GLUniformBlock(this, GL.GetUniformBlockIndex(this, layout.Elements[0].Name), blockBindingIndex++);
                    uniformBlocks[layout.Elements[0].Name] = block;
                }
            }

            return true;
        }

        private protected virtual string GetProgramLog() => GL.GetProgramInfoLog(this);

        private protected virtual int CreateProgram() => GL.CreateProgram();

        private protected virtual void DeleteProgram(int id) => GL.DeleteProgram(id);

        public override string ToString() => $@"{name} Shader (Compiled: {programID != -1})";

        public static implicit operator int(GLShader shader) => shader.programID;

        #region IDisposable Support

        protected internal bool IsDisposed { get; private set; }

        ~GLShader()
        {
            renderer.ScheduleDisposal(s => s.Dispose(false), this);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            IsDisposed = true;

            if (shaderCompileDelegate.IsNotNull())
                shaderCompileDelegate.Cancel();

            if (programID != -1)
                DeleteProgram(this);
        }

        #endregion

        public class PartCompilationFailedException : Exception
        {
            public PartCompilationFailedException(string partName, string log)
                : base($"A {typeof(GLShaderPart)} failed to compile: {partName}:\n{log.Trim()}")
            {
            }
        }

        public class ProgramLinkingFailedException : Exception
        {
            public ProgramLinkingFailedException(string programName, string log)
                : base($"A {typeof(GLShader)} failed to link: {programName}:\n{log.Trim()}")
            {
            }
        }
    }
}
