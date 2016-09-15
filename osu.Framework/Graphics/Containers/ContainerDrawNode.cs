//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.OpenGL;
using System.Drawing;

namespace osu.Framework.Graphics.Containers
{
    public class ContainerDrawNode : DrawNode
    {
        private Rectangle? maskingRect;

        public ContainerDrawNode(DrawInfo drawInfo, Rectangle? masking = null) : base(drawInfo)
        {
            maskingRect = masking;
        }

        protected override void PreDraw()
        {
            base.PreDraw();

            if (maskingRect != null)
                GLWrapper.PushScissor(maskingRect);
        }

        protected override void PostDraw()
        {
            base.PostDraw();

            if (maskingRect != null)
                GLWrapper.PopScissor();
        }
    }
}
