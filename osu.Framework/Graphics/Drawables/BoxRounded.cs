// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.Shaders;

namespace osu.Framework.Graphics.Drawables
{
    public class BoxRounded : Drawable
    {
        public float Radius = 0.0f;

        private QuadBatch<TexturedVertex2D> quadBatch = null;

        protected override DrawNode CreateDrawNode() => new BoxRoundedDrawNode();

        private static Shader shader;

        protected override void ApplyDrawNode(DrawNode node)
        {
            BoxRoundedDrawNode n = node as BoxRoundedDrawNode;

            n.ScreenSpaceDrawQuad = ScreenSpaceDrawQuad;
            n.Shader = shader;
            n.Batch = quadBatch;
            n.Radius = Radius;
            n.Size = Size * Scale;

            base.ApplyDrawNode(node);
        }

        public override void Load(BaseGame game)
        {
            base.Load(game);

            if (shader == null)
                shader = game.Shaders.Load(VertexShader.Texture2D, FragmentShader.ColourRounded);
        }
    }
}
