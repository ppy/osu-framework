// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.Shaders;

namespace osu.Framework.Graphics.Drawables
{
    public class Box : Drawable
    {
        // Set this to 1.0f to turn on linear interpolation for making Box edges look smoother, at the cose of blurring boundaries.
        public float Radius = 0.0f;

        protected override DrawNode CreateDrawNode() => new BoxDrawNode();

        private static Shader shader;

        protected override void ApplyDrawNode(DrawNode node)
        {
            BoxDrawNode n = node as BoxDrawNode;

            n.ScreenSpaceDrawQuad = ScreenSpaceDrawQuad;
            n.Shader = shader;
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
