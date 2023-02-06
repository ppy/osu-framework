// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders.Types;

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

        private IUniformBuffer<YuvData> yuvDataBuffer;

        public override void Draw(IRenderer renderer)
        {
            yuvDataBuffer ??= renderer.CreateUniformBuffer<YuvData>();
            yuvDataBuffer.Data = yuvDataBuffer.Data with { YuvCoeff = video.ConversionMatrix };

            var shader = TextureShader;
            shader.AssignUniformBlock("m_yuvData", yuvDataBuffer);

            base.Draw(renderer);
        }

        private record struct YuvData
        {
            public UniformMatrix3 YuvCoeff;
        }
    }
}
