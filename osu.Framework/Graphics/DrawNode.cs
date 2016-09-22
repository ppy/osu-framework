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

        TripleBuffer<List<DrawNode>> childrenBuffer;

        public DrawNode(DrawInfo drawInfo)
        {
            DrawInfo = drawInfo;
        }

        public void DrawSubTree()
        {
            PreDraw();

            GLWrapper.SetBlend(DrawInfo.Blending.Source, DrawInfo.Blending.Destination);

            Draw();

            if (childrenBuffer != null)
            {
                using (var buffer = childrenBuffer.ForRead())
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
            if (childrenBuffer == null)
                childrenBuffer = new TripleBuffer<List<DrawNode>>();

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
        None,
        Read,
        Write
    }

    public class TripleBuffer<T>
    {
        private ObjectUsage<T>[] buffers = new ObjectUsage<T>[3];

        int read;
        int write;
        int lastWrite = -1;

        Action<ObjectUsage<T>, UsageType> finishDelegate;

        private void finish(ObjectUsage<T> obj, UsageType type)
        {
            switch (type)
            {
                case UsageType.Read:
                    lock (buffers)
                        buffers[read].Usage = UsageType.None;
                    break;
                case UsageType.Write:
                    lock (buffers)
                    {
                        buffers[write].Usage = UsageType.None;
                        lastWrite = write;
                    }
                    break;
            }
        }

        public TripleBuffer()
        {
            finishDelegate = finish;
        }

        public ObjectUsage<T> ForWrite()
        {
            lock (buffers)
            {
                while ((buffers[write]?.Usage == UsageType.Read) || write == lastWrite)
                    write = (write + 1) % 3;

                if (buffers[write] == null)
                {
                    buffers[write] = new ObjectUsage<T>
                    {
                        Finish = finishDelegate,
                        Usage = UsageType.Write
                    };
                }
                else
                {
                    buffers[write].Usage = UsageType.Write;
                }

                return buffers[write];
            }
        }

        public ObjectUsage<T> ForRead()
        {
            if (lastWrite < 0) return null;

            lock (buffers)
            {
                read = lastWrite;
                buffers[read].Usage = UsageType.Read;
                return buffers[read];
            }
        }
    }
}