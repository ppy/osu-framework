// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.OpenGL;

namespace osu.Framework.Graphics.Drawables
{
    public class Box : Drawable
    {
        private QuadBatch<Vertex2d> quadBatch = new QuadBatch<Vertex2d>(1, 3);

        protected override DrawNode CreateDrawNode() => new BoxDrawNode();

        protected override void ApplyDrawNode(DrawNode node)
        {
            BoxDrawNode n = node as BoxDrawNode;

            n.ScreenSpaceDrawQuad = ScreenSpaceDrawQuad;
            n.Batch = quadBatch;
            n.Game = Game;

            base.ApplyDrawNode(node);
        }
    }
}
