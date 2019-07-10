﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Batches;
using osuTK;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Colour;
using System;
using System.Runtime.CompilerServices;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.OpenGL.Vertices;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.Containers
{
    public partial class CompositeDrawable
    {
        /// <summary>
        /// A draw node responsible for rendering a <see cref="CompositeDrawable"/> and the <see cref="DrawNode"/>s of its children.
        /// </summary>
        protected class CompositeDrawableDrawNode : DrawNode, ICompositeDrawNode
        {
            private static readonly float cos_45 = (float)Math.Cos(Math.PI / 4);

            protected new CompositeDrawable Source => (CompositeDrawable)base.Source;

            /// <summary>
            /// The <see cref="IShader"/> to use for rendering.
            /// </summary>
            protected IShader Shader { get; private set; }

            /// <summary>
            /// The <see cref="DrawNode"/>s of the children of our <see cref="CompositeDrawable"/>.
            /// </summary>
            public List<DrawNode> Children { get; set; }

            /// <summary>
            /// Information about how masking of children should be carried out.
            /// </summary>
            private MaskingInfo? maskingInfo;

            /// <summary>
            /// The screen-space version of <see cref="OpenGL.MaskingInfo.MaskingRect"/>.
            /// Used as cache of screen-space masking quads computed in previous frames.
            /// Assign null to reset.
            /// </summary>
            private Quad? screenSpaceMaskingQuad;

            /// <summary>
            /// Information about how the edge effect should be rendered.
            /// </summary>
            private EdgeEffectParameters edgeEffect;

            /// <summary>
            /// Whether to use a local vertex batch for rendering. If false, a parenting vertex batch will be used.
            /// </summary>
            private bool forceLocalVertexBatch;

            /// <summary>
            /// The vertex batch used for child quads during the back-to-front pass.
            /// </summary>
            private QuadBatch<TexturedVertex2D> quadBatch;

            /// <summary>
            /// The vertex batch used for child triangles during the front-to-back pass.
            /// </summary>
            private LinearBatch<TexturedVertex2D> triangleBatch;

            public CompositeDrawableDrawNode(CompositeDrawable source)
                : base(source)
            {
            }

            public override void ApplyState()
            {
                base.ApplyState();

                if (!Source.Masking && (Source.BorderThickness != 0.0f || edgeEffect.Type != EdgeEffectType.None))
                    throw new InvalidOperationException("Can not have border effects/edge effects if masking is disabled.");

                Vector3 scale = DrawInfo.MatrixInverse.ExtractScale();
                float blendRange = Source.MaskingSmoothness * (scale.X + scale.Y) / 2;

                // Calculate a shrunk rectangle which is free from corner radius/smoothing/border effects
                float shrinkage = Source.CornerRadius - Source.CornerRadius * cos_45 + blendRange + Source.borderThickness;
                RectangleF shrunkDrawRectangle = Source.DrawRectangle.Shrink(shrinkage);

                maskingInfo = !Source.Masking
                    ? (MaskingInfo?)null
                    : new MaskingInfo
                    {
                        ScreenSpaceAABB = Source.ScreenSpaceDrawQuad.AABB,
                        MaskingRect = Source.DrawRectangle,
                        ConservativeScreenSpaceQuad = Quad.FromRectangle(shrunkDrawRectangle) * DrawInfo.Matrix,
                        ToMaskingSpace = DrawInfo.MatrixInverse,
                        CornerRadius = Source.CornerRadius,
                        BorderThickness = Source.BorderThickness,
                        BorderColour = Source.BorderColour,
                        // We are setting the linear blend range to the approximate size of a _pixel_ here.
                        // This results in the optimal trade-off between crispness and smoothness of the
                        // edges of the masked region according to sampling theory.
                        BlendRange = blendRange,
                        AlphaExponent = 1,
                    };

                edgeEffect = Source.EdgeEffect;
                screenSpaceMaskingQuad = null;
                Shader = Source.Shader;
                forceLocalVertexBatch = Source.ForceLocalVertexBatch;
            }

            public virtual bool AddChildDrawNodes => true;

            private void drawEdgeEffect()
            {
                if (maskingInfo == null || edgeEffect.Type == EdgeEffectType.None || edgeEffect.Radius <= 0.0f || edgeEffect.Colour.Linear.A <= 0)
                    return;

                RectangleF effectRect = maskingInfo.Value.MaskingRect.Inflate(edgeEffect.Radius).Offset(edgeEffect.Offset);

                if (!screenSpaceMaskingQuad.HasValue)
                    screenSpaceMaskingQuad = Quad.FromRectangle(effectRect) * DrawInfo.Matrix;

                MaskingInfo edgeEffectMaskingInfo = maskingInfo.Value;
                edgeEffectMaskingInfo.MaskingRect = effectRect;
                edgeEffectMaskingInfo.ScreenSpaceAABB = screenSpaceMaskingQuad.Value.AABB;
                edgeEffectMaskingInfo.CornerRadius = maskingInfo.Value.CornerRadius + edgeEffect.Radius + edgeEffect.Roundness;
                edgeEffectMaskingInfo.BorderThickness = 0;
                // HACK HACK HACK. We abuse blend range to give us the linear alpha gradient of
                // the edge effect along its radius using the same rounded-corners shader.
                edgeEffectMaskingInfo.BlendRange = edgeEffect.Radius;
                edgeEffectMaskingInfo.AlphaExponent = 2;
                edgeEffectMaskingInfo.EdgeOffset = edgeEffect.Offset;
                edgeEffectMaskingInfo.Hollow = edgeEffect.Hollow;
                edgeEffectMaskingInfo.HollowCornerRadius = maskingInfo.Value.CornerRadius + edgeEffect.Radius;

                GLWrapper.PushMaskingInfo(edgeEffectMaskingInfo);

                GLWrapper.SetBlend(new BlendingInfo(edgeEffect.Type == EdgeEffectType.Glow ? BlendingMode.Additive : BlendingMode.Mixture));

                Shader.Bind();

                ColourInfo colour = ColourInfo.SingleColour(edgeEffect.Colour);
                colour.TopLeft.MultiplyAlpha(DrawColourInfo.Colour.TopLeft.Linear.A);
                colour.BottomLeft.MultiplyAlpha(DrawColourInfo.Colour.BottomLeft.Linear.A);
                colour.TopRight.MultiplyAlpha(DrawColourInfo.Colour.TopRight.Linear.A);
                colour.BottomRight.MultiplyAlpha(DrawColourInfo.Colour.BottomRight.Linear.A);

                DrawQuad(
                    Texture.WhitePixel,
                    screenSpaceMaskingQuad.Value,
                    colour, null, null, null,
                    // HACK HACK HACK. We re-use the unused vertex blend range to store the original
                    // masking blend range when rendering edge effects. This is needed for smooth inner edges
                    // with a hollow edge effect.
                    new Vector2(maskingInfo.Value.BlendRange));

                Shader.Unbind();

                GLWrapper.PopMaskingInfo();
            }

            private const int min_amount_children_to_warrant_batch = 5;

            private bool mayHaveOwnVertexBatch(int amountChildren) => forceLocalVertexBatch || amountChildren >= min_amount_children_to_warrant_batch;

            private void updateQuadBatch()
            {
                if (Children == null)
                    return;

                // This logic got roughly copied from the old osu! code base. These constants seem to have worked well so far.
                int clampedAmountChildren = MathHelper.Clamp(Children.Count, 1, 1000);
                if (mayHaveOwnVertexBatch(clampedAmountChildren) && (quadBatch == null || quadBatch.Size < clampedAmountChildren))
                    quadBatch = new QuadBatch<TexturedVertex2D>(clampedAmountChildren * 2, 500);
            }

            private void updateTriangleBatch()
            {
                if (Children == null)
                    return;

                // This logic got roughly copied from the old osu! code base. These constants seem to have worked well so far.
                int clampedAmountChildren = MathHelper.Clamp(Children.Count, 1, 1000);

                if (mayHaveOwnVertexBatch(clampedAmountChildren) && (triangleBatch == null || triangleBatch.Size < clampedAmountChildren))
                {
                    // The same general idea as updateQuadBatch(), except that each child draws up to 3 vertices * 6 triangles after quad-quad intersection
                    triangleBatch = new LinearBatch<TexturedVertex2D>(clampedAmountChildren * 2 * 3, 500, PrimitiveType.Triangles);
                }
            }

            public override void Draw(Action<TexturedVertex2D> vertexAction)
            {
                updateQuadBatch();

                // Prefer to use own vertex batch instead of the parent-owned one.
                if (quadBatch != null)
                    vertexAction = quadBatch.AddAction;

                base.Draw(vertexAction);

                drawEdgeEffect();

                if (maskingInfo != null)
                {
                    MaskingInfo info = maskingInfo.Value;
                    if (info.BorderThickness > 0)
                        info.BorderColour *= DrawColourInfo.Colour.AverageColour;

                    GLWrapper.PushMaskingInfo(info);
                }

                if (Children != null)
                    for (int i = 0; i < Children.Count; i++)
                        Children[i].Draw(vertexAction);

                if (maskingInfo != null)
                    GLWrapper.PopMaskingInfo();
            }

            internal override void DrawOpaqueInteriorSubTree(DepthValue depthValue, Action<TexturedVertex2D> vertexAction)
            {
                DrawChildrenOpaqueInteriors(depthValue, vertexAction);
                base.DrawOpaqueInteriorSubTree(depthValue, vertexAction);
            }

            /// <summary>
            /// Performs <see cref="DrawOpaqueInteriorSubTree"/> on all children of this <see cref="CompositeDrawableDrawNode"/>.
            /// </summary>
            /// <param name="depthValue">The previous depth value.</param>
            /// <param name="vertexAction">The action to be performed on each vertex of the draw node in order to draw it if required. This is primarily used by textured sprites.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected virtual void DrawChildrenOpaqueInteriors(DepthValue depthValue, Action<TexturedVertex2D> vertexAction)
            {
                bool canIncrement = depthValue.CanIncrement;

                // Assume that if we can't increment the depth value, no child can, thus nothing will be drawn.
                if (canIncrement)
                {
                    updateTriangleBatch();

                    // Prefer to use own vertex batch instead of the parent-owned one.
                    if (triangleBatch != null)
                        vertexAction = triangleBatch.AddAction;

                    if (maskingInfo != null)
                        GLWrapper.PushMaskingInfo(maskingInfo.Value);
                }

                // We still need to invoke this method recursively for all children so their depth value is updated
                if (Children != null)
                {
                    for (int i = Children.Count - 1; i >= 0; i--)
                        Children[i].DrawOpaqueInteriorSubTree(depthValue, vertexAction);
                }

                // Assume that if we can't increment the depth value, no child can, thus nothing will be drawn.
                if (canIncrement)
                {
                    if (maskingInfo != null)
                        GLWrapper.PopMaskingInfo();
                }
            }

            protected override void Dispose(bool isDisposing)
            {
                base.Dispose(isDisposing);

                // Children disposed via their source drawables
                Children = null;

                quadBatch?.Dispose();
                triangleBatch?.Dispose();
            }
        }
    }
}
