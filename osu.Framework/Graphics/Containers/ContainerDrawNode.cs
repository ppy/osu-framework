// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Batches;
using OpenTK.Graphics;
using OpenTK.Graphics.ES30;
using osu.Framework.Extensions.MatrixExtensions;
using OpenTK;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;

namespace osu.Framework.Graphics.Containers
{
    public enum EdgeEffectType
    {
        None,
        Glow,
        Shadow,
    }

    public struct EdgeEffect
    {
        public Color4 Colour;
        public Vector2 Offset;
        public EdgeEffectType Type;
        public float Roundness;
        public float Radius;
    }

    public class ContainerDrawNodeSharedData
    {
        public QuadBatch<TexturedVertex2D> VertexBatch;
        public bool ForceOwnVertexBatch = false;
    }

    public class ContainerDrawNode : DrawNode
    {
        public List<DrawNode> Children;
        public MaskingInfo? MaskingInfo;
        public Quad? ScreenSpaceMaskingQuad = null;

        public EdgeEffect EdgeEffect;

        public ContainerDrawNodeSharedData Shared;

        public Shader Shader;

        private void drawEdgeEffect()
        {
            if (MaskingInfo == null || EdgeEffect.Type == EdgeEffectType.None || EdgeEffect.Radius <= 0.0f || EdgeEffect.Colour.A <= 0.0f)
                return;

            RectangleF effectRect = MaskingInfo.Value.MaskingRect.Inflate(EdgeEffect.Radius).Offset(EdgeEffect.Offset);
            if (!ScreenSpaceMaskingQuad.HasValue)
                ScreenSpaceMaskingQuad = Quad.FromRectangle(effectRect) * DrawInfo.Matrix;

            MaskingInfo edgeEffectMaskingInfo = MaskingInfo.Value;
            edgeEffectMaskingInfo.MaskingRect = effectRect;
            edgeEffectMaskingInfo.ScreenSpaceAABB = ScreenSpaceMaskingQuad.Value.AABB;
            edgeEffectMaskingInfo.CornerRadius += EdgeEffect.Radius + EdgeEffect.Roundness;
            edgeEffectMaskingInfo.BorderThickness = 0;
            edgeEffectMaskingInfo.LinearBlendRange = EdgeEffect.Radius;

            GLWrapper.PushScissor(edgeEffectMaskingInfo);

            GLWrapper.SetBlend(new BlendingInfo(EdgeEffect.Type == EdgeEffectType.Glow ? BlendingMode.Additive : BlendingMode.Mixture));

            Shader.Bind();

            Color4 colour = EdgeEffect.Colour;
            colour.A *= DrawInfo.Colour.A;

            Texture.WhitePixel.Draw(ScreenSpaceMaskingQuad.Value, colour);

            Shader.Unbind();

            GLWrapper.PopScissor();
        }

        private const int MIN_AMOUNT_CHILDREN_TO_WARRANT_BATCH = 5;

        private bool mayHaveOwnVertexBatch(int amountChildren) => Shared.ForceOwnVertexBatch || amountChildren >= MIN_AMOUNT_CHILDREN_TO_WARRANT_BATCH;

        private void updateVertexBatch()
        {
            if (Children == null)
                return;

            // This logic got roughly copied from the old osu! code base. These constants seem to have worked well so far.
            int clampedAmountChildren = MathHelper.Clamp(Children.Count, 1, 1000);
            if (mayHaveOwnVertexBatch(clampedAmountChildren) && (Shared.VertexBatch == null || Shared.VertexBatch.Size < clampedAmountChildren))
                Shared.VertexBatch = new QuadBatch<TexturedVertex2D>(clampedAmountChildren * 2, 500);
        }

        public override void Draw(IVertexBatch vertexBatch)
        {
            updateVertexBatch();

            // Prefer to use own vertex batch instead of the parent-owned one.
            if (Shared.VertexBatch != null)
                vertexBatch = Shared.VertexBatch;

            base.Draw(vertexBatch);

            drawEdgeEffect();
            if (MaskingInfo != null)
                GLWrapper.PushScissor(MaskingInfo.Value);

            if (Children != null)
                foreach (DrawNode child in Children)
                    child.Draw(vertexBatch);

            if (MaskingInfo != null)
                GLWrapper.PopScissor();
        }
    }
}
