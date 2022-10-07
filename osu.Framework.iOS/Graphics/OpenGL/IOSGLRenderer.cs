// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.OpenGL;

namespace osu.Framework.iOS.Graphics.OpenGL
{
    internal class IOSGLRenderer : GLRenderer
    {
        private readonly IOSGameView gameView;

        public IOSGLRenderer(IOSGameView gameView)
        {
            this.gameView = gameView;
        }

        protected override int BackbufferFramebuffer => gameView.DefaultFrameBuffer;
    }
}
