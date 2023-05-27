// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;

namespace osu.Framework.Graphics
{
    public abstract class TexturedShaderDrawNode : DrawNode
    {
        private IShader textureShader;

        protected new ITexturedShaderDrawable Source => (ITexturedShaderDrawable)base.Source;

        protected TexturedShaderDrawNode(ITexturedShaderDrawable source)
            : base(source)
        {
        }

        public override void ApplyState()
        {
            base.ApplyState();

            textureShader = Source.TextureShader;
        }

        /// <summary>
        /// Binds the <see cref="IShader"/> used for rendering the texture.
        /// </summary>
        /// <param name="renderer">The renderer to use for setting up uniform resources.</param>
        protected void BindTextureShader(IRenderer renderer)
        {
            textureShader.Bind();

            BindUniformResources(textureShader, renderer);
        }

        protected void UnbindTextureShader(IRenderer renderer) => textureShader.Unbind();

        /// <summary>
        /// Binds uniform resources against the provided shader.
        /// </summary>
        /// <param name="shader">The shader to bind uniform resources against.</param>
        /// <param name="renderer">The renderer to use for setting up uniform resources.</param>
        protected virtual void BindUniformResources(IShader shader, IRenderer renderer)
        {
        }
    }
}
