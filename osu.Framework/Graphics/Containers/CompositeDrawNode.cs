// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
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
using osu.Framework.Graphics.OpenGL.Vertices;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// Types of edge effects that can be applied to <see cref="CompositeDrawable"/>s.
    /// </summary>
    public enum EdgeEffectType
    {
        None,
        Glow,
        Shadow,
    }

    /// <summary>
    /// Parametrizes the appearance of an edge effect.
    /// </summary>
    public struct EdgeEffectParameters : IEquatable<EdgeEffectParameters>
    {
        /// <summary>
        /// Colour of the edge effect.
        /// </summary>
        public SRGBColour Colour;

        /// <summary>
        /// Positional offset applied to the edge effect.
        /// Useful for off-center shadows.
        /// </summary>
        public Vector2 Offset;

        /// <summary>
        /// The type of the edge effect.
        /// </summary>
        public EdgeEffectType Type;

        /// <summary>
        /// How round the edge effect should appear. Adds to the <see cref="CompositeDrawable.CornerRadius"/>
        /// of the corresponding <see cref="CompositeDrawable"/>. Not to confuse with the <see cref="Radius"/>.
        /// </summary>
        public float Roundness;

        /// <summary>
        /// How "thick" the edge effect is around the <see cref="CompositeDrawable"/>. In other words: At what distance
        /// from the <see cref="CompositeDrawable"/>'s border the edge effect becomes fully invisible.
        /// </summary>
        public float Radius;

        /// <summary>
        /// Whether the inside of the EdgeEffect rectangle should be empty.
        /// </summary>
        public bool Hollow;

        public bool Equals(EdgeEffectParameters other) =>
            Colour.Equals(other.Colour) &&
            Offset == other.Offset &&
            Type == other.Type &&
            Roundness == other.Roundness &&
            Radius == other.Radius;

        public override string ToString() => Type != EdgeEffectType.None ? $@"{Radius} {Type}EdgeEffect" : @"EdgeEffect (Disabled)";
    }

    /// <summary>
    /// A draw node responsible for rendering a <see cref="CompositeDrawable"/> and the
    /// <see cref="DrawNode"/>s of its children.
    /// </summary>
    public class CompositeDrawNode : DrawNode
    {
        protected new CompositeDrawable Source => (CompositeDrawable)base.Source;

        /// <summary>
        /// The shader to use for rendering.
        /// </summary>
        protected IShader Shader { get; private set; }

        /// <summary>
        /// The <see cref="DrawNode"/>s of the children of our <see cref="CompositeDrawable"/>.
        /// </summary>
        private IReadOnlyList<DrawNode> children;

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
        /// The vertex batch used for rendering.
        /// </summary>
        private QuadBatch<TexturedVertex2D> vertexBatch;

        public CompositeDrawNode(CompositeDrawable source)
            : base(source)
        {
        }

        public override void ApplyState()
        {
            base.ApplyState();

            if (!Source.Masking && (Source.BorderThickness != 0.0f || edgeEffect.Type != EdgeEffectType.None))
                throw new InvalidOperationException("Can not have border effects/edge effects if masking is disabled.");

            Vector3 scale = DrawInfo.MatrixInverse.ExtractScale();

            maskingInfo = !Source.Masking
                ? (MaskingInfo?)null
                : new MaskingInfo
                {
                    ScreenSpaceAABB = Source.ScreenSpaceDrawQuad.AABB,
                    MaskingRect = Source.DrawRectangle,
                    ToMaskingSpace = DrawInfo.MatrixInverse,
                    CornerRadius = Source.CornerRadius,
                    BorderThickness = Source.BorderThickness,
                    BorderColour = Source.BorderColour,
                    // We are setting the linear blend range to the approximate size of a _pixel_ here.
                    // This results in the optimal trade-off between crispness and smoothness of the
                    // edges of the masked region according to sampling theory.
                    BlendRange = Source.MaskingSmoothness * (scale.X + scale.Y) / 2,
                    AlphaExponent = 1,
                };

            edgeEffect = Source.EdgeEffect;
            screenSpaceMaskingQuad = null;
            Shader = Source.Shader;
            forceLocalVertexBatch = Source.ForceLocalVertexBatch;

            children = Source.ChildDrawNodes;
        }

        public virtual bool AddChildDrawNodes => true;

        private void drawEdgeEffect()
        {
            if (maskingInfo == null || edgeEffect.Type == EdgeEffectType.None || edgeEffect.Radius <= 0.0f || edgeEffect.Colour.Linear.A <= 0.0f)
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

            Texture.WhitePixel.DrawQuad(
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

        private void updateVertexBatch()
        {
            if (children == null)
                return;

            // This logic got roughly copied from the old osu! code base. These constants seem to have worked well so far.
            int clampedAmountChildren = MathHelper.Clamp(children.Count, 1, 1000);
            if (mayHaveOwnVertexBatch(clampedAmountChildren) && (vertexBatch == null || vertexBatch.Size < clampedAmountChildren))
                vertexBatch = new QuadBatch<TexturedVertex2D>(clampedAmountChildren * 2, 500);
        }

        public override void Draw(Action<TexturedVertex2D> vertexAction)
        {
            updateVertexBatch();

            // Prefer to use own vertex batch instead of the parent-owned one.
            if (vertexBatch != null)
                vertexAction = vertexBatch.AddAction;

            base.Draw(vertexAction);

            drawEdgeEffect();
            if (maskingInfo != null)
            {
                MaskingInfo info = maskingInfo.Value;
                if (info.BorderThickness > 0)
                    info.BorderColour *= DrawColourInfo.Colour.AverageColour;

                GLWrapper.PushMaskingInfo(info);
            }

            if (children != null)
                for (int i = 0; i < children.Count; i++)
                    children[i].Draw(vertexAction);

            if (maskingInfo != null)
                GLWrapper.PopMaskingInfo();
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            vertexBatch?.Dispose();
        }
    }
}
