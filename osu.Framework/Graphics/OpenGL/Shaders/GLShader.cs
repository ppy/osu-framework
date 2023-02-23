// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Threading;
using osuTK;
using osuTK.Graphics.ES30;
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

        /// <summary>
        /// Holds all the <see cref="Uniforms"/> values for faster access than iterating on <see cref="Dictionary{TKey,TValue}.Values"/>.
        /// </summary>
        private List<IUniform> uniformsValues;

        /// <summary>
        /// Holds all <see cref="uniformBlocks"/> values for faster access than iterating on <see cref="Dictionary{TKey,TValue}.Values"/>.
        /// </summary>
        private List<GLUniformBlock> uniformBlocksValues;

        public bool IsLoaded { get; private set; }

        public bool IsBound { get; private set; }

        private int programID = -1;

        internal GLShader(GLRenderer renderer, string name, GLShaderPart[] parts, IUniformBuffer<GlobalUniformData> globalUniformBuffer)
        {
            this.renderer = renderer;
            this.name = name;
            this.globalUniformBuffer = globalUniformBuffer;
            this.parts = parts.Where(p => p != null).ToArray();

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

            SetupUniforms();
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

            foreach (var uniform in uniformsValues)
                uniform?.Update();

            foreach (var block in uniformBlocksValues)
                block?.Bind();

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

        public void BindUniformBlock(string blockName, IUniformBuffer buffer) => uniformBlocks[blockName].Assign(buffer);

        private protected virtual bool CompileInternal()
        {
            foreach (GLShaderPart p in parts)
            {
                if (!p.Compiled) p.Compile();
                GL.AttachShader(this, p);

                foreach (ShaderInputInfo input in p.ShaderInputs)
                    GL.BindAttribLocation(this, input.Location, input.Name);
            }

            GL.LinkProgram(this);
            GL.GetProgram(this, GetProgramParameterName.LinkStatus, out int linkResult);

            foreach (var part in parts)
                GL.DetachShader(this, part);

            return linkResult == 1;
        }

        private protected virtual void SetupUniforms()
        {
            GL.GetProgram(this, GetProgramParameterName.ActiveUniforms, out int uniformCount);

            uniformsValues = new List<IUniform>(uniformCount);
            uniformBlocksValues = new List<GLUniformBlock>(uniformCount);

            int[] uniformIndices = Enumerable.Range(0, uniformCount).ToArray();
            int[] blockIndices = new int[uniformCount];
            GL.GetActiveUniforms(this, uniformCount, uniformIndices, ActiveUniformParameter.UniformBlockIndex, blockIndices);

            int blockBindingIndex = 0;
            int textureIndex = 0;

            for (int i = 0; i < uniformCount; i++)
            {
                int blockIndex = blockIndices[i];
                string uniformName;

                // Block index of -1 indicates a uniform that isn't part of a block and is instead a free-floating uniform.
                if (blockIndex >= 0)
                {
                    GL.GetActiveUniformBlockName(this, blockIndex, 100, out _, out uniformName);

                    // The block may have been seen before since we're iterating over all uniform members in the composite.
                    if (uniformBlocks.ContainsKey(uniformName))
                        continue;

                    var block = new GLUniformBlock(renderer, this, blockIndex, blockBindingIndex++);
                    uniformBlocks[uniformName] = block;
                    uniformBlocksValues.Add(block);
                }
                else
                {
                    GL.GetActiveUniform(this, i, 100, out _, out _, out ActiveUniformType type, out uniformName);

                    IUniform uniform;

                    switch (type)
                    {
                        case ActiveUniformType.Bool:
                            uniform = createUniform<bool>(uniformName);
                            break;

                        case ActiveUniformType.Float:
                            uniform = createUniform<float>(uniformName);
                            break;

                        case ActiveUniformType.Int:
                            uniform = createUniform<int>(uniformName);
                            break;

                        case ActiveUniformType.FloatMat3:
                            uniform = createUniform<Matrix3>(uniformName);
                            break;

                        case ActiveUniformType.FloatMat4:
                            uniform = createUniform<Matrix4>(uniformName);
                            break;

                        case ActiveUniformType.FloatVec2:
                            uniform = createUniform<Vector2>(uniformName);
                            break;

                        case ActiveUniformType.FloatVec3:
                            uniform = createUniform<Vector3>(uniformName);
                            break;

                        case ActiveUniformType.FloatVec4:
                            uniform = createUniform<Vector4>(uniformName);
                            break;

                        case ActiveUniformType.Sampler2D:
                            uniform = createUniform<int>(uniformName);
                            ((Uniform<int>)uniform).Value = textureIndex++;
                            break;

                        default:
                            continue;
                    }

                    Uniforms[uniformName] = uniform;
                    uniformsValues.Add(uniform);
                }
            }

            BindUniformBlock("g_GlobalUniforms", globalUniformBuffer);

            IUniform createUniform<T>(string name)
                where T : unmanaged, IEquatable<T>
            {
                int location = GL.GetUniformLocation(this, name);
                return new Uniform<T>(renderer, this, name, location);
            }
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
            if (!IsDisposed)
            {
                IsDisposed = true;

                shaderCompileDelegate?.Cancel();

                if (programID != -1)
                    DeleteProgram(this);
            }
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
