// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osuTK;
using osu.Framework.Graphics.Colour;
using System;
using System.Runtime.CompilerServices;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;

namespace osu.Framework.Graphics.Containers
{
    public partial class CompositeDrawable
    {
        /// <summary>
        /// A draw node responsible for rendering a <see cref="CompositeDrawable"/> and the <see cref="DrawNode"/>s of its children.
        /// </summary>
        protected class CompositeDrawableDrawNode : DrawNode, ICompositeDrawNode
        {
            private static readonly float cos_45 = MathF.Cos(MathF.PI / 4);

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
            /// The screen-space version of <see cref="MaskingInfo.MaskingRect"/>.
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
            private IVertexBatch<TexturedVertex2D> quadBatch;

            private int sourceChildrenCount;

            public CompositeDrawableDrawNode(CompositeDrawable source)
                : base(source)
            {
            }

            public override void ApplyState()
            {
                base.ApplyState();

                if (!Source.Masking && (Source.BorderThickness != 0.0f || Source.EdgeEffect.Type != EdgeEffectType.None))
                    throw new InvalidOperationException("Can not have border effects/edge effects if masking is disabled.");

                Vector3 scale = DrawInfo.MatrixInverse.ExtractScale();
                float blendRange = Source.MaskingSmoothness * (scale.X + scale.Y) / 2;

                // Calculate a shrunk rectangle which is free from corner radius/smoothing/border effects
                float shrinkage = Source.CornerRadius - Source.CornerRadius * cos_45 + blendRange + Source.borderThickness;

                // Normalise to handle negative sizes, and clamp the shrinkage to prevent size from going negative.
                RectangleF shrunkDrawRectangle = Source.DrawRectangle.Normalize();
                shrunkDrawRectangle = shrunkDrawRectangle.Shrink(new Vector2(Math.Min(shrunkDrawRectangle.Width / 2, shrinkage), Math.Min(shrunkDrawRectangle.Height / 2, shrinkage)));

                maskingInfo = !Source.Masking
                    ? null
                    : new MaskingInfo
                    {
                        ScreenSpaceAABB = Source.ScreenSpaceDrawQuad.AABB,
                        MaskingRect = Source.DrawRectangle.Normalize(),
                        ConservativeScreenSpaceQuad = Quad.FromRectangle(shrunkDrawRectangle) * DrawInfo.Matrix,
                        ToMaskingSpace = DrawInfo.MatrixInverse,
                        CornerRadius = Source.effectiveCornerRadius,
                        CornerExponent = Source.CornerExponent,
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
                sourceChildrenCount = Source.internalChildren.Count;
            }

            public virtual bool AddChildDrawNodes => true;

            private void drawEdgeEffect(IRenderer renderer)
            {
                if (maskingInfo == null || edgeEffect.Type == EdgeEffectType.None || edgeEffect.Radius <= 0.0f || edgeEffect.Colour.Linear.A <= 0)
                    return;

                RectangleF effectRect = maskingInfo.Value.MaskingRect.Inflate(edgeEffect.Radius).Offset(edgeEffect.Offset);

                screenSpaceMaskingQuad ??= Quad.FromRectangle(effectRect) * DrawInfo.Matrix;

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

                renderer.PushMaskingInfo(edgeEffectMaskingInfo);

                renderer.SetBlend(edgeEffect.Type == EdgeEffectType.Glow ? BlendingParameters.Additive : BlendingParameters.Mixture);

                Shader.Bind();

                ColourInfo colour = ColourInfo.SingleColour(edgeEffect.Colour);
                colour.TopLeft.MultiplyAlpha(DrawColourInfo.Colour.TopLeft.Linear.A);
                colour.BottomLeft.MultiplyAlpha(DrawColourInfo.Colour.BottomLeft.Linear.A);
                colour.TopRight.MultiplyAlpha(DrawColourInfo.Colour.TopRight.Linear.A);
                colour.BottomRight.MultiplyAlpha(DrawColourInfo.Colour.BottomRight.Linear.A);

                renderer.DrawQuad(
                    renderer.WhitePixel,
                    screenSpaceMaskingQuad.Value,
                    colour, null, null, null,
                    // HACK HACK HACK. We re-use the unused vertex blend range to store the original
                    // masking blend range when rendering edge effects. This is needed for smooth inner edges
                    // with a hollow edge effect.
                    new Vector2(maskingInfo.Value.BlendRange));

                Shader.Unbind();

                renderer.PopMaskingInfo();
            }

            private const int min_amount_children_to_warrant_batch = 8;

            private bool mayHaveOwnVertexBatch(int amountChildren) => forceLocalVertexBatch || amountChildren >= min_amount_children_to_warrant_batch;

            private void updateQuadBatch(IRenderer renderer)
            {
                if (Children == null)
                    return;

                if (quadBatch == null && mayHaveOwnVertexBatch(sourceChildrenCount))
                    quadBatch = renderer.CreateQuadBatch<TexturedVertex2D>(100, 1000);
            }

            public override void Draw(IRenderer renderer)
            {
                updateQuadBatch(renderer);

                // Prefer to use own vertex batch instead of the parent-owned one.
                if (quadBatch != null)
                    renderer.PushQuadBatch(quadBatch);

                base.Draw(renderer);

                drawEdgeEffect(renderer);

                if (maskingInfo != null)
                {
                    MaskingInfo info = maskingInfo.Value;
                    if (info.BorderThickness > 0)
                        info.BorderColour = ColourInfo.Multiply(info.BorderColour, DrawColourInfo.Colour);

                    renderer.PushMaskingInfo(info);
                }

                if (Children != null)
                {
                    for (int i = 0; i < Children.Count; i++)
                        Children[i].Draw(renderer);
                }

                if (maskingInfo != null)
                    renderer.PopMaskingInfo();

                if (quadBatch != null)
                    renderer.PopQuadBatch();
            }

            internal override void DrawOpaqueInteriorSubTree(IRenderer renderer, DepthValue depthValue)
            {
                DrawChildrenOpaqueInteriors(renderer, depthValue);
                base.DrawOpaqueInteriorSubTree(renderer, depthValue);
            }

            /// <summary>
            /// Performs <see cref="DrawOpaqueInteriorSubTree"/> on all children of this <see cref="CompositeDrawableDrawNode"/>.
            /// </summary>
            /// <param name="renderer">The renderer to draw with.</param>
            /// <param name="depthValue">The previous depth value.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected virtual void DrawChildrenOpaqueInteriors(IRenderer renderer, DepthValue depthValue)
            {
                bool canIncrement = depthValue.CanIncrement;

                // Assume that if we can't increment the depth value, no child can, thus nothing will be drawn.
                if (canIncrement)
                {
                    updateQuadBatch(renderer);

                    // Prefer to use own vertex batch instead of the parent-owned one.
                    if (quadBatch != null)
                        renderer.PushQuadBatch(quadBatch);

                    if (maskingInfo != null)
                        renderer.PushMaskingInfo(maskingInfo.Value);
                }

                // We still need to invoke this method recursively for all children so their depth value is updated
                if (Children != null)
                {
                    for (int i = Children.Count - 1; i >= 0; i--)
                        Children[i].DrawOpaqueInteriorSubTree(renderer, depthValue);
                }

                // Assume that if we can't increment the depth value, no child can, thus nothing will be drawn.
                if (canIncrement)
                {
                    if (maskingInfo != null)
                        renderer.PopMaskingInfo();

                    if (quadBatch != null)
                        renderer.PopQuadBatch();
                }
            }

            protected override void Dispose(bool isDisposing)
            {
                base.Dispose(isDisposing);

                // Children disposed via their source drawables
                Children = null;

                quadBatch?.Dispose();
            }
        }
    }
}
