// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable
using System;
using System.Drawing;
using System.Linq;
using osu.Framework.Logging;
using osuTK.Graphics;
using osuTK.Graphics.ES30;

namespace osu.Framework.Platform
{
    public class OsuTKGraphicsSurface : IGraphicsSurface, IOpenGLGraphicsSurface
    {
        private readonly OsuTKWindow window;

        public GraphicsSurfaceType Type => GraphicsSurfaceType.OpenGL;

        public IntPtr WindowHandle => window.WindowInfo.Handle;
        public IntPtr DisplayHandle => throw new NotSupportedException($@"{nameof(DisplayHandle)} is not supported.");

        public bool VerticalSync { get; set; }

        public IntPtr WindowContext { get; private set; }
        public IntPtr CurrentContext => GraphicsContext.CurrentContextHandle.Handle;

        internal Version GLVersion;
        internal Version GLSLVersion;
        internal bool IsEmbedded;

        public OsuTKGraphicsSurface(OsuTKWindow window)
        {
            this.window = window;
        }

        public void Initialise()
        {
            window.MakeCurrent();

            // there's no sane way to retrieve the GL context of the window so just use the current context after making window current.
            WindowContext = CurrentContext;

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
        public void ClearCurrent() => window.ClearCurrent();
        public void SwapBuffers() => window.SwapBuffers();

        public void CreateContext() => throw new NotSupportedException($@"{nameof(CreateContext)} is not supported.");
        public void DeleteContext(IntPtr context) => throw new NotSupportedException($@"{nameof(DeleteContext)} is not supported.");
        public IntPtr GetProcAddress(string symbol) => throw new NotSupportedException($@"{nameof(GetProcAddress)} is not supported.");
    }
}
