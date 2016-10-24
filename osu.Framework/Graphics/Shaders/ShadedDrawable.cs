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

            if (shader == null)
                // TODO: Ensure game is never null, and already loaded in here.
                shader = game?.Shaders?.Load(ShaderDescriptor);
        }

        protected override DrawNode CreateDrawNode() => new ShadedDrawNode();
        protected override bool IsCompatibleDrawNode(DrawNode node) => node is ShadedDrawNode;

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
