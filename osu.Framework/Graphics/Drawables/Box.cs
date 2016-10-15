// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.Shaders;

namespace osu.Framework.Graphics.Drawables
{
    public class Box : Drawable
    {
        protected override DrawNode CreateDrawNode() => new BoxDrawNode();

        private static Shader shader;

        protected override void ApplyDrawNode(DrawNode node)
        {
            BoxDrawNode n = node as BoxDrawNode;

            n.ScreenSpaceDrawQuad = ScreenSpaceDrawQuad;
            n.Shader = shader;

            base.ApplyDrawNode(node);
        }

        public override void Load(BaseGame game)
        {
            base.Load(game);

            if (shader == null)
                shader = game.Shaders.Load(VertexShader.Colour, FragmentShader.Colour);
        }
    }
}
