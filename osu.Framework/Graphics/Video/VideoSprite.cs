// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Allocation;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Graphics.Video
{
    /// <summary>
    /// A sprite which holds a video with a custom conversion matrix.
    /// </summary>
    internal class VideoSprite : Sprite
    {
        private readonly Video video;

        public VideoSprite(Video video)
        {
            this.video = video;
        }

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders)
        {
            TextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.VIDEO);
            RoundedTextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.VIDEO_ROUNDED);
        }

        protected override DrawNode CreateDrawNode() => new VideoSpriteDrawNode(video);
    }
}
