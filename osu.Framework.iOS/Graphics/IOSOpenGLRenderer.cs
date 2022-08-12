// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.iOS;
using osu.Framework.Graphics.OpenGL;

namespace osu.Framework.iOS.Graphics
{
    internal class IOSOpenGLRenderer : OpenGLRenderer
    {
        private readonly IOSGameView gameView;

        public IOSOpenGLRenderer(IOSGameView gameView)
        {
            this.gameView = gameView;
        }

        protected override int BackbufferFramebuffer => gameView.DefaultFrameBuffer;
    }
}
