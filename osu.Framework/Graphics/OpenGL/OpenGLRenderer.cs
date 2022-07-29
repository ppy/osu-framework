// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.OpenGL.Buffers;
using osu.Framework.Graphics.Rendering;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL
{
    public class OpenGLRenderer : IRenderer
    {
        public IFrameBuffer CreateFrameBuffer(RenderbufferInternalFormat[]? renderBufferFormats = null, All filteringMode = All.Linear)
            => new FrameBuffer(renderBufferFormats, filteringMode);
    }
}
