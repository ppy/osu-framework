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
    public class ContainerDrawNodeSharedData
    {
        public QuadBatch<TexturedVertex2D> VertexBatch;
        public bool ForceOwnVertexBatch = false;
    }

    public class ContainerDrawNode : ShadedDrawNode
    {
        public List<DrawNode> Children;
        public MaskingInfo? MaskingInfo;
        public Quad? ScreenSpaceMaskingQuad = null;
        public float GlowRadius;
        public Color4 GlowColour;

        public ContainerDrawNodeSharedData Shared;

        private void drawGlow()
        {
            if (MaskingInfo == null || GlowRadius <= 0.0f || GlowColour.A <= 0.0f)
                return;

            RectangleF glowRect = MaskingInfo.Value.MaskingRect.Inflate(GlowRadius);
            if (!ScreenSpaceMaskingQuad.HasValue)
                ScreenSpaceMaskingQuad = Quad.FromRectangle(glowRect) * DrawInfo.Matrix;

            MaskingInfo glowMaskingInfo = MaskingInfo.Value;
            glowMaskingInfo.MaskingRect = glowRect;
            glowMaskingInfo.ScreenSpaceAABB = ScreenSpaceMaskingQuad.Value.AABB;
            glowMaskingInfo.CornerRadius += GlowRadius;
            glowMaskingInfo.BorderThickness = 0;
            glowMaskingInfo.LinearBlendRange = GlowRadius;

            GLWrapper.PushScissor(glowMaskingInfo);

            GLWrapper.SetBlend(new BlendingInfo(true));

            Shader.Bind();

            Color4 glowColour = GlowColour;
            glowColour.A *= DrawInfo.Colour.A;

            Texture.WhitePixel.Draw(ScreenSpaceMaskingQuad.Value, glowColour);

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

            if (MaskingInfo != null)
                GLWrapper.PushScissor(MaskingInfo.Value);

            if (Children != null)
                for (int i = Children.Count - 1; i >= 0; --i)
                    Children[i].Draw(vertexBatch);

            if (MaskingInfo != null)
                GLWrapper.PopScissor();

            drawGlow();
        }
    }
}
