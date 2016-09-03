//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK.Graphics;
using OpenTK;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.Shaders;

namespace osu.Framework.Graphics.Drawables
{
    public class Box : Drawable
    {
        private QuadBatch<Vertex2d> quadBatch = new QuadBatch<Vertex2d>(1, 3);

        protected override DrawNode BaseDrawNode => new BoxDrawNode(DrawInfo, Game, ScreenSpaceDrawQuad, quadBatch);
    }
}
