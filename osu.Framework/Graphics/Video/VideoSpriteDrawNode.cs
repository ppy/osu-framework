// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osuTK;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Rendering;

namespace osu.Framework.Graphics.Video
{
    internal class VideoSpriteDrawNode : SpriteDrawNode
    {
        private readonly Video video;

        public VideoSpriteDrawNode(Video source)
            : base(source.Sprite)
        {
            video = source;
        }

        private int yLoc, uLoc = 1, vLoc = 2;

        public override void Draw(IRenderer renderer)
        {
            var shader = GetAppropriateShader(renderer);

            shader.GetUniform<int>("m_SamplerY").UpdateValue(ref yLoc);
            shader.GetUniform<int>("m_SamplerU").UpdateValue(ref uLoc);
            shader.GetUniform<int>("m_SamplerV").UpdateValue(ref vLoc);

            var yuvCoeff = video.ConversionMatrix;
            shader.GetUniform<Matrix3>("yuvCoeff").UpdateValue(ref yuvCoeff);

            base.Draw(renderer);
        }
    }
}
