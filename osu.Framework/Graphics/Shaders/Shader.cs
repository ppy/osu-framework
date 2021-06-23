// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.OpenGL;
using osuTK;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.Shaders
{
    public class Shader : IShader, IDisposable
    {
        public bool IsLoaded { get; private set; }

        internal bool IsBound;

        private readonly string name;
        private int programID = -1;

        internal readonly Dictionary<string, IUniform> Uniforms = new Dictionary<string, IUniform>();
        private readonly List<ShaderPart> parts;

        internal Shader(string name, List<ShaderPart> parts)
        {
            this.name = name;
            this.parts = parts;

            GLWrapper.EnqueueShaderCompile(this);
        }

        internal void Compile()
        {
            if (IsDisposed)
                return;

            parts.RemoveAll(p => p == null);
            Uniforms.Clear();

            if (parts.Count == 0)
                return;

            programID = CreateProgram();

            if (!CompileInternal())
                throw new ProgramLinkingFailedException(name, GetProgramLog());

            IsLoaded = true;

            SetupUniforms();

            GlobalPropertyManager.Register(this);
        }

        internal void EnsureLoaded()
        {
            if (!IsLoaded)
                Compile();
        }

        public void Bind()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not bind a disposed shader.");

            if (IsBound)
                return;

            EnsureLoaded();

            GLWrapper.UseProgram(this);

            foreach (var uniform in Uniforms.Values)
                uniform?.Update();

            IsBound = true;
        }

        public void Unbind()
        {
            if (!IsBound)
                return;

            GLWrapper.UseProgram(null);

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
            EnsureLoaded();

            return (Uniform<T>)Uniforms[name];
        }

        private protected virtual bool CompileInternal()
        {
            foreach (ShaderPart p in parts)
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

            for (int i = 0; i < uniformCount; i++)
            {
                GL.GetActiveUniform(this, i, 100, out _, out _, out ActiveUniformType type, out string uniformName);

                switch (type)
                {
                    case ActiveUniformType.Bool:
                        Uniforms.Add(uniformName, createUniform<bool>(uniformName));
                        break;

                    case ActiveUniformType.Float:
                        Uniforms.Add(uniformName, createUniform<float>(uniformName));
                        break;

                    case ActiveUniformType.Int:
                        Uniforms.Add(uniformName, createUniform<int>(uniformName));
                        break;

                    case ActiveUniformType.FloatMat3:
                        Uniforms.Add(uniformName, createUniform<Matrix3>(uniformName));
                        break;

                    case ActiveUniformType.FloatMat4:
                        Uniforms.Add(uniformName, createUniform<Matrix4>(uniformName));
                        break;

                    case ActiveUniformType.FloatVec2:
                        Uniforms.Add(uniformName, createUniform<Vector2>(uniformName));
                        break;

                    case ActiveUniformType.FloatVec3:
                        Uniforms.Add(uniformName, createUniform<Vector3>(uniformName));
                        break;

                    case ActiveUniformType.FloatVec4:
                        Uniforms.Add(uniformName, createUniform<Vector4>(uniformName));
                        break;

                    case ActiveUniformType.Sampler2D:
                        Uniforms.Add(uniformName, createUniform<int>(uniformName));
                        break;
                }
            }

            IUniform createUniform<T>(string name)
                where T : struct, IEquatable<T>
            {
                int location = GL.GetUniformLocation(this, name);

                if (GlobalPropertyManager.CheckGlobalExists(name)) return new GlobalUniform<T>(this, name, location);

                return new Uniform<T>(this, name, location);
            }
        }

        private protected virtual string GetProgramLog() => GL.GetProgramInfoLog(this);

        private protected virtual int CreateProgram() => GL.CreateProgram();

        private protected virtual void DeleteProgram(int id) => GL.DeleteProgram(id);

        public override string ToString() => $@"{name} Shader (Compiled: {programID != -1})";

        public static implicit operator int(Shader shader) => shader.programID;

        #region IDisposable Support

        protected internal bool IsDisposed { get; private set; }

        ~Shader()
        {
            GLWrapper.ScheduleDisposal(() => Dispose(false));
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

                GlobalPropertyManager.Unregister(this);

                if (programID != -1)
                    DeleteProgram(this);
            }
        }

        #endregion

        public class PartCompilationFailedException : Exception
        {
            public PartCompilationFailedException(string partName, string log)
                : base($"A {typeof(ShaderPart)} failed to compile: {partName}:\n{log.Trim()}")
            {
            }
        }

        public class ProgramLinkingFailedException : Exception
        {
            public ProgramLinkingFailedException(string programName, string log)
                : base($"A {typeof(Shader)} failed to link: {programName}:\n{log.Trim()}")
            {
            }
        }
    }
}
