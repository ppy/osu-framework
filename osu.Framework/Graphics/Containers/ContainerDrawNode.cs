// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Batches;
using OpenTK;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Colour;
using System;

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
        public SRGBColour Colour;
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
            if (MaskingInfo == null || EdgeEffect.Type == EdgeEffectType.None || EdgeEffect.Radius <= 0.0f || EdgeEffect.Colour.Linear.A <= 0.0f)
                return;

            RectangleF effectRect = MaskingInfo.Value.MaskingRect.Inflate(EdgeEffect.Radius).Offset(EdgeEffect.Offset);
            if (!ScreenSpaceMaskingQuad.HasValue)
                ScreenSpaceMaskingQuad = Quad.FromRectangle(effectRect) * DrawInfo.Matrix;

            MaskingInfo edgeEffectMaskingInfo = MaskingInfo.Value;
            edgeEffectMaskingInfo.MaskingRect = effectRect;
            edgeEffectMaskingInfo.ScreenSpaceAABB = ScreenSpaceMaskingQuad.Value.AABB;
            edgeEffectMaskingInfo.CornerRadius += EdgeEffect.Radius + EdgeEffect.Roundness;
            edgeEffectMaskingInfo.BorderThickness = 0;
            edgeEffectMaskingInfo.BlendRange = EdgeEffect.Radius;
            edgeEffectMaskingInfo.AlphaExponent = 2;

            GLWrapper.PushMaskingInfo(edgeEffectMaskingInfo);

            GLWrapper.SetBlend(new BlendingInfo(EdgeEffect.Type == EdgeEffectType.Glow ? BlendingMode.Additive : BlendingMode.Mixture));

            Shader.Bind();

            ColourInfo colour = ColourInfo.SingleColour(EdgeEffect.Colour);
            colour.TopLeft.MultiplyAlpha(DrawInfo.Colour.TopLeft.Linear.A);
            colour.BottomLeft.MultiplyAlpha(DrawInfo.Colour.BottomLeft.Linear.A);
            colour.TopRight.MultiplyAlpha(DrawInfo.Colour.TopRight.Linear.A);
            colour.BottomRight.MultiplyAlpha(DrawInfo.Colour.BottomRight.Linear.A);

            Texture.WhitePixel.DrawQuad(ScreenSpaceMaskingQuad.Value, colour);

            Shader.Unbind();

            GLWrapper.PopMaskingInfo();
        }

        private const int min_amount_children_to_warrant_batch = 5;

        protected Action<TexturedVertex2D> CustomVertexAction => null;

        private bool mayHaveOwnVertexBatch(int amountChildren) => Shared.ForceOwnVertexBatch || amountChildren >= min_amount_children_to_warrant_batch;

        private void updateVertexBatch()
        {
            if (Children == null)
                return;

            // This logic got roughly copied from the old osu! code base. These constants seem to have worked well so far.
            int clampedAmountChildren = MathHelper.Clamp(Children.Count, 1, 1000);
            if (mayHaveOwnVertexBatch(clampedAmountChildren) && (Shared.VertexBatch == null || Shared.VertexBatch.Size < clampedAmountChildren))
                Shared.VertexBatch = new QuadBatch<TexturedVertex2D>(clampedAmountChildren * 2, 500);
        }

        public override void Draw(Action<TexturedVertex2D> vertexAction)
        {
            if (CustomVertexAction == null)
            {
                updateVertexBatch();

                // Prefer to use own vertex batch instead of the parent-owned one.
                if (Shared.VertexBatch != null)
                    vertexAction = Shared.VertexBatch.Add;
            }
            else
                vertexAction = CustomVertexAction;

            base.Draw(vertexAction);

            drawEdgeEffect();
            if (MaskingInfo != null)
            {
                MaskingInfo info = MaskingInfo.Value;
                if (info.BorderThickness > 0)
                    info.BorderColour *= DrawInfo.Colour.AverageColour;

                GLWrapper.PushMaskingInfo(info);
            }

            if (Children != null)
                foreach (DrawNode child in Children)
                    child.Draw(vertexAction);

            if (MaskingInfo != null)
                GLWrapper.PopMaskingInfo();
        }
    }
}
