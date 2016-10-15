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
        private static VertexBatch<Vertex2D> quadBatch;

        public Shader Shader;
        public Quad ScreenSpaceDrawQuad;
        public VertexBatch<Vertex2D> Batch;

        protected override void Draw()
        {
            base.Draw();

            if (!Shader.Loaded) Shader.Compile();

            Shader.Bind();

            if (Batch == null)
            {
                if (quadBatch == null)
                    quadBatch = new QuadBatch<Vertex2D>(1, 100);
                Batch = quadBatch;
            }

            Batch.Add(new Vertex2D
            {
                Colour = DrawInfo.Colour,
                Position = ScreenSpaceDrawQuad.BottomLeft
            });
            Batch.Add(new Vertex2D
            {
                Colour = DrawInfo.Colour,
                Position = ScreenSpaceDrawQuad.BottomRight
            });
            Batch.Add(new Vertex2D
            {
                Colour = DrawInfo.Colour,
                Position = ScreenSpaceDrawQuad.TopRight
            });
            Batch.Add(new Vertex2D
            {
                Colour = DrawInfo.Colour,
                Position = ScreenSpaceDrawQuad.TopLeft
            });

            Shader.Unbind();
        }
    }
}
