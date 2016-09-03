using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace osu.Framework.Graphics.Containers
{
    public class MaskingContainerDrawNode : DrawNode
    {
        private Quad screenSpaceDrawQuad;

        public MaskingContainerDrawNode(DrawInfo drawInfo, Quad screenSpaceDrawQuad)
            : base(drawInfo)
        {
            this.screenSpaceDrawQuad = screenSpaceDrawQuad;
        }

        protected override void PreDraw()
        {
            base.PreDraw();

            GLWrapper.PushScissor(screenSpaceDrawQuad.BoundingRectangle);
        }

        protected override void PostDraw()
        {
            base.PostDraw();

            GLWrapper.PopScissor();
        }
    }
}
