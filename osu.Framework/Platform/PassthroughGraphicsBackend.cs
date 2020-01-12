// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Logging;
using osuTK.Graphics;
using osuTK.Graphics.ES30;
using Veldrid.Sdl2;

namespace osu.Framework.Platform
{
    /// <summary>
    /// Implementation of <see cref="IGraphicsBackend"/> that force-loads OpenGL
    /// endpoints into osuTK's bindings.
    /// </summary>
    public class PassthroughGraphicsBackend : IGraphicsBackend
    {
        private bool initialised;

        internal IntPtr SdlWindowHandle;
        internal IntPtr Context;

        internal Version GLVersion { get; private set; }

        internal Version GLSLVersion { get; private set; }

        internal bool IsEmbedded { get; private set; }

        public bool VerticalSync
        {
            get => Sdl2Functions.SDL_GL_GetSwapInterval() != 0;
            set => Sdl2Native.SDL_GL_SetSwapInterval(value ? 1 : 0);
        }

        public void Initialise(IWindowBackend windowBackend)
        {
            if (initialised)
                return;

            initialised = true;

            if (!(windowBackend is Sdl2WindowBackend sdlWindowBackend))
                return;

            SdlWindowHandle = sdlWindowBackend.SdlWindowHandle;
            Context = Sdl2Native.SDL_GL_CreateContext(SdlWindowHandle);

            MakeCurrent();

            loadTKBindings();

            string version = GL.GetString(StringName.Version);
            string versionNumberSubstring = getVersionNumberSubstring(version);

            GLVersion = new Version(versionNumberSubstring);

            // As defined by https://www.khronos.org/registry/OpenGL-Refpages/es2.0/xhtml/glGetString.xml
            IsEmbedded = version.Contains("OpenGL ES");
            GLWrapper.IsEmbedded = IsEmbedded;

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
            Sdl2Native.SDL_GL_MakeCurrent(SdlWindowHandle, IntPtr.Zero);
        }

        public void MakeCurrent() => Sdl2Native.SDL_GL_MakeCurrent(SdlWindowHandle, Context);

        public void SwapBuffers() => Sdl2Native.SDL_GL_SwapWindow(SdlWindowHandle);

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
            var entryPointNamesInstance = (byte[])namesInfo.GetValue(bindings);
            var entryPointNameOffsetsInstance = (int[])offsetsInfo.GetValue(bindings);

            fixed (byte* name = entryPointNamesInstance)
            {
                for (int i = 0; i < entryPointsInstance.Length; i++)
                {
                    var ptr = name + entryPointNameOffsetsInstance[i];
                    var str = Marshal.PtrToStringAnsi(new IntPtr(ptr));
                    entryPointsInstance[i] = Sdl2Native.SDL_GL_GetProcAddress(str);
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
