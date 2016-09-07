//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Drawing;
using System.Windows.Forms;
using osu.Framework.Logging;
using OpenTK.Graphics;
using OpenTK.Graphics.ES20;

namespace osu.Framework.OS
{
    public class GLControl : OpenTK.GLControl
    {
        private string SupportedExtensions;

        internal Version GLVersion;
        internal Version GLSLVersion;

        public GLControl(GraphicsMode mode, int major, int minor, GraphicsContextFlags flags)
            : base(mode, major, minor, flags)
        {
        }

        /// <summary>
        /// Initialize this GLControl. Make sure this is called on the thread which will render stuff.
        /// </summary>
        public void Initialize()
        {
            //make sure our context is current on the correct frame.
            Invoke((MethodInvoker)delegate { Context.MakeCurrent(null); });
            MakeCurrent();

            string version = GL.GetString(StringName.Version);
            GLVersion = new Version(version.Split(' ')[0]);
            version = GL.GetString(StringName.ShadingLanguageVersion);
            if (!string.IsNullOrEmpty(version))
            {
                try
                {
                    GLSLVersion = new Version(version.Split(' ')[0]);
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
                        GL Version:                 { GL.GetString(StringName.Version)}
                        GL Renderer:                { GL.GetString(StringName.Renderer)}
                        GL Shader Language version: { GL.GetString(StringName.ShadingLanguageVersion)}
                        GL Vendor:                  { GL.GetString(StringName.Vendor)}
                        GL Extensions:              { GL.GetString(StringName.Extensions)}
                        GL Context:                 { GraphicsMode}", LoggingTarget.Runtime, LogLevel.Important);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            Cursor.Hide();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            Cursor.Show();
            base.OnMouseLeave(e);
        }

        internal bool CheckExtension(string extensionName)
        {
            try
            {
                if (string.IsNullOrEmpty(SupportedExtensions))
                    SupportedExtensions = GL.GetString(StringName.Extensions);

                return SupportedExtensions.Contains(extensionName);
            }
            catch { }

            return false;
        }

        bool firstDraw = true;
        protected override void OnPaint(PaintEventArgs e)
        {
            //block call to base method to allow for threaded GL drawing.

            if (firstDraw)
            {
                //avoid single white frame on startup.
                e.Graphics.Clear(Color.Black);
                firstDraw = false;
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            //block call to base method to allow for threaded GL drawing.
        }
    }
}
