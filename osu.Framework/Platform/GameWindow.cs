// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Linq;
using osu.Framework.Configuration;
using osu.Framework.Logging;
using OpenTK;
using OpenTK.Graphics.ES30;
using OpenTK.Input;

namespace osu.Framework.Platform
{
    public abstract class GameWindow : OpenTK.GameWindow
    {
        internal Version GLVersion;
        internal Version GLSLVersion;

        protected GameWindow(int width, int height) : base(width, height)
        {
            Closing += (sender, e) => e.Cancel = ExitRequested?.Invoke() ?? false;
            Closed += (sender, e) => Exited?.Invoke();
            Cursor = MouseCursor.Empty;

            MakeCurrent();

            string version = GL.GetString(StringName.Version);
            string versionNumberSubstring = getVersionNumberSubstring(version);
            GLVersion = new Version(versionNumberSubstring);
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

            //Set up OpenGL related characteristics
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.StencilTest);
            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.ScissorTest);

            Logger.Log($@"GL Initialized
                        GL Version:                 {GL.GetString(StringName.Version)}
                        GL Renderer:                {GL.GetString(StringName.Renderer)}
                        GL Shader Language version: {GL.GetString(StringName.ShadingLanguageVersion)}
                        GL Vendor:                  {GL.GetString(StringName.Vendor)}
                        GL Extensions:              {GL.GetString(StringName.Extensions)}", LoggingTarget.Runtime, LogLevel.Important);

            Context.MakeCurrent(null);
        }

        private string getVersionNumberSubstring(string version)
        {
            string result = version.Split(' ').FirstOrDefault(s => char.IsDigit(s, 0));
            if (result != null) return result;
            throw new ArgumentException(nameof(version));
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);
        }

        public void SetTitle(string title)
        {
            Title = title;
        }

        public abstract void SetupWindow(FrameworkConfigManager config);

        /// <summary>
        /// Return value decides whether we should intercept and cancel this exit (if possible).
        /// </summary>
        public event Func<bool> ExitRequested;

        public event Action Exited;

        protected void OnExited() => Exited?.Invoke();

        protected bool OnExitRequested() => ExitRequested?.Invoke() ?? false;

        public virtual Vector2 Position { get; set; }

        public virtual void CycleMode() {}
    }
}
