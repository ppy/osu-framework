// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using System.Drawing;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Graphics.Containers
{
    public class ContainerDrawNode : DrawNode
    {
        public List<DrawNode> Children;

        public MaskingInfo? MaskingInfo;

        protected override void PreDraw()
        {
            base.PreDraw();

            if (MaskingInfo != null)
                GLWrapper.PushScissor(MaskingInfo.Value);
        }

        protected override void Draw()
        {
            base.Draw();

            if (Children != null)
                foreach (DrawNode child in Children)
                    child.DrawSubTree();
        }

        protected override void PostDraw()
        {
            base.PostDraw();

            if (MaskingInfo != null)
                GLWrapper.PopScissor();
        }
    }
}
