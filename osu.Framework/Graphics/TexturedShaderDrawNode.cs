// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.Shaders;

namespace osu.Framework.Graphics
{
    public abstract class TexturedShaderDrawNode : DrawNode
    {
        protected IShader Shader => RequiresRoundedShader ? roundedTextureShader : textureShader;

        private IShader textureShader;
        private IShader roundedTextureShader;

        protected new ITexturedShaderDrawable Source => (ITexturedShaderDrawable)base.Source;

        protected TexturedShaderDrawNode(ITexturedShaderDrawable source)
            : base(source)
        {
        }

        public override void ApplyState()
        {
            base.ApplyState();

            textureShader = Source.TextureShader;
            roundedTextureShader = Source.RoundedTextureShader;
        }

        protected virtual bool RequiresRoundedShader => GLWrapper.IsMaskingActive;
    }
}
