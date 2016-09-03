//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace osu.Framework.Graphics.Drawables
{
    public class BoxDrawNode : DrawNode
    {
        private static Shader shader;

        private Game game;
        private Quad screenSpaceDrawQuad;
        private QuadBatch<Vertex2d> batch;

        public BoxDrawNode(DrawInfo drawInfo, Game game, Quad screenSpaceDrawQuad, QuadBatch<Vertex2d> batch)
            : base(drawInfo)
        {
            this.game = game;
            this.screenSpaceDrawQuad = screenSpaceDrawQuad;
            this.batch = batch;
        }

        protected override void Draw()
        {
            base.Draw();

            if (shader == null)
                shader = game.Shaders.Load(VertexShader.Colour, FragmentShader.Colour);

            shader.Bind();

            batch.Add(new Vertex2d() { Colour = DrawInfo.Colour, Position = screenSpaceDrawQuad.BottomLeft });
            batch.Add(new Vertex2d() { Colour = DrawInfo.Colour, Position = screenSpaceDrawQuad.BottomRight });
            batch.Add(new Vertex2d() { Colour = DrawInfo.Colour, Position = screenSpaceDrawQuad.TopRight });
            batch.Add(new Vertex2d() { Colour = DrawInfo.Colour, Position = screenSpaceDrawQuad.TopLeft });
            batch.Draw();

            shader.Unbind();
        }
    }
}
