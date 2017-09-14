﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
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
        internal bool Loaded;
        internal bool IsBound;

        private readonly string name;
        private int programID = -1;

        private static readonly List<Shader> all_shaders = new List<Shader>();
        private static readonly Dictionary<string, object> global_properties = new Dictionary<string, object>();

        private readonly Dictionary<string, UniformBase> uniforms = new Dictionary<string, UniformBase>();
        private UniformBase[] uniformsArray;
        private readonly List<ShaderPart> parts;

        internal Shader(string name, List<ShaderPart> parts)
        {
            this.name = name;
            this.parts = parts;
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
                all_shaders.Remove(this);
            }
        }

        #endregion

        internal void Compile()
        {
            parts.RemoveAll(p => p == null);
            uniforms.Clear();
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

            int linkResult;
            GL.GetProgram(this, GetProgramParameterName.LinkStatus, out linkResult);
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
                int uniformCount;
                GL.GetProgram(this, GetProgramParameterName.ActiveUniforms, out uniformCount);
                uniformsArray = new UniformBase[uniformCount];

                for (int i = 0; i < uniformCount; i++)
                {
                    int size;
                    int length;
                    ActiveUniformType type;
                    string uniformName;
                    GL.GetActiveUniform(this, i, 100, out length, out size, out type, out uniformName);

                    uniformsArray[i] = new UniformBase(this, uniformName, GL.GetUniformLocation(this, uniformName), type);
                    uniforms.Add(uniformName, uniformsArray[i]);
                }

                foreach (KeyValuePair<string, object> kvp in global_properties)
                {
                    if (!uniforms.ContainsKey(kvp.Key))
                        continue;
                    uniforms[kvp.Key].Value = kvp.Value;
                }

                all_shaders.Add(this);
            }
        }

        private void ensureLoaded()
        {
            if (!Loaded)
                Compile();
        }

        public void Bind()
        {
            if (IsBound)
                return;

            ensureLoaded();

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
            ensureLoaded();
            if (!uniforms.ContainsKey(name))
                throw new ArgumentException($"Uniform {name} does not exist in shader {this.name}.", nameof(name));
            return new Uniform<T>(uniforms[name]);
        }

        /// <summary>
        /// Sets a uniform for all shaders that contain this property.
        /// <para>Any future-initialized shaders will also have this uniform set.</para>
        /// </summary>
        /// <param name="name">The uniform name.</param>
        /// <param name="value">The uniform value.</param>
        public static void SetGlobalProperty(string name, object value)
        {
            global_properties[name] = value;

            foreach (Shader shader in all_shaders)
            {
                shader.ensureLoaded();
                if (shader.uniforms.ContainsKey(name))
                    shader.uniforms[name].Value = value;
            }
        }

        public static implicit operator int(Shader shader)
        {
            return shader.programID;
        }
    }
}
