// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;

namespace osu.Framework.Graphics.Drawables
{
    public class BoxDrawNode : DrawNode
    {
        public float Radius;
        public Vector2 Size;

        private static VertexBatch<TexturedVertex2D> quadBatch;

        public Shader Shader;
        public Quad ScreenSpaceDrawQuad;
        public VertexBatch<TexturedVertex2D> Batch;

        protected override void Draw()
        {
            base.Draw();

            if (!Shader.Loaded) Shader.Compile();

            Shader.GetUniform<Vector4>(@"g_TexRect").Value = new Vector4(0, 0, Size.X, Size.Y);
            Shader.GetUniform<float>(@"g_Radius").Value = Radius;

            Shader.Bind();

            if (Batch == null)
            {
                if (quadBatch == null)
                    quadBatch = new QuadBatch<TexturedVertex2D>(1, 100);
                Batch = quadBatch;
            }

            Batch.Add(new TexturedVertex2D
            {
                Colour = DrawInfo.Colour,
                Position = ScreenSpaceDrawQuad.BottomLeft,
                TexturePosition = new Vector2(0, Size.Y),
            });
            Batch.Add(new TexturedVertex2D
            {
                Colour = DrawInfo.Colour,
                Position = ScreenSpaceDrawQuad.BottomRight,
                TexturePosition = new Vector2(Size.X, Size.Y),
            });
            Batch.Add(new TexturedVertex2D
            {
                Colour = DrawInfo.Colour,
                Position = ScreenSpaceDrawQuad.TopRight,
                TexturePosition = new Vector2(Size.X, 0),
            });
            Batch.Add(new TexturedVertex2D
            {
                Colour = DrawInfo.Colour,
                Position = ScreenSpaceDrawQuad.TopLeft,
                TexturePosition = new Vector2(0, 0),
            });

            Shader.Unbind();
        }
    }
}
