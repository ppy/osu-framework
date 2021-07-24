// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Graphics
{
    public abstract partial class Drawable
    {
        private class ProxyDrawable : Drawable
        {
            private readonly ulong[] drawNodeValidationIds = new ulong[GLWrapper.MAX_DRAW_NODES];
            private readonly DrawNode[] originalDrawNodes;

            internal ProxyDrawable(Drawable original)
            {
                Original = original;
                originalDrawNodes = (original as ProxyDrawable)?.originalDrawNodes ?? original.drawNodes;

                original.LifetimeChanged += _ => LifetimeChanged?.Invoke(this);
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

            internal override DrawNode GenerateDrawNodeSubtree(ulong frame, int treeIndex, bool forceNewDrawNode)
            {
                var node = (ProxyDrawNode)base.GenerateDrawNodeSubtree(frame, treeIndex, forceNewDrawNode);

                node.DrawNodeIndex = treeIndex;
                node.FrameCount = frame;

                return node;
            }

            internal sealed override Drawable Original { get; }

            public override bool RemoveWhenNotAlive => Original.RemoveWhenNotAlive;

            protected internal override bool ShouldBeAlive => Original.ShouldBeAlive;

            public override double LifetimeStart => Original.LifetimeStart;

            public override double LifetimeEnd => Original.LifetimeEnd;

            // We do not want to receive updates. That is the business of the original drawable.
            public override bool IsPresent => false;

            public override bool UpdateSubTreeMasking(Drawable source, RectangleF maskingBounds)
            {
                if (Original.IsDisposed)
                    return false;

                return Original.UpdateSubTreeMasking(this, maskingBounds);
            }

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

                protected new ProxyDrawable Source => (ProxyDrawable)base.Source;

                public ProxyDrawNode(ProxyDrawable proxyDrawable)
                    : base(proxyDrawable)
                {
                }

                internal override void DrawOpaqueInteriorSubTree(DepthValue depthValue, Action<TexturedVertex2D> vertexAction)
                    => getCurrentFrameSource()?.DrawOpaqueInteriorSubTree(depthValue, vertexAction);

                public override void Draw(Action<TexturedVertex2D> vertexAction)
                    => getCurrentFrameSource()?.Draw(vertexAction);

                protected internal override bool CanDrawOpaqueInterior => getCurrentFrameSource()?.CanDrawOpaqueInterior ?? false;

                private DrawNode getCurrentFrameSource()
                {
                    var target = Source.originalDrawNodes[DrawNodeIndex];

                    if (target == null)
                        return null;

                    if (Source.drawNodeValidationIds[DrawNodeIndex] != FrameCount)
                        return null;

                    if (target.IsDisposed)
                        return null;

                    return target;
                }
            }
        }
    }
}
