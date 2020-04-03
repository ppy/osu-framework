// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Platform;
using osuTK;

namespace osu.Framework.Graphics.Video
{
    /// <summary>
    /// A sprite which holds a video with a custom conversion matrix. Use <see cref="Video"/> for loading and displaying a video.
    /// </summary>
    public class VideoSprite : Sprite
    {
        public VideoDecoder Decoder;

        [BackgroundDependencyLoader]
        private void load(GameHost gameHost, ShaderManager shaders)
        {
            TextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.VIDEO);
            RoundedTextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.VIDEO_ROUNDED);
        }

        /// <summary>
        /// YUV->RGB conversion matrix based on the video colorspace
        /// </summary>
        public Matrix3 ConversionMatrix => Decoder.GetConversionMatrix();

        protected override DrawNode CreateDrawNode() => new VideoSpriteDrawNode(this);
    }
}
