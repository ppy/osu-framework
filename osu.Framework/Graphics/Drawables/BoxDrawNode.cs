// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;

namespace osu.Framework.Graphics.Drawables
{
    public class BoxDrawNode : DrawNode
    {
        private static Shader shader;

        public Game Game;
        public Quad ScreenSpaceDrawQuad;
        public QuadBatch<Vertex2d> Batch;

        protected override void Draw()
        {
            base.Draw();

            if (shader == null)
                shader = Game.Shaders.Load(VertexShader.Colour, FragmentShader.Colour);

            shader.Bind();

            Batch.Add(new Vertex2d
            {
                Colour = DrawInfo.Colour,
                Position = ScreenSpaceDrawQuad.BottomLeft
            });
            Batch.Add(new Vertex2d
            {
                Colour = DrawInfo.Colour,
                Position = ScreenSpaceDrawQuad.BottomRight
            });
            Batch.Add(new Vertex2d
            {
                Colour = DrawInfo.Colour,
                Position = ScreenSpaceDrawQuad.TopRight
            });
            Batch.Add(new Vertex2d
            {
                Colour = DrawInfo.Colour,
                Position = ScreenSpaceDrawQuad.TopLeft
            });
            Batch.Draw();

            shader.Unbind();
        }
    }
}
