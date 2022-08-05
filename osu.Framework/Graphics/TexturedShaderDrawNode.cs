// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;

namespace osu.Framework.Graphics
{
    public abstract class TexturedShaderDrawNode : DrawNode
    {
        protected IShader TextureShader { get; private set; }
        protected IShader RoundedTextureShader { get; private set; }

        protected new ITexturedShaderDrawable Source => (ITexturedShaderDrawable)base.Source;

        protected TexturedShaderDrawNode(ITexturedShaderDrawable source)
            : base(source)
        {
        }

        public override void ApplyState()
        {
            base.ApplyState();

            TextureShader = Source.TextureShader;
            RoundedTextureShader = Source.RoundedTextureShader;
        }

        /// <summary>
        /// Gets the appropriate <see cref="IShader"/> to use for the current masking state.
        /// This will return <see cref="RoundedTextureShader"/> if masking is active, otherwise <see cref="TextureShader"/>.
        /// </summary>
        /// <param name="renderer">The renderer that will be drawn with.</param>
        protected IShader GetAppropriateShader(IRenderer renderer) => RequiresRoundedShader(renderer) ? RoundedTextureShader : TextureShader;

        /// <summary>
        /// Whether rounded texture is required for the current draw state (masking, etc).
        /// </summary>
        /// <param name="renderer">The renderer that will be drawn with.</param>
        protected virtual bool RequiresRoundedShader(IRenderer renderer) => renderer.IsMaskingActive;
    }
}
