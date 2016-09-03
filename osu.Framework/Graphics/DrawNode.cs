//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace osu.Framework.Graphics
{
    public class DrawNode
    {
        public List<DrawNode> Children = new List<DrawNode>();
        public DrawInfo DrawInfo;

        public DrawNode(DrawInfo drawInfo)
        {
            DrawInfo = drawInfo;
        }

        public void DrawSubTree()
        {
            PreDraw();

            GLWrapper.SetBlend(DrawInfo.Blending.Source, DrawInfo.Blending.Destination);

            Draw();

            foreach (DrawNode child in Children)
                child.DrawSubTree();

            PostDraw();
        }

        protected virtual void PreDraw()
        {
        }

        protected virtual void Draw()
        {
        }

        protected virtual void PostDraw()
        {
        }
    }
}
