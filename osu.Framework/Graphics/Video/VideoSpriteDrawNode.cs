// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.OpenGL.Vertices;

namespace osu.Framework.Graphics.Video
{
    public class VideoSpriteDrawNode : SpriteDrawNode
    {
        public VideoSpriteDrawNode(VideoSprite source)
            : base(source)
        {
        }

        private int yLoc, uLoc = 1, vLoc = 2;

        public override void Draw(Action<TexturedVertex2D> vertexAction)
        {
            Shader.GetUniform<int>("m_SamplerY").UpdateValue(ref yLoc);
            Shader.GetUniform<int>("m_SamplerU").UpdateValue(ref uLoc);
            Shader.GetUniform<int>("m_SamplerV").UpdateValue(ref vLoc);

            base.Draw(vertexAction);
        }
    }
}
