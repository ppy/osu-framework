// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Text;
using osu.Framework.Graphics.OpenGL;
using OpenTK.Graphics.ES30;
using System.Diagnostics;

namespace osu.Framework.Graphics.Shaders
{
    public class Shader : IDisposable
    {
        internal StringBuilder Log = new StringBuilder();
        internal bool Loaded;
        internal bool IsBound;

        private string name;
        private int programID = -1;

        private static List<Shader> allShaders = new List<Shader>();
        private static Dictionary<string, object> globalProperties = new Dictionary<string, object>();

        private Dictionary<string, UniformBase> uniforms = new Dictionary<string, UniformBase>();
        private UniformBase[] uniformsArray;
        private List<ShaderPart> parts;

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
                allShaders.Remove(this);
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
            }

            GL.BindAttribLocation(this, 0, "m_Position");
            GL.BindAttribLocation(this, 1, "m_Colour");
            GL.BindAttribLocation(this, 2, "m_TexCoord");
            GL.BindAttribLocation(this, 3, "m_TexRect");
            GL.BindAttribLocation(this, 4, "m_BlendRange");

            GL.LinkProgram(this);

            int linkResult;
            GL.GetProgram(this, GetProgramParameterName.LinkStatus, out linkResult);
            string linkLog = GL.GetProgramInfoLog(this);

            Log.AppendLine(string.Format(ShaderPart.BOUNDARY, name));
            Log.AppendLine(string.Format("Linked: {0}", linkResult == 1));
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
                    StringBuilder uniformName = new StringBuilder(100);
                    GL.GetActiveUniform(this, i, 100, out length, out size, out type, uniformName);

                    string strName = uniformName.ToString();

                    uniformsArray[i] = new UniformBase(this, strName, GL.GetUniformLocation(this, strName), type);
                    uniforms.Add(strName, uniformsArray[i]);
                }

                foreach (KeyValuePair<string, object> kvp in globalProperties)
                {
                    if (!uniforms.ContainsKey(kvp.Key))
                        continue;
                    uniforms[kvp.Key].Value = kvp.Value;
                }

                allShaders.Add(this);
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

        /// <summary>
        /// Returns a uniform from the shader.
        /// </summary>
        /// <param name="name">The name of the uniform.</param>
        /// <returns>Returns a base uniform.</returns>
        public Uniform<T> GetUniform<T>(string name)
        {
            ensureLoaded();
            Debug.Assert(uniforms.ContainsKey(name), string.Format(@"Inexisting uniform {0} in shader {1}.", name, this.name));
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
            globalProperties[name] = value;

            for (int i = 0; i < allShaders.Count; i++)
            {
                allShaders[i].ensureLoaded();
                if (allShaders[i].uniforms.ContainsKey(name))
                    allShaders[i].uniforms[name].Value = value;
            }
        }

        public static implicit operator int(Shader shader)
        {
            return shader.programID;
        }
    }
}
