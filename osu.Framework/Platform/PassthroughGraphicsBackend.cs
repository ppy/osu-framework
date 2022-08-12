// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using osu.Framework.Logging;
using osuTK.Graphics;
using osuTK.Graphics.ES30;

namespace osu.Framework.Platform
{
    /// <summary>
    /// Implementation of <see cref="IGraphicsBackend"/> that force-loads OpenGL
    /// endpoints into osuTK's bindings.
    /// </summary>
    public abstract class PassthroughGraphicsBackend : IGraphicsBackend
    {
        internal IntPtr Context;

        internal Version GLVersion { get; private set; }

        internal Version GLSLVersion { get; private set; }

        internal bool IsEmbedded { get; private set; }

        public abstract bool VerticalSync { get; set; }

        protected abstract IntPtr CreateContext();
        protected abstract void MakeCurrent(IntPtr context);
        protected abstract IntPtr GetProcAddress(string symbol);

        public abstract void SwapBuffers();

        public abstract void InitialiseBeforeWindowCreation();

        public virtual void Initialise(IWindow window)
        {
            Context = CreateContext();

            MakeCurrent(Context);

            loadTKBindings();

            string version = GL.GetString(StringName.Version);
            string versionNumberSubstring = getVersionNumberSubstring(version);

            GLVersion = new Version(versionNumberSubstring);

            // As defined by https://www.khronos.org/registry/OpenGL-Refpages/es2.0/xhtml/glGetString.xml
            IsEmbedded = version.Contains("OpenGL ES");

            version = GL.GetString(StringName.ShadingLanguageVersion);

            if (!string.IsNullOrEmpty(version))
            {
                try
                {
                    GLSLVersion = new Version(versionNumberSubstring);
                }
                catch (Exception e)
                {
                    Logger.Error(e, $@"couldn't set GLSL version using string '{version}'");
                }
            }

            if (GLSLVersion == null)
                GLSLVersion = new Version();

            Logger.Log($@"GL Initialized
                        GL Version:                 {GL.GetString(StringName.Version)}
                        GL Renderer:                {GL.GetString(StringName.Renderer)}
                        GL Shader Language version: {GL.GetString(StringName.ShadingLanguageVersion)}
                        GL Vendor:                  {GL.GetString(StringName.Vendor)}
                        GL Extensions:              {GL.GetString(StringName.Extensions)}");

            // We need to release the context in this thread, since Windows locks it and prevents
            // the draw thread from taking it. macOS seems to gracefully ignore this.
            MakeCurrent(IntPtr.Zero);
        }

        public void MakeCurrent() => MakeCurrent(Context);

        public void ClearCurrent() => MakeCurrent(IntPtr.Zero);

        private void loadTKBindings()
        {
            loadEntryPoints(new osuTK.Graphics.OpenGL.GL());
            loadEntryPoints(new osuTK.Graphics.OpenGL4.GL());
            loadEntryPoints(new osuTK.Graphics.ES11.GL());
            loadEntryPoints(new osuTK.Graphics.ES20.GL());
            loadEntryPoints(new GL());
        }

        private unsafe void loadEntryPoints(GraphicsBindingsBase bindings)
        {
            var type = bindings.GetType();
            var pointsInfo = type.GetRuntimeFields().First(x => x.Name == "_EntryPointsInstance");
            var namesInfo = type.GetRuntimeFields().First(x => x.Name == "_EntryPointNamesInstance");
            var offsetsInfo = type.GetRuntimeFields().First(x => x.Name == "_EntryPointNameOffsetsInstance");

            var entryPointsInstance = (IntPtr[])pointsInfo.GetValue(bindings);
            byte[] entryPointNamesInstance = (byte[])namesInfo.GetValue(bindings);
            int[] entryPointNameOffsetsInstance = (int[])offsetsInfo.GetValue(bindings);

            Debug.Assert(entryPointsInstance != null);
            Debug.Assert(entryPointNameOffsetsInstance != null);

            fixed (byte* name = entryPointNamesInstance)
            {
                for (int i = 0; i < entryPointsInstance.Length; i++)
                {
                    byte* ptr = name + entryPointNameOffsetsInstance[i];
                    string str = Marshal.PtrToStringAnsi(new IntPtr(ptr));
                    entryPointsInstance[i] = GetProcAddress(str);
                }
            }

            pointsInfo.SetValue(bindings, entryPointsInstance);
        }

        private string getVersionNumberSubstring(string version)
        {
            string result = version.Split(' ').FirstOrDefault(s => char.IsDigit(s, 0));
            if (result != null) return result;

            throw new ArgumentException($"Invalid version string: \"{version}\"", nameof(version));
        }
    }
}
