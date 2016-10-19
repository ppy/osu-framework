// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.Textures;

namespace osu.Framework.Graphics.Shaders
{
    public class ShadedDrawable : Drawable
    {
        public override void Load(BaseGame game)
        {
            base.Load(game);

            // Still not happy about the right hand side of the conditional.
            // It exists because BasicGameHost.Load invokes this before Game.Load,
            // and hence before Game.Shaders has been created.
            if (shader == null && game.Shaders != null)
                shader = game.Shaders.Load(ShaderDescriptor);
        }

        protected override DrawNode CreateDrawNode() => new ShadedDrawNode();

        protected override void ApplyDrawNode(DrawNode node)
        {
            ShadedDrawNode n = node as ShadedDrawNode;
            n.Shader = shader;

            base.ApplyDrawNode(node);
        }

        protected virtual ShaderDescriptor ShaderDescriptor => new ShaderDescriptor(VertexShaderDescriptor.Texture2D, FragmentShaderDescriptor.TextureRounded);
        private Shader shader;
    }
}
