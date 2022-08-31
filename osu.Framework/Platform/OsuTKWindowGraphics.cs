// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable
using System;
using System.Drawing;
using System.Linq;
using osu.Framework.Logging;
using osuTK.Graphics;
using osuTK.Graphics.ES30;
using osuTK.Platform;

namespace osu.Framework.Platform
{
    public class OsuTKWindowGraphics : IWindowGraphics, IOpenGLWindowGraphics
    {
        private readonly IGameWindow window;

        public GraphicsBackend BackendType => GraphicsBackend.OpenGL;

        public IntPtr WindowHandle => window.WindowInfo.Handle;
        public IntPtr DisplayHandle => throw new NotSupportedException($@"{nameof(DisplayHandle)} is not supported.");
        public bool VerticalSync { get; set; }

        public IntPtr WindowContext => throw new NotSupportedException($@"{nameof(WindowContext)} is not supported.");
        public IntPtr CurrentContext => GraphicsContext.CurrentContextHandle.Handle;

        internal Version GLVersion;
        internal Version GLSLVersion;
        internal bool IsEmbedded;

        public OsuTKWindowGraphics(IGameWindow window)
        {
            this.window = window;
        }

        public void Initialise()
        {
            window.MakeCurrent();

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
        }

        private string getVersionNumberSubstring(string version)
        {
            string result = version.Split(' ').FirstOrDefault(s => char.IsDigit(s, 0));
            if (result != null) return result;

            throw new ArgumentException($"Cannot get version number from {version}!", nameof(version));
        }

        public Size GetDrawableSize() => window.ClientSize;
        public void MakeCurrent(IntPtr context) => window.MakeCurrent();
        public void SwapBuffers() => window.SwapBuffers();

        public void CreateContext() => throw new NotSupportedException($@"{nameof(CreateContext)} is not supported.");
        public void ClearCurrent() => throw new NotSupportedException($@"{nameof(ClearCurrent)} is not supported.");
        public void DeleteContext(IntPtr context) => throw new NotSupportedException($@"{nameof(DeleteContext)} is not supported.");
        public IntPtr GetProcAddress(string symbol) => throw new NotSupportedException($@"{nameof(GetProcAddress)} is not supported.");
    }
}
