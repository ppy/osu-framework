// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.OpenGL;

namespace osu.Framework.Graphics
{
    public class DrawNode
    {
        protected DrawInfo DrawInfo;

        internal TripleBuffer<List<DrawNode>> ChildrenBuffer;

        public DrawNode(DrawInfo drawInfo)
        {
            DrawInfo = drawInfo;
        }

        public void DrawSubTree()
        {
            PreDraw();

            GLWrapper.SetBlend(DrawInfo.Blending.Source, DrawInfo.Blending.Destination);

            Draw();

            if (ChildrenBuffer != null)
            {
                using (var buffer = ChildrenBuffer.Get(UsageType.Read))
                {
                    var drawNodes = buffer?.Object;

                    if (drawNodes != null)
                    {
                        foreach (DrawNode child in drawNodes)
                            child?.DrawSubTree();
                    }
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

        public ObjectUsage<List<DrawNode>> BeginChildrenUpdate()
        {
            if (ChildrenBuffer == null)
                ChildrenBuffer = new TripleBuffer<List<DrawNode>>();

            return ChildrenBuffer.Get(UsageType.Write);
        }
    }
}