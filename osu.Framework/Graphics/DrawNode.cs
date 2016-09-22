// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics.OpenGL;

namespace osu.Framework.Graphics
{
    public class DrawNode
    {
        protected DrawInfo DrawInfo;

        public DrawNode(DrawInfo drawInfo)
        {
            DrawInfo = drawInfo;
        }

        public void DrawSubTree()
        {
            PreDraw();

            GLWrapper.SetBlend(DrawInfo.Blending.Source, DrawInfo.Blending.Destination);

            Draw();

            List<DrawNode> drawNodes = null;

            lock (childLists)
            {
                if (childLists.Count > 0)
                {
                    drawNodes = childLists[0];
                    childLists.RemoveAt(0);
                }
            }

            if (drawNodes != null)
            {
                foreach (DrawNode child in drawNodes)
                    child?.DrawSubTree();

                lock (childLists)
                {
                    childLists.Add(drawNodes);
                }
            }

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

        private List<List<DrawNode>> childLists = new List<List<DrawNode>>(3);

        public List<DrawNode> BeginUpdate()
        {
            List<DrawNode> updateList;
            lock (childLists)
            {
                if (childLists.Count < 2)
                    updateList = new List<DrawNode>();
                else
                {
                    updateList = childLists.Last();
                    childLists.RemoveAt(childLists.Count - 1);
                }
            }

            updateList.Clear();

            return updateList;
        }

        public void EndUpdate(List<DrawNode> nc)
        {
            lock (childLists)
                childLists.Insert(0, nc);
        }
    }
}
