// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Graphics
{
    public class DrawableShader : Sprite
    {
        private readonly string shaderName;

        public DrawableShader(string shaderName)
        {
            this.shaderName = shaderName;
        }

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders, IRenderer renderer)
        {
            Texture ??= renderer.WhitePixel;
            TextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, shaderName);
        }

        protected override DrawNode CreateDrawNode() => new ShaderDrawNode(this);

        protected class ShaderDrawNode : SpriteDrawNode
        {
            public new DrawableShader Source => (DrawableShader)base.Source;

            public ShaderDrawNode(DrawableShader source)
                : base(source)
            {
            }

            protected override void Blit(IRenderer renderer)
            {
                UpdateUniforms(TextureShader);
                base.Blit(renderer);
            }

            protected virtual void UpdateUniforms(IShader shader)
            {
            }
        }
    }
}
