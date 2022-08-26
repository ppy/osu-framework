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
        private readonly IRenderer renderer;
        private readonly string name;
        private readonly GLShaderPart[] parts;

        private readonly ScheduledDelegate shaderCompileDelegate;

        internal readonly Dictionary<string, IUniform> Uniforms = new Dictionary<string, IUniform>();

        /// <summary>
        /// Holds all the <see cref="Uniforms"/> values for faster access than iterating on <see cref="Dictionary{TKey,TValue}.Values"/>.
        /// </summary>
        private IUniform[] uniformsValues;

        public bool IsLoaded { get; private set; }

        public bool IsBound { get; private set; }

        private int programID = -1;

        internal GLShader(IRenderer renderer, string name, GLShaderPart[] parts)
        {
            this.renderer = renderer;
            this.name = name;
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

            GlobalPropertyManager.Register(this);
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

            renderer.UseProgram(this);

            foreach (var uniform in uniformsValues)
                uniform?.Update();

            IsBound = true;
        }

        public void Unbind()
        {
            if (!IsBound)
                return;

            renderer.UseProgram(null);

            IsBound = false;
        }

        /// <summary>
        /// Returns a uniform from the shader.
        /// </summary>
        /// <param name="name">The name of the uniform.</param>
        /// <returns>Returns a base uniform.</returns>
        public Uniform<T> GetUniform<T>(string name)
            where T : struct, IEquatable<T>
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not retrieve uniforms from a disposed shader.");

            EnsureShaderCompiled();

            return (Uniform<T>)Uniforms[name];
        }

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

            uniformsValues = new IUniform[uniformCount];

            for (int i = 0; i < uniformCount; i++)
            {
                GL.GetActiveUniform(this, i, 100, out _, out _, out ActiveUniformType type, out string uniformName);

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
                        break;

                    default:
                        continue;
                }

                Uniforms.Add(uniformName, uniform);
                uniformsValues[i] = uniform;
            }

            IUniform createUniform<T>(string name)
                where T : struct, IEquatable<T>
            {
                int location = GL.GetUniformLocation(this, name);

                if (GlobalPropertyManager.CheckGlobalExists(name)) return new GlobalUniform<T>(renderer, this, name, location);

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

                GlobalPropertyManager.Unregister(this);

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
