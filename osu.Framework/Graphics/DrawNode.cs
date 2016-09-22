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

        TripleBuffer<List<DrawNode>> childrenBuffer = new TripleBuffer<List<DrawNode>>();

        public DrawNode(DrawInfo drawInfo)
        {
            DrawInfo = drawInfo;
        }

        public void DrawSubTree()
        {
            PreDraw();

            GLWrapper.SetBlend(DrawInfo.Blending.Source, DrawInfo.Blending.Destination);

            Draw();

            using (var buffer = childrenBuffer.ForRead())
            {
                var drawNodes = buffer?.Object;

                if (drawNodes != null)
                {
                    foreach (DrawNode child in drawNodes)
                        child?.DrawSubTree();
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
            return childrenBuffer.ForWrite();
        }
    }

    public class ObjectUsage<T> : IDisposable
    {
        public T Object;

        internal Action<ObjectUsage<T>, UsageType> Finish;

        public UsageType Usage;

        public void Dispose()
        {
            Finish?.Invoke(this, Usage);
        }
    }

    public enum UsageType
    {
        Read,
        Write
    }

    public class TripleBuffer<T>
    {
        private List<ObjectUsage<T>> childLists = new List<ObjectUsage<T>>(3);

        Action<ObjectUsage<T>, UsageType> finishDelegate;

        private void finish(ObjectUsage<T> obj, UsageType type)
        {
            switch (type)
            {
                case UsageType.Read:
                    lock (childLists)
                        childLists.Add(obj);
                    break;
                case UsageType.Write:
                    lock (childLists)
                        childLists.Insert(0, obj);
                    break;
            }
        }

        public TripleBuffer()
        {
            finishDelegate = finish;
        }

        public ObjectUsage<T> ForWrite()
        {
            ObjectUsage<T> obj;
            lock (childLists)
            {
                if (childLists.Count < 2)
                    obj = new ObjectUsage<T>() { Finish = finishDelegate };
                else
                {
                    obj = childLists.Last();
                    childLists.RemoveAt(childLists.Count - 1);
                }
            }

            obj.Usage = UsageType.Write;
            return obj;
        }

        public ObjectUsage<T> ForRead()
        {
            ObjectUsage<T> obj;

            lock (childLists)
            {
                if (childLists.Count == 0) return null;

                obj = childLists[0];
                childLists.RemoveAt(0);
            }

            obj.Usage = UsageType.Read;
            return obj;
        }
    }
}