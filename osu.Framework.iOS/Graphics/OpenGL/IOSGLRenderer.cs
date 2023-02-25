// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using osu.Framework.Graphics.OpenGL;
using osuTK;
using osuTK.Graphics.ES30;

namespace osu.Framework.iOS.Graphics.OpenGL
{
    internal class IOSGLRenderer : GLRenderer
    {
        private int? backbufferFramebuffer;

        protected override int BackbufferFramebuffer
        {
            get
            {
                Debug.Assert(backbufferFramebuffer != null);
                return backbufferFramebuffer.Value;
            }
        }

        protected internal override void BeginFrame(Vector2 windowSize)
        {
            // this is supposed to be retrieved via SDL_SysWMinfo.info.uikit.framebuffer,
            // but apparently that returns a different handle from what's actually bound here on startup.
            backbufferFramebuffer ??= GL.GetInteger(GetPName.FramebufferBinding);

            base.BeginFrame(windowSize);
        }
    }
}
