// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Graphics
{
    public abstract partial class Drawable
    {
        private class ProxyDrawable : Drawable
        {
            private readonly ulong[] drawNodeValidationIds = new ulong[3];
            private readonly DrawNode[] originalDrawNodes;

            internal ProxyDrawable(Drawable original)
            {
                Original = original;
                originalDrawNodes = (original as ProxyDrawable)?.originalDrawNodes ?? original.drawNodes;
            }

            internal override void ValidateProxyDrawNode(int treeIndex, ulong frame)
            {
                if (HasProxy)
                {
                    // Proxy of a proxy - forward the next proxy
                    base.ValidateProxyDrawNode(treeIndex, frame);
                    return;
                }

                drawNodeValidationIds[treeIndex] = frame;
            }

            protected override DrawNode CreateDrawNode() => new ProxyDrawNode(this);

            internal override DrawNode GenerateDrawNodeSubtree(ulong frame, int treeIndex)
            {
                var node = (ProxyDrawNode)base.GenerateDrawNodeSubtree(frame, treeIndex);

                node.DrawNodeIndex = treeIndex;
                node.FrameCount = frame;

                return node;
            }

            internal sealed override Drawable Original { get; }

            public override bool RemoveWhenNotAlive => base.RemoveWhenNotAlive && Original.RemoveWhenNotAlive;

            protected internal override bool ShouldBeAlive => base.ShouldBeAlive && Original.ShouldBeAlive;

            // We do not want to receive updates. That is the business of the original drawable.
            public override bool IsPresent => false;

            public override bool UpdateSubTreeMasking(Drawable source, RectangleF maskingBounds) => Original.UpdateSubTreeMasking(this, maskingBounds);

            private class ProxyDrawNode : DrawNode
            {
                /// <summary>
                /// The index of the original draw node to draw.
                /// </summary>
                public int DrawNodeIndex;

                /// <summary>
                /// The current draw frame index.
                /// If this differs from the frame index of the original draw node, the original drawable will have not been drawn this frame.
                /// </summary>
                public ulong FrameCount;

                private readonly ProxyDrawable proxyDrawable;

                public ProxyDrawNode(ProxyDrawable proxyDrawable)
                {
                    this.proxyDrawable = proxyDrawable;
                }

                public override void Draw(Action<TexturedVertex2D> vertexAction)
                {
                    var target = proxyDrawable.originalDrawNodes[DrawNodeIndex];
                    if (target == null)
                        return;

                    if (proxyDrawable.drawNodeValidationIds[DrawNodeIndex] != FrameCount)
                        return;

                    target.Draw(vertexAction);
                }
            }
        }
    }
}
