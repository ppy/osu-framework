// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Text;
using osu.Framework.Graphics.OpenGL;
using OpenTK.Graphics.ES30;

namespace osu.Framework.Graphics.Shaders
{
    public class Shader : IDisposable
    {
        internal StringBuilder Log = new StringBuilder();

        /// <summary>
        /// Whether this shader has been loaded and compiled.
        /// </summary>
        public bool Loaded { get; private set; }

        internal bool IsBound;

        private readonly string name;
        private int programID = -1;

        internal readonly Dictionary<string, UniformBase> Uniforms = new Dictionary<string, UniformBase>();
        private UniformBase[] uniformsArray;
        private readonly List<ShaderPart> parts;

        internal Shader(string name, List<ShaderPart> parts)
        {
            this.name = name;
            this.parts = parts;

            GLWrapper.EnqueueShaderCompile(this);
        }

        #region Disposal

        ~Shader()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Loaded)
            {
                Unbind();

                GLWrapper.DeleteProgram(this);
                Loaded = false;
                programID = -1;
                GlobalPropertyManager.Remove(this);
            }
        }

        #endregion

        internal void Compile()
        {
            parts.RemoveAll(p => p == null);
            Uniforms.Clear();
            uniformsArray = null;
            Log.Clear();

            if (programID != -1)
                Dispose(true);

            if (parts.Count == 0)
                return;

            programID = GL.CreateProgram();
            foreach (ShaderPart p in parts)
            {
                if (!p.Compiled) p.Compile();
                GL.AttachShader(this, p);

                foreach (AttributeInfo attribute in p.Attributes)
                    GL.BindAttribLocation(this, attribute.Location, attribute.Name);
            }

            GL.LinkProgram(this);

            GL.GetProgram(this, GetProgramParameterName.LinkStatus, out int linkResult);
            string linkLog = GL.GetProgramInfoLog(this);

            Log.AppendLine(string.Format(ShaderPart.BOUNDARY, name));
            Log.AppendLine($"Linked: {linkResult == 1}");
            if (linkResult == 0)
            {
                Log.AppendLine("Log:");
                Log.AppendLine(linkLog);
            }

            foreach (var part in parts)
                GL.DetachShader(this, part);

            Loaded = linkResult == 1;

            if (Loaded)
            {
                //Obtain all the shader uniforms
                GL.GetProgram(this, GetProgramParameterName.ActiveUniforms, out int uniformCount);
                uniformsArray = new UniformBase[uniformCount];

                for (int i = 0; i < uniformCount; i++)
                {
                    GL.GetActiveUniform(this, i, 100, out _, out _, out ActiveUniformType type, out string uniformName);

                    uniformsArray[i] = new UniformBase(this, uniformName, GL.GetUniformLocation(this, uniformName), type);
                    Uniforms.Add(uniformName, uniformsArray[i]);
                }

                GlobalPropertyManager.InitializeShader(this);
            }
        }

        internal void EnsureLoaded()
        {
            if (!Loaded)
                Compile();
        }

        public void Bind()
        {
            if (IsBound)
                return;

            EnsureLoaded();

            GLWrapper.UseProgram(this);

            foreach (var uniform in uniformsArray)
                if (uniform.HasChanged)
                    uniform.Update();

            IsBound = true;
        }

        public void Unbind()
        {
            if (!IsBound)
                return;

            GLWrapper.UseProgram(null);

            IsBound = false;
        }

        public override string ToString() => $@"{name} Shader (Compiled: {programID != -1})";

        /// <summary>
        /// Returns a uniform from the shader.
        /// </summary>
        /// <param name="name">The name of the uniform.</param>
        /// <returns>Returns a base uniform.</returns>
        public Uniform<T> GetUniform<T>(string name)
        {
            EnsureLoaded();
            if (!Uniforms.ContainsKey(name))
                throw new ArgumentException($"Uniform {name} does not exist in shader {this.name}.", nameof(name));
            return new Uniform<T>(Uniforms[name]);
        }

        public static implicit operator int(Shader shader)
        {
            return shader.programID;
        }
    }
}
